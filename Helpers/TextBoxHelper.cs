using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MyProgram.Helpers
{
    public static class TextBoxHelper
    {
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.RegisterAttached("PlaceholderText", typeof(string), typeof(TextBoxHelper),
                new PropertyMetadata(string.Empty, OnPlaceholderTextChanged));

        public static string GetPlaceholderText(DependencyObject obj) => (string)obj.GetValue(PlaceholderTextProperty);
        public static void SetPlaceholderText(DependencyObject obj, string value) => obj.SetValue(PlaceholderTextProperty, value);

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Control control)
            {
                control.Loaded -= Control_Loaded;
                control.Loaded += Control_Loaded;

                if (control.IsLoaded)
                    AddAdorner(control);
            }
        }

        private static void Control_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Control control)
                AddAdorner(control);
        }

        private static void AddAdorner(Control control)
        {
            var layer = AdornerLayer.GetAdornerLayer(control);
            if (layer == null) return;

            // 移除旧的相同装饰器
            var existing = layer.GetAdorners(control);
            if (existing != null)
                foreach (var adorner in existing)
                    if (adorner is WatermarkAdorner)
                        layer.Remove(adorner);

            layer.Add(new WatermarkAdorner(control, GetPlaceholderText(control)));
        }

        private class WatermarkAdorner : Adorner
        {
            private readonly string _placeholder;

            public WatermarkAdorner(UIElement adornedElement, string placeholder) : base(adornedElement)
            {
                _placeholder = placeholder;
                IsHitTestVisible = false;

                if (adornedElement is TextBox tb)
                {
                    tb.TextChanged += OnTextOrFocusChanged;
                    tb.GotFocus += OnTextOrFocusChanged;
                    tb.LostFocus += OnTextOrFocusChanged;
                    tb.Unloaded += OnAdornedElementUnloaded;   
                }
                else if (adornedElement is ComboBox cb)
                {
                    cb.SelectionChanged += OnSelectionChanged;
                    cb.GotFocus += OnTextOrFocusChanged;
                    cb.LostFocus += OnTextOrFocusChanged;
                    cb.Unloaded += OnAdornedElementUnloaded;   
                }
            }

            private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => InvalidateVisual();

            private void OnTextOrFocusChanged(object sender, RoutedEventArgs e) => InvalidateVisual();

            private void OnAdornedElementUnloaded(object sender, RoutedEventArgs e)
            {
                if (AdornedElement is not Control control) return;

                if (control is TextBox tb)
                {
                    tb.TextChanged -= OnTextOrFocusChanged;
                    tb.GotFocus -= OnTextOrFocusChanged;
                    tb.LostFocus -= OnTextOrFocusChanged;
                    tb.Unloaded -= OnAdornedElementUnloaded;
                }
                else if (control is ComboBox cb)
                {
                    cb.SelectionChanged -= OnSelectionChanged;
                    cb.GotFocus -= OnTextOrFocusChanged;
                    cb.LostFocus -= OnTextOrFocusChanged;
                    cb.Unloaded -= OnAdornedElementUnloaded;
                }

                var layer = AdornerLayer.GetAdornerLayer(control);
                layer?.Remove(this);
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                var adorned = AdornedElement as Control;
                if (adorned == null || string.IsNullOrEmpty(_placeholder)) return;

                bool shouldShow = false;
                if (adorned is TextBox tb)
                    shouldShow = string.IsNullOrEmpty(tb.Text) && !tb.IsFocused;
                else if (adorned is ComboBox cb)
                    shouldShow = string.IsNullOrEmpty(cb.Text) && !cb.IsFocused;

                if (!shouldShow) return;

                // 绘制灰色提示文字
                var foreground = new SolidColorBrush(Colors.Gray);
                var typeface = new Typeface(adorned.FontFamily, adorned.FontStyle,
                                            adorned.FontWeight, adorned.FontStretch);
                var formattedText = new FormattedText(_placeholder, CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, typeface, adorned.FontSize, foreground,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                // 简单偏移，实际可根据 Padding 调整
                double left = adorned.Padding.Left;
                double top = (adorned.ActualHeight - formattedText.Height) / 2;
                drawingContext.DrawText(formattedText, new Point(left, top));
            }
        }
    }
}
