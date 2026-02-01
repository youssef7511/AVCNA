using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Tests.Models;

/// <summary>
/// Tests unitaires pour le modèle Medic
/// </summary>
public class MedicTests
{
    [Fact]
    public void Medic_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var medic = new Medic();

        // Assert
        medic.recordid.Should().Be(0);
        medic.itemname.Should().BeEmpty();
        medic.barcode.Should().BeEmpty();
        medic.pediatric.Should().Be(0);
        medic.isactive.Should().Be(1);
    }

    [Fact]
    public void Medic_WithAllProperties_SetsCorrectly()
    {
        // Arrange
        var medic = new Medic
        {
            recordid = 1,
            itemname = "Test Medic",
            barcode = "3400935262585",
            dci1 = "Paracétamol",
            dci2 = "Ibuprofène",
            dose1 = "500mg",
            dose2 = "250mg",
            family = "Antalgiques",
            labo = "Sanofi",
            forme = "Comprimé",
            voie = "Orale",
            present = "Boîte de 20",
            price = 2550,
            refprice = 2000,
            pediatric = 1,
            tableau = "A",
            amm = "AMM123456"
        };

        // Assert
        medic.recordid.Should().Be(1);
        medic.itemname.Should().Be("Test Medic");
        medic.barcode.Should().Be("3400935262585");
        medic.dci1.Should().Be("Paracétamol");
        medic.dci2.Should().Be("Ibuprofène");
        medic.dose1.Should().Be("500mg");
        medic.dose2.Should().Be("250mg");
        medic.price.Should().Be(2550);
        medic.pediatric.Should().Be(1);
        medic.tableau.Should().Be("A");
    }

    [Theory]
    [InlineData("", "", true)]  // Sans DCI
    [InlineData("Paracétamol", "", false)]    // Avec DCI1
    [InlineData("Paracétamol", "Ibuprofène", false)]       // Avec DCI1 et DCI2
    public void Medic_HasNoDci_ReturnsExpectedResult(string dci1, string dci2, bool expectedNoDci)
    {
        // Arrange
        var medic = new Medic { dci1 = dci1, dci2 = dci2 };

        // Act
        var hasNoDci = string.IsNullOrEmpty(medic.dci1) && string.IsNullOrEmpty(medic.dci2);

        // Assert
        hasNoDci.Should().Be(expectedNoDci);
    }
}
