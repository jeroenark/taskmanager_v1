using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace taskmanager_v1
{
    public class CompletionColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            return status == "Y"
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))    // Green for completed
                : new SolidColorBrush(Color.FromRgb(255, 152, 0));   // Orange for not completed
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CompletionTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            return status == "Y" ? "Completed" : "Pending";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}