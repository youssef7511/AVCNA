using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Models;
using Xunit;

namespace AVCNDB.WPF.Tests.Helpers;

public class EntityCopyHelperTests
{
    [Fact]
    public void CopyWritableProperties_ExcludesRecordId_WhenSpecified()
    {
        var source = new Dci
        {
            recordid = 123,
            itemname = "AAA",
            subvalue = "S",
            iteminfo = "I"
        };

        var dest = new Dci
        {
            recordid = 999,
            itemname = "OLD",
            subvalue = "OLD",
            iteminfo = "OLD"
        };

        EntityCopyHelper.CopyWritableProperties(source, dest, "recordid");

        Assert.Equal(999, dest.recordid);
        Assert.Equal("AAA", dest.itemname);
        Assert.Equal("S", dest.subvalue);
        Assert.Equal("I", dest.iteminfo);
    }
}
