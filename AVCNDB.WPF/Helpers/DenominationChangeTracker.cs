using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Helpers;

/// <summary>
/// Tracks which medics changed since they were loaded into the Dénominations view,
/// so "Appliquer" only writes the rows that actually changed (one edited row → one
/// UPDATE) instead of re-saving the entire table.
///
/// A lightweight per-row signature over the editable fields (denomination + dose/unit
/// pointers) is captured at load; on apply, rows whose signature differs are the dirty set.
/// </summary>
public static class DenominationChangeTracker
{
    // Field delimiter for the signature — chosen to not occur in denomination data.
    // (A spurious match would only cause one extra harmless row write, never data loss.)
    private const string Sep = "␟";

    /// <summary>Signature of the fields editable from the Dénominations view.</summary>
    public static string Signature(Medic m) => string.Join(Sep,
        m.itemname, m.basename,
        m.dose1, m.u1, m.dose2, m.u2, m.dose3, m.u3, m.dose4, m.u4);

    /// <summary>Snapshot of the current state, keyed by recordid (taken at load).</summary>
    public static Dictionary<int, string> Snapshot(IEnumerable<Medic> medics)
    {
        var snapshot = new Dictionary<int, string>();
        foreach (var m in medics)
            snapshot[m.recordid] = Signature(m);
        return snapshot;
    }

    /// <summary>Rows whose signature differs from the snapshot (or that are new).</summary>
    public static List<Medic> GetChanged(IEnumerable<Medic> medics, IReadOnlyDictionary<int, string> snapshot)
        => medics.Where(m => !snapshot.TryGetValue(m.recordid, out var sig) || Signature(m) != sig)
                 .ToList();
}
