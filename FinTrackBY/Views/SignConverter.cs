using System;
using System.Globalization;
using System.Windows.Data;

namespace FinTrackBY.Views
{
    public class SignConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "Null";

            if (value is decimal d)
            {
                if (d > 0) return "Positive";
                if (d < 0) return "Negative";
                return "Zero";
            }

            return "Null";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
