using System;
using System.Globalization;
using System.Windows.Data;

namespace CocoroAIGUI.Converters
{
    /// <summary>
    /// 指定された値をパーセンテージで計算するコンバーター
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double actualValue && parameter is string percentageStr)
            {
                if (double.TryParse(percentageStr, out double percentage))
                {
                    return actualValue * percentage;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}