using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MyProgram.Helpers
{
    public static class TextBoxHelper
    {
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.RegisterAttached(
                "PlaceholderText",
                typeof(string),
                typeof(TextBoxHelper),
                new PropertyMetadata(string.Empty, OnPlaceholderChanged));

        public static string GetPlaceholderText(DependencyObject obj) =>
            (string)obj.GetValue(PlaceholderTextProperty);

        public static void SetPlaceholderText(DependencyObject obj, string value) =>
            obj.SetValue(PlaceholderTextProperty, value);

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.GotFocus -= OnTextBoxGotFocus;
                textBox.LostFocus -= OnTextBoxLostFocus;
                textBox.GotFocus += OnTextBoxGotFocus;
                textBox.LostFocus += OnTextBoxLostFocus;

                // 初始加载时设置
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.Text = (string)e.NewValue;
                    textBox.Foreground = Brushes.Gray;
                }
            }
        }

        private static void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Text == GetPlaceholderText(tb))
            {
                tb.Text = string.Empty;
                tb.Foreground = Brushes.Black;
            }
        }

        private static void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrEmpty(tb.Text))
            {
                tb.Text = GetPlaceholderText(tb);
                tb.Foreground = Brushes.Gray;
            }
        }
    }
}
