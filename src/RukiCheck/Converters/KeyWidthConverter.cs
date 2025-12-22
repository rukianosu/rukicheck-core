using System.Globalization;
using System.Windows.Data;

namespace RukiCheck.Converters;

/// <summary>
/// キーの相対幅を実際のピクセル幅に変換
/// </summary>
public class KeyWidthConverter : IValueConverter
{
    private const double BaseWidth = 50.0; // 基準幅（1.0の時のピクセル数）

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            return width * BaseWidth;
        }
        return BaseWidth;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
