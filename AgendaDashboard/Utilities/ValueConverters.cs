using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AgendaDashboard.Utilities;

// AddValueConverter is a value converter that adds a specified value to the input.
public class AddValueConverter : IValueConverter
{
    public double Addend { get; set; } // Size values in WPF are double

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => throw new ArgumentNullException(nameof(value)),
            double d => d + Addend,
            _ => value
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// OrBooleanToVisibilityConverter is a multi-value converter that returns Visibility.Visible if any of the input booleans are true.
public class OrBooleanToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var value in values)
            if (value is true)
                return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
