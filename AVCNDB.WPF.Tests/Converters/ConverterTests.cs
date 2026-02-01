using AVCNDB.WPF.Converters;
using System.Globalization;

namespace AVCNDB.WPF.Tests.Converters;

/// <summary>
/// Tests unitaires pour les converters
/// </summary>
public class ConverterTests
{
    #region BooleanToVisibilityConverter Tests

    [Fact]
    public void BooleanToVisibilityConverter_TrueValue_ReturnsVisible()
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.Convert(true, typeof(object), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(System.Windows.Visibility.Visible);
    }

    [Fact]
    public void BooleanToVisibilityConverter_FalseValue_ReturnsCollapsed()
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.Convert(false, typeof(object), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(System.Windows.Visibility.Collapsed);
    }

    [Fact]
    public void BooleanToVisibilityConverter_WithInvert_ReturnsOpposite()
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var resultTrue = converter.Convert(true, typeof(object), "invert", CultureInfo.InvariantCulture);
        var resultFalse = converter.Convert(false, typeof(object), "invert", CultureInfo.InvariantCulture);

        // Assert
        resultTrue.Should().Be(System.Windows.Visibility.Collapsed);
        resultFalse.Should().Be(System.Windows.Visibility.Visible);
    }

    #endregion

    #region InverseBooleanConverter Tests

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void InverseBooleanConverter_ReturnsOpposite(bool input, bool expected)
    {
        // Arrange
        var converter = new InverseBooleanConverter();

        // Act
        var result = converter.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region CurrencyConverter Tests

    [Fact]
    public void CurrencyConverter_DecimalValue_ReturnsFormattedCurrency()
    {
        // Arrange
        var converter = new CurrencyConverter();

        // Act
        var result = converter.Convert(25.50m, typeof(string), "DH", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("25.50 DH");
    }

    [Fact]
    public void CurrencyConverter_NullValue_ReturnsDash()
    {
        // Arrange
        var converter = new CurrencyConverter();

        // Act
        var result = converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("-");
    }

    #endregion

    #region CountToVisibilityConverter Tests

    [Theory]
    [InlineData(0, "Collapsed")]
    [InlineData(1, "Visible")]
    [InlineData(10, "Visible")]
    public void CountToVisibilityConverter_ReturnsExpectedVisibility(int count, string expected)
    {
        // Arrange
        var converter = new CountToVisibilityConverter();
        var expectedVisibility = expected == "Visible" 
            ? System.Windows.Visibility.Visible 
            : System.Windows.Visibility.Collapsed;

        // Act
        var result = converter.Convert(count, typeof(object), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(expectedVisibility);
    }

    #endregion

    #region BoolToTextConverter Tests

    [Theory]
    [InlineData(true, "Oui|Non", "Oui")]
    [InlineData(false, "Oui|Non", "Non")]
    [InlineData(true, "Actif|Inactif", "Actif")]
    public void BoolToTextConverter_ReturnsExpectedText(bool input, string parameter, string expected)
    {
        // Arrange
        var converter = new BoolToTextConverter();

        // Act
        var result = converter.Convert(input, typeof(string), parameter, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region RelativeDateConverter Tests

    [Fact]
    public void RelativeDateConverter_JustNow_ReturnsInstant()
    {
        // Arrange
        var converter = new RelativeDateConverter();
        var date = DateTime.Now.AddSeconds(-30);

        // Act
        var result = converter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("À l'instant");
    }

    [Fact]
    public void RelativeDateConverter_HoursAgo_ReturnsHours()
    {
        // Arrange
        var converter = new RelativeDateConverter();
        var date = DateTime.Now.AddHours(-2);

        // Act
        var result = converter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        ((string)result).Should().Contain("Il y a 2 h");
    }

    [Fact]
    public void RelativeDateConverter_DaysAgo_ReturnsDays()
    {
        // Arrange
        var converter = new RelativeDateConverter();
        var date = DateTime.Now.AddDays(-3);

        // Act
        var result = converter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        ((string)result).Should().Contain("Il y a 3 jour");
    }

    [Fact]
    public void RelativeDateConverter_OldDate_ReturnsFormattedDate()
    {
        // Arrange
        var converter = new RelativeDateConverter();
        var date = new DateTime(2024, 1, 15);

        // Act
        var result = converter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        ((string)result).Should().Be("15/01/2024");
    }

    #endregion
}
