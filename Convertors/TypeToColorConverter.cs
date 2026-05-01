using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace MyProgram.Convertors
{
    /// <summary>
    /// 日志类型 → 颜色转换器
    /// 连接：蓝色
    /// 发送：青色
    /// 接收：绿色
    /// 错误：红色
    /// </summary>
    [ValueConversion(typeof(string), typeof(Brush))]
    public class TypeToColorConverter : MarkupExtension, IValueConverter
    {
        private static TypeToColorConverter _instance;

        // 直接在 XAML 中使用，无需声明资源
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ??= new TypeToColorConverter();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string type)
                return Brushes.Black;

            return type switch
            {
                "连接" => Brushes.Blue,
                "发送" => Brushes.DarkCyan,
                "接收" => Brushes.Green,
                "错误" => Brushes.Red,
                "断开" => Brushes.Orange,
                _ => Brushes.Black
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}