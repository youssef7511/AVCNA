using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Models;
using Xunit;

namespace AVCNDB.WPF.Tests.Helpers;

public class ExcelTemplateHelperTests
{
    [Fact]
    public void GetStrictColumns_Medic_ContainsKnownColumns()
    {
        var cols = ExcelTemplateHelper.GetStrictColumns<Medic>();

        Assert.Contains("recordid", cols);
        Assert.Contains("itemname", cols);
        Assert.Contains("dci", cols);
        Assert.Contains("forme", cols);
        Assert.Contains("voie", cols);
        Assert.Contains("present", cols);
        Assert.Contains("labo", cols);
    }

    [Fact]
    public void GetDuplicateColumns_IsCaseInsensitive()
    {
        var duplicates = ExcelTemplateHelper.GetDuplicateColumns(new[] { "itemname", "ItemName", "dci" });
        Assert.Single(duplicates);
        Assert.Equal("itemname", duplicates[0], ignoreCase: true);
    }

    [Fact]
    public void GetUnexpectedColumns_IsCaseInsensitive()
    {
        var unexpected = ExcelTemplateHelper.GetUnexpectedColumns(
            foundColumns: new[] { "itemname", "unknown" },
            expectedColumns: new[] { "ItemName" });

        Assert.Single(unexpected);
        Assert.Equal("unknown", unexpected[0]);
    }
}
