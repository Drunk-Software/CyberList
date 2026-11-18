using System.Globalization;
using Avalonia.Data;
using CyberList.Converters;
using CyberList.ViewModels;

namespace CyberList.Tests;

public class EnumEqualsConverterTests
{
    private readonly EnumEqualsConverter _converter = new();

    [Fact]
    public void Convert_is_true_only_for_the_matching_value()
    {
        Assert.Equal(true, _converter.Convert(ItemFilter.Done, typeof(bool?), ItemFilter.Done, CultureInfo.InvariantCulture));
        Assert.Equal(false, _converter.Convert(ItemFilter.All, typeof(bool?), ItemFilter.Done, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ConvertBack_selects_the_value_when_checked()
    {
        var result = _converter.ConvertBack(true, typeof(ItemFilter), ItemFilter.Active, CultureInfo.InvariantCulture);

        Assert.Equal(ItemFilter.Active, result);
    }

    [Fact]
    public void ConvertBack_does_nothing_when_unchecked()
    {
        // When one radio button turns off because another turned on, the filter must not change.
        var result = _converter.ConvertBack(false, typeof(ItemFilter), ItemFilter.Active, CultureInfo.InvariantCulture);

        Assert.Same(BindingOperations.DoNothing, result);
    }
}
