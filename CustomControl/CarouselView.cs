using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MyProgram.CustomControl
{
    /// <summary>
    /// 自适应宽度、支持无限循环的轮播控件。
    /// 自动填满父容器，始终只显示一个项，不显示相邻项。
    /// 提供导航按钮、指示器、拖拽、自动播放等功能。
    /// </summary>
    [TemplatePart(Name = "PART_ScrollViewer", Type = typeof(ScrollViewer))]
    [TemplatePart(Name = "PART_IndicatorPanel", Type = typeof(ItemsControl))]
    [TemplatePart(Name = "PART_PreviousButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_NextButton", Type = typeof(Button))]
    public class CarouselView : Selector
    {
        #region 依赖属性

        /// <summary> 是否启用循环滚动 </summary>
        public static readonly DependencyProperty IsLoopingProperty =
            DependencyProperty.Register("IsLooping", typeof(bool), typeof(CarouselView),
                new PropertyMetadata(false));

        /// <summary> 是否自动播放 </summary>
        public static readonly DependencyProperty IsAutoPlayProperty =
            DependencyProperty.Register("IsAutoPlay", typeof(bool), typeof(CarouselView),
                new PropertyMetadata(false, OnAutoPlayChanged));

        /// <summary> 自动播放间隔（毫秒） </summary>
        public static readonly DependencyProperty AutoPlayIntervalProperty =
            DependencyProperty.Register("AutoPlayInterval", typeof(int), typeof(CarouselView),
                new PropertyMetadata(3000));

        /// <summary> 切换动画时长（秒） </summary>
        public static readonly DependencyProperty TransitionDurationProperty =
            DependencyProperty.Register("TransitionDuration", typeof(double), typeof(CarouselView),
                new PropertyMetadata(0.3));

        /// <summary> 指示器项模板（可自定义） </summary>
        public static readonly DependencyProperty IndicatorTemplateProperty =
            DependencyProperty.Register("IndicatorTemplate", typeof(DataTemplate), typeof(CarouselView));

        /// <summary> 是否允许用户通过拖拽/触摸滑动 </summary>
        public static readonly DependencyProperty CanUserSwipeProperty =
            DependencyProperty.Register("CanUserSwipe", typeof(bool), typeof(CarouselView),
                new PropertyMetadata(true));

        /// <summary> 选中指示器的画刷 </summary>
        public static readonly DependencyProperty SelectedIndicatorBrushProperty =
            DependencyProperty.Register("SelectedIndicatorBrush", typeof(Brush), typeof(CarouselView),
                new PropertyMetadata(Brushes.White));

        /// <summary> 未选中指示器的画刷 </summary>
        public static readonly DependencyProperty UnselectedIndicatorBrushProperty =
            DependencyProperty.Register("UnselectedIndicatorBrush", typeof(Brush), typeof(CarouselView),
                new PropertyMetadata(Brushes.Gray));

        /// <summary> 是否显示左右导航按钮 </summary>
        public static readonly DependencyProperty ShowNavigationButtonsProperty =
            DependencyProperty.Register("ShowNavigationButtons", typeof(bool), typeof(CarouselView),
                new PropertyMetadata(true));

        // CLR 包装器，方便在 XAML 中直接赋值
        public bool IsLooping { get => (bool)GetValue(IsLoopingProperty); set => SetValue(IsLoopingProperty, value); }
        public bool IsAutoPlay { get => (bool)GetValue(IsAutoPlayProperty); set => SetValue(IsAutoPlayProperty, value); }
        public int AutoPlayInterval { get => (int)GetValue(AutoPlayIntervalProperty); set => SetValue(AutoPlayIntervalProperty, value); }
        public double TransitionDuration { get => (double)GetValue(TransitionDurationProperty); set => SetValue(TransitionDurationProperty, value); }
        public DataTemplate IndicatorTemplate { get => (DataTemplate)GetValue(IndicatorTemplateProperty); set => SetValue(IndicatorTemplateProperty, value); }
        public bool CanUserSwipe { get => (bool)GetValue(CanUserSwipeProperty); set => SetValue(CanUserSwipeProperty, value); }
        public Brush SelectedIndicatorBrush { get => (Brush)GetValue(SelectedIndicatorBrushProperty); set => SetValue(SelectedIndicatorBrushProperty, value); }
        public Brush UnselectedIndicatorBrush { get => (Brush)GetValue(UnselectedIndicatorBrushProperty); set => SetValue(UnselectedIndicatorBrushProperty, value); }
        public bool ShowNavigationButtons { get => (bool)GetValue(ShowNavigationButtonsProperty); set => SetValue(ShowNavigationButtonsProperty, value); }

        /// <summary> 切换自动播放的开关 </summary>
        private static void OnAutoPlayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CarouselView cv)
            {
                if ((bool)e.NewValue) cv.StartAutoPlay();
                else cv.StopAutoPlay();
            }
        }
        #endregion

        #region 内部字段

        private ScrollViewer _scrollViewer;
        private ItemsControl _indicatorPanel;
        private Button _prevButton, _nextButton;
        private DispatcherTimer _autoTimer;

        // 拖拽状态
        private bool _isUserInteracting;
        private bool _isDragging;
        private Point _dragStart;
        private double _dragStartOffset;

        // 动画状态
        private bool _isAnimating;
        private bool _isLoopingJump;                     // 循环跳转中，忽略 ScrollChanged 的干扰

        /// <summary> 扩展后的数据集合（原始集合 + 首尾影子项） </summary>
        private List<object> _extendedItems = new List<object>();

        /// <summary> 原始数据项个数（不含影子） </summary>
        private int OriginalItemCount => _extendedItems.Count > 2 ? _extendedItems.Count - 2 : 0;

        private bool _buildingExtended;                  // 防止递归构建扩展集合

        // 动画代理依赖属性，解决 ScrollViewer.HorizontalOffset 无法应用动画的限制
        private static readonly DependencyProperty AnimatedOffsetProperty =
            DependencyProperty.Register(
                "AnimatedOffset",
                typeof(double),
                typeof(CarouselView),
                new PropertyMetadata(0.0, OnAnimatedOffsetChanged));

        private double AnimatedOffset
        {
            get => (double)GetValue(AnimatedOffsetProperty);
            set => SetValue(AnimatedOffsetProperty, value);
        }

        /// <summary> 当代理属性值改变时，驱动 ScrollViewer 滚动到指定偏移 </summary>
        private static void OnAnimatedOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cv = (CarouselView)d;
            // 用户拖拽中或循环跳转中不执行自动滚动，避免冲突
            if (cv._scrollViewer != null && !cv._isUserInteracting && !cv._isLoopingJump)
                cv._scrollViewer.ScrollToHorizontalOffset((double)e.NewValue);
        }
        #endregion

        static CarouselView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CarouselView), new FrameworkPropertyMetadata(typeof(CarouselView)));
        }

        public CarouselView()
        {
            Loaded += OnLoaded;
        }

        /// <summary> 在控件模板应用时获取子控件并注册事件 </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
            if (_scrollViewer != null)
            {
                // 当 ScrollViewer 尺寸变化时，重新对齐当前选中项（因为步长可能改变）
                _scrollViewer.SizeChanged += (s, e) => AlignToCurrentItem();
                _scrollViewer.ScrollChanged += OnScrollChanged;
                _scrollViewer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
                _scrollViewer.PreviewMouseMove += OnMouseMove;
                _scrollViewer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
                _scrollViewer.PreviewMouseWheel += OnMouseWheel;
            }

            _indicatorPanel = GetTemplateChild("PART_IndicatorPanel") as ItemsControl;
            if (_indicatorPanel != null)
                _indicatorPanel.PreviewMouseLeftButtonDown += OnIndicatorClick; // 点击指示器切换

            _prevButton = GetTemplateChild("PART_PreviousButton") as Button;
            if (_prevButton != null)
                _prevButton.Click += (s, e) => Previous();

            _nextButton = GetTemplateChild("PART_NextButton") as Button;
            if (_nextButton != null)
                _nextButton.Click += (s, e) => Next();

            // 初始对齐
            AlignToCurrentItem();
        }

        // ═══════════════ 数据源处理 ═══════════════
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);
            if (!_buildingExtended)
                BuildExtendedItems();
        }

        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (!_buildingExtended)
                BuildExtendedItems();
        }

        /// <summary>
        /// 根据原始数据构建首尾各加一个影子项的扩展集合。
        /// 例如原始 [A,B,C,D] → 扩展后 [D, A,B,C,D, A]。
        /// </summary>
        private void BuildExtendedItems()
        {
            _buildingExtended = true;
            _extendedItems.Clear();

            if (ItemsSource != null)
            {
                // 将数据源转换为可索引的列表
                var source = ItemsSource as IList ?? ItemsSource.Cast<object>().ToList();
                if (source.Count > 0)
                {
                    // 首部影子 = 最后一项；尾部影子 = 第一项
                    _extendedItems.Add(source[source.Count - 1]);
                    foreach (var item in source)
                        _extendedItems.Add(item);
                    _extendedItems.Add(source[0]);
                }
            }

            // 替换底层 ItemsSource，触发 UI 生成
            base.ItemsSource = _extendedItems;
            UpdateIndicators();
            AlignToCurrentItem();
        }

        // ═══════════════ 索引映射 ═══════════════
        /// <summary> 真实索引（0~N-1）→ 扩展索引（真实索引+1） </summary>
        private int RealToExtended(int realIndex) => realIndex + 1;

        /// <summary> 扩展索引 → 真实索引，处理首位影子的特殊情况 </summary>
        private int ExtendedToReal(int extendedIndex)
        {
            if (OriginalItemCount == 0) return 0;
            if (extendedIndex <= 0) return OriginalItemCount - 1;      // 第一个影子 → 最后一项
            if (extendedIndex >= OriginalItemCount + 1) return 0;      // 最后一个影子 → 第一项
            return extendedIndex - 1;
        }

        // ═══════════════ 布局与步长 ═══════════════
        /// <summary> 当前滚动步长 = ScrollViewer 的视口宽度 </summary>
        private double Step => _scrollViewer?.ViewportWidth ?? 0;

        /// <summary> 让 ScrollViewer 定位到当前 SelectedIndex 对应的正确位置 </summary>
        private void AlignToCurrentItem()
        {
            if (_scrollViewer == null || _extendedItems.Count < 3 || SelectedIndex < 0 || Step <= 0) return;
            _scrollViewer.ScrollToHorizontalOffset(RealToExtended(SelectedIndex) * Step);
        }

        // ═══════════════ 滚动动画（无限循环核心） ═══════════════
        /// <summary>
        /// 滚动到当前选中项的位置。
        /// </summary>
        /// <param name="animate">是否播放动画</param>
        /// <param name="direction">滚动方向：1=正向（下一项），-1=反向（上一项），0=未知/回弹</param>
        private void ScrollToSelectedIndex(bool animate, int direction = 0)
        {
            if (_scrollViewer == null || _extendedItems.Count < 3 || Step <= 0) return;

            int extIdx = RealToExtended(SelectedIndex);
            double realTarget = extIdx * Step; // 真实目标偏移

            // 非循环或不需要动画时直接跳转
            if (!IsLooping || !animate)
            {
                _scrollViewer.ScrollToHorizontalOffset(realTarget);
                return;
            }

            double cur = _scrollViewer.HorizontalOffset;  // 当前实际偏移
            double animTarget = realTarget;
            bool useShadow = false;                       // 是否使用影子项作为动画终点

            // 正向跨边界（最后一项 → 第一项）：动画终点设为尾部影子
            if (direction == 1 && SelectedIndex == 0)
            {
                animTarget = (OriginalItemCount + 1) * Step;
                useShadow = true;
            }
            // 反向跨边界（第一项 → 最后一项）：动画终点设为首部影子
            else if (direction == -1 && SelectedIndex == OriginalItemCount - 1)
            {
                animTarget = 0;
                useShadow = true;
            }

            _isAnimating = true;
            var story = new Storyboard();
            var anim = new DoubleAnimation(cur, animTarget, TimeSpan.FromSeconds(TransitionDuration))
            {
                AccelerationRatio = 0.3,
                DecelerationRatio = 0.3
            };
            Storyboard.SetTarget(anim, this);
            Storyboard.SetTargetProperty(anim, new PropertyPath(AnimatedOffsetProperty));
            story.Children.Add(anim);

            story.Completed += (s, e) =>
            {
                _isAnimating = false;
                if (useShadow)
                {
                    // 动画到达影子位置后瞬间跳转到真实位置（用户感觉不到）
                    _isLoopingJump = true;
                    _scrollViewer.ScrollToHorizontalOffset(realTarget);
                    Dispatcher.BeginInvoke(new Action(() => _isLoopingJump = false), DispatcherPriority.Background);
                }
                else
                {
                    // 确保最终位置精确对齐
                    _scrollViewer.ScrollToHorizontalOffset(realTarget);
                }
                ResetAutoPlay();
            };

            story.Begin();
        }

        /// <summary> 快速吸附到指定索引（拖拽释放时使用） </summary>
        private void SnapToIndex(int realIndex, int direction)
        {
            SelectedIndex = realIndex;
            ScrollToSelectedIndex(true, direction);
        }

        // ═══════════════ 指示器 ═══════════════
        /// <summary> 根据当前选中项刷新指示器面板 </summary>
        private void UpdateIndicators()
        {
            if (_indicatorPanel == null || OriginalItemCount == 0) return;

            var list = new List<IndicatorItem>();
            for (int i = 0; i < OriginalItemCount; i++)
                list.Add(new IndicatorItem { Index = i, IsActive = i == SelectedIndex });

            _indicatorPanel.ItemsSource = list;
        }

        /// <summary> 指示器数据项 </summary>
        public class IndicatorItem
        {
            public int Index { get; set; }
            public bool IsActive { get; set; }
        }

        /// <summary> 处理用户点击指示器圆点 </summary>
        private void OnIndicatorClick(object sender, MouseButtonEventArgs e)
        {
            if (_indicatorPanel == null || OriginalItemCount == 0) return;

            // 从被点击的视觉元素沿树向上查找 IndicatorItem
            DependencyObject element = e.OriginalSource as DependencyObject;
            while (element != null && element != _indicatorPanel)
            {
                if (element is FrameworkElement fe && fe.DataContext is IndicatorItem item)
                {
                    int dir = item.Index > SelectedIndex ? 1 : -1;
                    SelectedIndex = item.Index;
                    ScrollToSelectedIndex(true, dir);
                    e.Handled = true;
                    return;
                }
                element = VisualTreeHelper.GetParent(element);
            }
        }

        // ═══════════════ 选择变化 ═══════════════
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateIndicators();
            ResetAutoPlay();
        }

        // ═══════════════ ScrollChanged 同步索引 ═══════════════
        /// <summary>
        /// 当用户手动滚动（非拖拽、非动画）时，根据当前偏移量计算最近的真实索引，
        /// 并更新 SelectedIndex。
        /// </summary>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Items.Count == 0 || _isUserInteracting || _isAnimating || _isLoopingJump) return;

            double step = Step;
            if (step <= 0) return;

            int near = (int)Math.Round(e.HorizontalOffset / step);
            near = Math.Clamp(near, 0, _extendedItems.Count - 1);
            int real = ExtendedToReal(near);

            if (real != SelectedIndex)
                SelectedIndex = real;
        }

        // ═══════════════ 导航 ═══════════════
        /// <summary> 切换到下一项 </summary>
        public void Next()
        {
            if (OriginalItemCount == 0) return;

            int nxt = SelectedIndex + 1;
            if (nxt >= OriginalItemCount && IsLooping)
                nxt = 0;
            else if (nxt >= OriginalItemCount)
                return;

            SelectedIndex = nxt;
            ScrollToSelectedIndex(true, direction: 1);
        }

        /// <summary> 切换到上一项 </summary>
        public void Previous()
        {
            if (OriginalItemCount == 0) return;

            int prv = SelectedIndex - 1;
            if (prv < 0 && IsLooping)
                prv = OriginalItemCount - 1;
            else if (prv < 0)
                return;

            SelectedIndex = prv;
            ScrollToSelectedIndex(true, direction: -1);
        }

        // ═══════════════ 自动播放 ═══════════════
        private void StartAutoPlay()
        {
            if (_autoTimer == null)
            {
                _autoTimer = new DispatcherTimer();
                _autoTimer.Tick += (s, e) =>
                {
                    if (!_isUserInteracting && !_isAnimating)
                        Next();
                };
            }
            _autoTimer.Interval = TimeSpan.FromMilliseconds(AutoPlayInterval);
            _autoTimer.Start();
        }

        private void StopAutoPlay() => _autoTimer?.Stop();

        /// <summary> 重置自动播放定时器（用户交互后重新计时） </summary>
        private void ResetAutoPlay()
        {
            if (!IsAutoPlay) return;
            _autoTimer?.Stop();
            _autoTimer?.Start();
        }

        // ═══════════════ 拖拽手势 ═══════════════
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!CanUserSwipe || OriginalItemCount == 0) return;

            // 取消可能正在播放的动画
            BeginAnimation(AnimatedOffsetProperty, null);
            _isAnimating = false;

            _isUserInteracting = true;
            _isDragging = true;
            _dragStart = e.GetPosition(_scrollViewer);
            _dragStartOffset = _scrollViewer.HorizontalOffset;
            _scrollViewer.CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var pos = e.GetPosition(_scrollViewer);
            // 跟手滑动：起始偏移 + 鼠标移动距离
            _scrollViewer.ScrollToHorizontalOffset(_dragStartOffset + (_dragStart.X - pos.X));
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;

            _isDragging = false;
            _isUserInteracting = false;
            _scrollViewer.ReleaseMouseCapture();

            double delta = _dragStart.X - e.GetPosition(_scrollViewer).X;
            int dir = delta > 0 ? 1 : (delta < 0 ? -1 : 0); // 左滑为正 → 下一项

            // 滑动超过视口宽度的 30% 则切换
            if (Math.Abs(delta) > Step * 0.3)
            {
                int target = SelectedIndex + dir;
                if (target < 0 || target >= OriginalItemCount)
                {
                    if (IsLooping)
                        target = (target + OriginalItemCount) % OriginalItemCount;
                    else
                        target = Math.Clamp(target, 0, OriginalItemCount - 1);
                }
                SnapToIndex(target, dir);
            }
            else
            {
                // 距离不足，回弹到当前项
                ScrollToSelectedIndex(true, direction: 0);
            }

            ResetAutoPlay();
        }

        /// <summary> 鼠标滚轮切换 </summary>
        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!CanUserSwipe) return;
            if (e.Delta > 0) Previous();
            else Next();
            e.Handled = true;
        }

        /// <summary> 控件加载完成后的初始化 </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (IsAutoPlay) StartAutoPlay();
            AlignToCurrentItem();
        }
    }
}
