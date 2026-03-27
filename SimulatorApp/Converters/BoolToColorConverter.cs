using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SimulatorApp.Converters;

/// <summary>bool → Brush，true 用 TrueColor，false 用 FalseColor。</summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public class BoolToColorConverter : IValueConverter
{
    public Brush TrueColor  { get; set; } = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
    public Brush FalseColor { get; set; } = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? TrueColor : FalseColor;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
