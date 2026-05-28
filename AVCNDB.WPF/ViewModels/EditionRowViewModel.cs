using CommunityToolkit.Mvvm.ComponentModel;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// Observable wrapper around EditionRow.
/// Exposes per-field IsUnknown_* booleans for XAML DataTrigger cell coloring (D-03).
/// </summary>
public partial class EditionRowViewModel : ObservableObject
{
    public EditionRow Row { get; }

    /// <summary>
    /// Callback de re-validation : (nom du champ de détection, nouvelle valeur) → la valeur est-elle connue ?
    /// Fourni par le ViewModel parent. Null = pas de re-validation (ex: tests).
    /// </summary>
    private readonly Func<string, string, Task<bool>>? _isFieldKnownAsync;

    public EditionRowViewModel(EditionRow row, Func<string, string, Task<bool>>? isFieldKnownAsync = null)
    {
        Row = row;
        _isFieldKnownAsync = isFieldKnownAsync;
        row.PropertyChanged += (_, e) =>
        {
            // Bubble property changes from the underlying EditionRow
            OnPropertyChanged(e.PropertyName);

            // Refresh all unknown-field indicators when UnknownFields list changes
            if (e.PropertyName is nameof(EditionRow.UnknownFields)
                              or nameof(EditionRow.HasUnknownFields)
                              or nameof(EditionRow.ActionFlag))
            {
                OnPropertyChanged(nameof(IsUnknown_Dci));
                OnPropertyChanged(nameof(IsUnknown_DciAssociation));
                OnPropertyChanged(nameof(IsUnknown_Labo));
                OnPropertyChanged(nameof(IsUnknown_Forme));
                OnPropertyChanged(nameof(IsUnknown_Fam1));
                OnPropertyChanged(nameof(IsUnknown_Fam2));
                OnPropertyChanged(nameof(IsUnknown_Fam3));
                OnPropertyChanged(nameof(IsUnknown_Voie));
                OnPropertyChanged(nameof(IsUnknown_Specialite));
                OnPropertyChanged(nameof(ActionLabel));
                OnPropertyChanged(nameof(HasUnknownFields));
            }
        };
    }

    // ─── Dirty tracking ──────────────────────────────────────────────────

    private bool _isModified;
    public bool IsModified
    {
        get => _isModified;
        private set { _isModified = value; OnPropertyChanged(); }
    }

    // ─── Re-validation on inline edit ────────────────────────────────────
    //
    // Au moment de l'import, la détection floue marque les champs inconnus
    // (coloration bleue). Sans re-validation, sélectionner une valeur valide
    // dans le combo laisserait l'alerte figée. On re-vérifie donc le champ
    // édité contre la bibliothèque et on met à jour Row.UnknownFields en direct.

    /// <summary>
    /// Re-vérifie un champ de référence après édition manuelle et efface (ou pose)
    /// l'indicateur « inconnu » en conséquence. async void : déclenché depuis un
    /// setter (UI thread) ; la bibliothèque est mise en cache, donc l'appel est rapide.
    /// </summary>
    private async void RevalidateField(string detectionFieldName, string? value)
    {
        if (_isFieldKnownAsync == null) return;

        bool known;
        try { known = await _isFieldKnownAsync(detectionFieldName, value ?? string.Empty); }
        catch { return; } // re-validation best-effort : ne jamais bloquer l'édition

        bool changed;
        if (known)
        {
            changed = Row.UnknownFields.Remove(detectionFieldName);
        }
        else if (!Row.UnknownFields.Contains(detectionFieldName))
        {
            Row.UnknownFields.Add(detectionFieldName);
            changed = true;
        }
        else
        {
            changed = false;
        }

        if (changed) Row.NotifyUnknownFieldsChanged();
    }

    // ─── Read-only properties ────────────────────────────────────────────

    public int LineNumber        => Row.LineNumber;
    public string MedicId        => Row.MedicId;

    // ─── Editable properties (for DataGrid inline editing) ───────────────

    public string PctCode
    {
        get => Row.PctCode;
        set { Row.PctCode = value; IsModified = true; }
    }

    public string ItemName
    {
        get => Row.ItemName;
        set { Row.ItemName = value; IsModified = true; }
    }

