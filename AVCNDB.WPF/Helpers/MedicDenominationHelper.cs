using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Helpers;

public static class MedicDenominationHelper
{
    /// <summary>
    /// Builds a denomination string from the medic's component fields.
    /// Formula: dose1 u1 dose2 u2 dose3 u3 dose4 u4 forme present colisage ucol
    /// </summary>
    public static string BuildDenomination(Medic m)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(m.dose1)) parts.Add(m.dose1.Trim());
        if (!string.IsNullOrWhiteSpace(m.u1)) parts.Add(m.u1.Trim());
        if (!string.IsNullOrWhiteSpace(m.dose2)) parts.Add(m.dose2.Trim());
        if (!string.IsNullOrWhiteSpace(m.u2)) parts.Add(m.u2.Trim());
        if (!string.IsNullOrWhiteSpace(m.dose3)) parts.Add(m.dose3.Trim());
        if (!string.IsNullOrWhiteSpace(m.u3)) parts.Add(m.u3.Trim());
        if (!string.IsNullOrWhiteSpace(m.dose4)) parts.Add(m.dose4.Trim());
        if (!string.IsNullOrWhiteSpace(m.u4)) parts.Add(m.u4.Trim());
        if (!string.IsNullOrWhiteSpace(m.forme)) parts.Add(m.forme.Trim());
        if (!string.IsNullOrWhiteSpace(m.present)) parts.Add(m.present.Trim());
        if (m.colisage > 0) parts.Add(m.colisage.ToString());
        if (!string.IsNullOrWhiteSpace(m.ucol)) parts.Add(m.ucol.Trim());

        return string.Join(" ", parts);
    }
}
