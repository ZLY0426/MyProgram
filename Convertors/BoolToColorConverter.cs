using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace MyProgram.Convertors
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOnline && isOnline)
            {
                // 在线：绿色
                return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // #4CAF50
            }
            else
            {
                // 离线：红色
                return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // #F44336
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
