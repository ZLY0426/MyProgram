using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MyProgram.Helpers
{
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;

    public static class PasswordHelper
    {
        // ==================== 可绑定密码功能（保持不变） ====================
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached(
                "Password",
                typeof(string),
                typeof(PasswordHelper),
                new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached(
                "Attach",
                typeof(bool),
                typeof(PasswordHelper),
                new PropertyMetadata(false, Attach));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(PasswordHelper));

        public static void SetAttach(DependencyObject obj, bool value) =>
            obj.SetValue(AttachProperty, value);

        public static bool GetAttach(DependencyObject obj) =>
            (bool)obj.GetValue(AttachProperty);

        public static string GetPassword(DependencyObject obj) =>
            (string)obj.GetValue(PasswordProperty);

        public static void SetPassword(DependencyObject obj, string value) =>
            obj.SetValue(PasswordProperty, value);

        private static bool GetIsUpdating(DependencyObject obj) =>
            (bool)obj.GetValue(IsUpdatingProperty);

        private static void SetIsUpdating(DependencyObject obj, bool value) =>
            obj.SetValue(IsUpdatingProperty, value);

        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not PasswordBox passwordBox) return;

            passwordBox.PasswordChanged -= PasswordChanged;

            if (!GetIsUpdating(passwordBox))
            {
                passwordBox.Password = (string)e.NewValue;
            }

            passwordBox.PasswordChanged += PasswordChanged;
        }

        private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not PasswordBox passwordBox) return;

            if ((bool)e.OldValue)
                passwordBox.PasswordChanged -= PasswordChanged;

            if ((bool)e.NewValue)
                passwordBox.PasswordChanged += PasswordChanged;
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox passwordBox) return;

            SetIsUpdating(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetIsUpdating(passwordBox, false);
        }

        // ==================== 基于装饰器的占位符功能 ====================
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.RegisterAttached(
                "PlaceholderText",
                typeof(string),
                typeof(PasswordHelper),
                new PropertyMetadata(null, OnPlaceholderTextChanged));

        public static string GetPlaceholderText(DependencyObject obj) =>
            (string)obj.GetValue(PlaceholderTextProperty);

        public static void SetPlaceholderText(DependencyObject obj, string value) =>
            obj.SetValue(PlaceholderTextProperty, value);

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Control control) return;

            // 在 Loaded 之后再添加装饰器（保证视觉树已构建）
            control.Loaded -= Control_Loaded;
            control.Loaded += Control_Loaded;

            if (control.IsLoaded)
                ApplyPlaceholderAdorner(control);
        }

        private static void Control_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Control control)
                ApplyPlaceholderAdorner(control);
        }

        private static void ApplyPlaceholderAdorner(Control control)
        {
            var layer = AdornerLayer.GetAdornerLayer(control);
            if (layer == null) return;

            // 移除旧的占位符装饰器（避免重复添加）
            RemoveExistingPlaceholderAdorner(layer, control);

            string placeholder = GetPlaceholderText(control);
            if (!string.IsNullOrEmpty(placeholder))
            {
                var adorner = new PlaceholderAdorner(control, placeholder);
                layer.Add(adorner);
            }
        }

        private static void RemoveExistingPlaceholderAdorner(AdornerLayer layer, Control control)
        {
            var adorners = layer.GetAdorners(control);
            if (adorners != null)
            {
                foreach (var adorner in adorners)
                {
                    if (adorner is PlaceholderAdorner)
                    {
                        layer.Remove(adorner);
                    }
                }
            }
        }

        /// <summary>
        /// 密码框占位符装饰器
        /// </summary>
        private class PlaceholderAdorner : Adorner
        {
            private readonly string _placeholder;

            public PlaceholderAdorner(Control adornedElement, string placeholder) : base(adornedElement)
            {
                _placeholder = placeholder;
                IsHitTestVisible = false; // 鼠标事件穿透到原控件

                if (adornedElement is PasswordBox passwordBox)
                {
                    // 监听密码变化
                    passwordBox.PasswordChanged += OnPasswordChanged;
                    // 监听焦点变化
                    passwordBox.GotFocus += OnFocusChanged;
                    passwordBox.LostFocus += OnFocusChanged;
                    // 监听尺寸变化
                    passwordBox.SizeChanged += OnSizeChanged;
                    // 当控件卸载时，去除装饰器并清理事件
                    passwordBox.Unloaded += OnAdornedElementUnloaded;
                }
                // 可扩展其他控件类型
            }

            private void OnPasswordChanged(object sender, RoutedEventArgs e) => InvalidateVisual();
            private void OnFocusChanged(object sender, RoutedEventArgs e) => InvalidateVisual();
            private void OnSizeChanged(object sender, SizeChangedEventArgs e) => InvalidateVisual();

            private void OnAdornedElementUnloaded(object sender, RoutedEventArgs e)
            {
                if (AdornedElement is not Control control) return;

                // 解除事件订阅
                if (control is PasswordBox pb)
                {
                    pb.PasswordChanged -= OnPasswordChanged;
                    pb.GotFocus -= OnFocusChanged;
                    pb.LostFocus -= OnFocusChanged;
                    pb.SizeChanged -= OnSizeChanged;
                    pb.Unloaded -= OnAdornedElementUnloaded;
                }

                // 从装饰器层移除自身
                var layer = AdornerLayer.GetAdornerLayer(control);
                if (layer != null)
                    layer.Remove(this);
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                var adorned = AdornedElement as Control;
                if (adorned == null) return;

                bool shouldShow = false;
                if (adorned is PasswordBox pb)
                {
                    // 仅当密码为空且无焦点时显示占位符
                    shouldShow = string.IsNullOrEmpty(pb.Password) && !pb.IsFocused;
                }

                if (!shouldShow) return;

                // 创建灰色提示文字
                var foreground = new SolidColorBrush(Colors.Gray) { Opacity = 0.7 };
                var typeface = new Typeface(
                    adorned.FontFamily,
                    adorned.FontStyle,
                    adorned.FontWeight,
                    adorned.FontStretch);

                var formattedText = new FormattedText(
                    _placeholder,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    adorned.FontSize,
                    foreground,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                // 计算绘制位置（考虑 Padding 及垂直居中）
                double left = adorned.Padding.Left;
                double top = (adorned.ActualHeight - formattedText.Height) / 2;

                drawingContext.DrawText(formattedText, new Point(left, top));
            }
        }
    }
}
