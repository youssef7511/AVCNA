namespace AVCNDB.WPF.Helpers;

/// <summary>
/// Single source of truth for the auto-generated posology label, shared by the Poso
/// table dialog and the medic dialog so they stay identical:
///   "{qty} {token} x {prises} / {periode}"
/// where <c>token</c> is the Forme's posology verb (<c>formes.posoform</c>) — the medic
/// dialog's "Forme poso." field plays exactly that role.
/// </summary>
public static class PosoDenominationHelper
{
    public static string Build(string? qty, string? token, string? prises, string? periode)
        => $"{(qty ?? string.Empty).Trim()} {(token ?? string.Empty).Trim()} x {(prises ?? string.Empty).Trim()} / {(periode ?? string.Empty).Trim()}"
            .Trim();
}
