using AVCNDB.WPF.Helpers;
using FluentAssertions;

namespace AVCNDB.WPF.Tests.Helpers;

public class MedicDenominationHelperTests
{
    [Theory]
    [InlineData("betadine 500 ML Capsule Flacon 15", "betadine")] // keep the brand
    [InlineData("ABILIFY Comp 10 mg Bt 28", "ABILIFY")]
    [InlineData("Doliprane 1000 mg", "Doliprane")]
    [InlineData("   Efferalgan 500 mg", "Efferalgan")]            // leading whitespace
    [InlineData("betadine", "betadine")]                          // single word brand
    [InlineData("400 ML Capsule Flacon 15", "")]                  // starts with a dose → no brand
    [InlineData("500", "")]                                       // pure number → no brand
    [InlineData("", "")]
    public void ExtractCommercialPrefix_KeepsBrandUnlessItStartsWithADose(string input, string expected)
    {
        MedicDenominationHelper.ExtractCommercialPrefix(input).Should().Be(expected);
    }
}