    public string ShortName
    {
        get => Row.ShortName;
        set { Row.ShortName = value; IsModified = true; }
    }

    public string Dci1
    {
        get => Row.Dci1;
        set { Row.Dci1 = value; IsModified = true; RevalidateField("Dci", value); }
    }

    public string Dci2
    {
        get => Row.Dci2;
        set { Row.Dci2 = value; IsModified = true; }
    }

    public string Dci3
    {
        get => Row.Dci3;
        set { Row.Dci3 = value; IsModified = true; }
    }

    public string Dci4
    {
        get => Row.Dci4;
        set { Row.Dci4 = value; IsModified = true; }
    }

    public string DciAssociation
    {
        get => Row.DciAssociation;
        set { Row.DciAssociation = value; IsModified = true; RevalidateField("DciAssociation", value); }
    }

    public string Forme
    {
        get => Row.Forme;
        set { Row.Forme = value; IsModified = true; RevalidateField("Forme", value); }
    }

    public string Voie
    {
        get => Row.Voie;
        set { Row.Voie = value; IsModified = true; RevalidateField("Voie", value); }
    }

    public string Tableau
    {
        get => Row.Tableau;
        set { Row.Tableau = value; IsModified = true; }
    }

    public string Veic
    {
        get => Row.Veic;
        set { Row.Veic = value; IsModified = true; }
    }

    public string Labo
    {
        get => Row.Labo;
        set { Row.Labo = value; IsModified = true; RevalidateField("Labo", value); }
    }

    public string Fam1
    {
        get => Row.Fam1;
        set { Row.Fam1 = value; IsModified = true; RevalidateField("Fam1", value); }
    }

    public string Fam2
    {
        get => Row.Fam2;
        set { Row.Fam2 = value; IsModified = true; RevalidateField("Fam2", value); }
    }

    public string Fam3
    {
        get => Row.Fam3;
        set { Row.Fam3 = value; IsModified = true; RevalidateField("Fam3", value); }
    }

    public string Specialite
    {
        get => Row.Specialite;
        set { Row.Specialite = value; IsModified = true; RevalidateField("Specialite", value); }
    }

    public int RefPrice
    {
        get => Row.RefPrice;
        set { Row.RefPrice = value; IsModified = true; }
    }

    public int Price
    {
        get => Row.Price;
        set { Row.Price = value; IsModified = true; }
    }

    public int IsAp              => Row.IsAp;
    public int IsRemboursable    => Row.IsRemboursable;

    public ActionFlag ActionFlag   => Row.ActionFlag;
    public RowStatus RowStatus     => Row.RowStatus;
    public string ActionLabel      => Row.ActionLabel;
    public bool HasUnknownFields   => Row.HasUnknownFields;
    public bool HasPriceChanged    => Row.HasPriceChanged;
    // True when this row was matched to an existing Medic by PctCode.
    // Used to hide Approuver/Rejeter buttons — existing drugs need no approval.
    public bool IsExistingMedic    => Row.OriginalMedicRecordId.HasValue;
    public bool IsNewMedic         => !Row.OriginalMedicRecordId.HasValue;

    public bool IsSelected
    {
        get => Row.IsSelected;
        set { Row.IsSelected = value; OnPropertyChanged(); }
    }

    // ─── Per-field "IsUnknown" booleans for XAML DataTrigger (D-03) ──────

    public bool IsUnknown_Dci            => Row.UnknownFields.Contains("Dci");
    public bool IsUnknown_DciAssociation => Row.UnknownFields.Contains("DciAssociation");
    public bool IsUnknown_Labo           => Row.UnknownFields.Contains("Labo");
    public bool IsUnknown_Forme          => Row.UnknownFields.Contains("Forme");
    public bool IsUnknown_Fam1           => Row.UnknownFields.Contains("Fam1");
    public bool IsUnknown_Fam2           => Row.UnknownFields.Contains("Fam2");
    public bool IsUnknown_Fam3           => Row.UnknownFields.Contains("Fam3");
    public bool IsUnknown_Voie           => Row.UnknownFields.Contains("Voie");
    public bool IsUnknown_Specialite     => Row.UnknownFields.Contains("Specialite");
}
