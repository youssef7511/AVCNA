using AVCNDB.WPF.Helpers;
using FluentAssertions;

namespace AVCNDB.WPF.Tests.Helpers;

/// <summary>
/// Pins the single posology-label formula shared by the Poso table dialog and the
/// medic dialog, so the two can never drift apart again ("x" / "/" vs " fois ").
/// </summary>
public class PosoDenominationHelperTests
{
    [Theory]
    [InlineData("1", "gel", "3", "jour", "1 gel x 3 / jour")]
    [InlineData("1", "gélule", "3", "14 jours", "1 gélule x 3 / 14 jours")]
    [InlineData("2", "cp", "2", "jour", "2 cp x 2 / jour")]
    public void Build_ProducesUnifiedXSlashFormula(string qty, string token, string prises, string periode, string expected)
    {
        PosoDenominationHelper.Build(qty, token, prises, periode).Should().Be(expected);
    }

    [Fact]
    public void Build_ToleratesNulls_DoesNotThrow()
    {
        var act = () => PosoDenominationHelper.Build(null, null, null, null);
        act.Should().NotThrow();
    }
}
