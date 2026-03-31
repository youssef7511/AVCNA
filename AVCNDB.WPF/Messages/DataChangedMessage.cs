using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AVCNDB.WPF.Messages;

/// <summary>
/// Message diffusé via WeakReferenceMessenger lorsqu'une donnée est modifiée.
/// Permet aux ViewModels de savoir qu'ils doivent se rafraîchir.
/// </summary>
public sealed class DataChangedMessage : ValueChangedMessage<DataChangeInfo>
{
    public DataChangedMessage(DataChangeInfo value) : base(value) { }
}

/// <summary>
/// Détails sur le changement de données.
/// </summary>
public sealed record DataChangeInfo(
    /// <summary>Type d'entité modifiée (ex: "Medic", "Dci", "Formes", etc.)</summary>
    string EntityType,
    /// <summary>Type d'opération : Created, Updated, Deleted, Renamed</summary>
    ChangeOperation Operation,
    /// <summary>Identifiant optionnel de l'entité concernée</summary>
    int? EntityId = null
);

public enum ChangeOperation
{
    Created,
    Updated,
    Deleted,
    Renamed,
    BulkUpdated
}
