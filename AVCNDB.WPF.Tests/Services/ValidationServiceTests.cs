using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.Tests.Services;

/// <summary>
/// Tests unitaires pour le service de validation
/// </summary>
public class ValidationServiceTests
{
    private readonly ValidationService _validationService;

    public ValidationServiceTests()
    {
        _validationService = new ValidationService();
    }

    #region Barcode Validation Tests

    [Theory]
    [InlineData("3400935262585", true)]  // EAN-13 valide (13 chiffres)
    [InlineData("34009352", true)]        // 8 chiffres minimum
    [InlineData("340093526258", true)]    // 12 chiffres valide
    [InlineData("1234567", false)]        // Trop court (7 chiffres)
    [InlineData("", false)]               // Vide
    public void IsValidBarcode_ReturnsExpectedResult(string barcode, bool expected)
    {
        // Act
        var result = _validationService.IsValidBarcode(barcode);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region AMM Validation Tests

    [Theory]
    [InlineData("AMM123456", true)]       // Format valide: lettres + chiffres
    [InlineData("A1234", true)]           // 1 lettre + 4 chiffres minimum
    [InlineData("ABC12345678", true)]     // 3 lettres + 8 chiffres
    [InlineData("AB1234567A", true)]      // Lettre finale optionnelle
    [InlineData("", true)]                // AMM optionnel
    public void IsValidAmm_ReturnsExpectedResult(string amm, bool expected)
    {
        // Act
        var result = _validationService.IsValidAmm(amm);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Phone Validation Tests

    [Theory]
    [InlineData("20123456", true)]        // Tunisien 8 chiffres commençant par 2
    [InlineData("50123456", true)]        // Tunisien 8 chiffres commençant par 5
    [InlineData("70123456", true)]        // Tunisien 8 chiffres commençant par 7
    [InlineData("90123456", true)]        // Tunisien 8 chiffres commençant par 9
    [InlineData("+21620123456", true)]    // Avec indicatif +216
    [InlineData("123", false)]            // Trop court
    [InlineData("", true)]                // Téléphone optionnel
    public void IsValidPhone_ReturnsExpectedResult(string phone, bool expected)
    {
        // Act
        var result = _validationService.IsValidPhone(phone);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Email Validation Tests

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user.name@domain.co.ma", true)]
    [InlineData("invalidemail", false)]
    [InlineData("@nodomain.com", false)]
    [InlineData("", true)]  // Email optionnel
    public void IsValidEmail_ReturnsExpectedResult(string email, bool expected)
    {
        // Act
        var result = _validationService.IsValidEmail(email);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
