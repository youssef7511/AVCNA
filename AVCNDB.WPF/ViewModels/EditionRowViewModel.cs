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

    public EditionRowViewModel(EditionRow row)
    {
        Row = row;
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
        set { Row.Dci1 = value; IsModified = true; }
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
        set { Row.DciAssociation = value; IsModified = true; }
    }

    public string Forme
    {
        get => Row.Forme;
        set { Row.Forme = value; IsModified = true; }
    }

    public string Voie
    {
        get => Row.Voie;
        set { Row.Voie = value; IsModified = true; }
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
        set { Row.Labo = value; IsModified = true; }
    }

    public string Fam1
    {
        get => Row.Fam1;
        set { Row.Fam1 = value; IsModified = true; }
    }

    public string Fam2
    {
        get => Row.Fam2;
        set { Row.Fam2 = value; IsModified = true; }
    }

    public string Fam3
    {
        get => Row.Fam3;
        set { Row.Fam3 = value; IsModified = true; }
    }

    public string Specialite
    {
        get => Row.Specialite;
        set { Row.Specialite = value; IsModified = true; }
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
