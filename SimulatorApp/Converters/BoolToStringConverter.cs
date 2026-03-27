using System.Globalization;
using System.Windows.Data;

namespace SimulatorApp.Converters;

/// <summary>
/// 将 bool 值转换为两段字符串之一。
/// ConverterParameter 格式：TrueString|FalseString
/// </summary>
[ValueConversion(typeof(bool), typeof(string))]
public class BoolToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is true;
        var parts = parameter?.ToString()?.Split('|');
        if (parts?.Length == 2)
            return b ? parts[0] : parts[1];
        return b.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
