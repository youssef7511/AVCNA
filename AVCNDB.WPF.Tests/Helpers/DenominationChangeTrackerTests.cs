using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Models;
using FluentAssertions;

namespace AVCNDB.WPF.Tests.Helpers;

/// <summary>
/// Pins the contract that "Appliquer" persists only the rows that actually changed
/// since load — so editing one denomination writes one row, not the whole table.
/// </summary>
public class DenominationChangeTrackerTests
{
    [Fact]
    public void GetChanged_ReturnsOnlyRowsModifiedSinceSnapshot()
    {
        var medics = new List<Medic>
        {
            new() { recordid = 1, itemname = "DOLIPRANE", basename = "" },
            new() { recordid = 2, itemname = "ASPIRINE",  basename = "" },
            new() { recordid = 3, itemname = "EFFERALGAN", basename = "" },
        };

        var snapshot = DenominationChangeTracker.Snapshot(medics);

        // User edits a single row's new denomination.
        medics[1].basename = "ASPIRINE 500 mg Comprimé Boîte 20";

        var changed = DenominationChangeTracker.GetChanged(medics, snapshot);

        changed.Should().ContainSingle("only one row was modified")
               .Which.recordid.Should().Be(2);
    }

    [Fact]
    public void GetChanged_NoEdits_ReturnsEmpty()
    {
        var medics = new List<Medic>
        {
            new() { recordid = 1, itemname = "DOLIPRANE", dose1 = "500", u1 = "mg" },
            new() { recordid = 2, itemname = "ASPIRINE" },
        };

        var snapshot = DenominationChangeTracker.Snapshot(medics);

        DenominationChangeTracker.GetChanged(medics, snapshot)
            .Should().BeEmpty("nothing changed since load");
    }

    [Fact]
    public void GetChanged_DetectsPointerEdits_NotJustBasename()
    {
        var medics = new List<Medic> { new() { recordid = 1, itemname = "X", dose1 = "1", u1 = "g" } };
        var snapshot = DenominationChangeTracker.Snapshot(medics);

        medics[0].dose1 = "2";   // "Init. Pointeurs"-style change

        DenominationChangeTracker.GetChanged(medics, snapshot)
            .Should().ContainSingle().Which.recordid.Should().Be(1);
    }
}
