using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace CyberList.Converters;

public class EnumEqualsConverter : IValueConverter
{
    /// <summary>
    /// Two-way: enum value "is this the selected one?" bool.
    /// Pass the enum value this control represents as ConverterParameter.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Equals(value, parameter);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? parameter : BindingOperations.DoNothing;
}
