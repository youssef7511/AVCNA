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

    // ─── Pass-through properties (for DataGrid column binding) ────────────

    public int LineNumber        => Row.LineNumber;
    public string MedicId        => Row.MedicId;
    public string PctCode        => Row.PctCode;
    public string ItemName       => Row.ItemName;
    public string ShortName      => Row.ShortName;
    public string Dci1           => Row.Dci1;
    public string Dci2           => Row.Dci2;
    public string Dci3           => Row.Dci3;
    public string Dci4           => Row.Dci4;
    public string DciAssociation => Row.DciAssociation;
    public string Forme          => Row.Forme;
    public string Voie           => Row.Voie;
    public string Tableau        => Row.Tableau;
    public string Veic           => Row.Veic;
    public string Labo           => Row.Labo;
    public string Fam1           => Row.Fam1;
    public string Fam2           => Row.Fam2;
    public string Fam3           => Row.Fam3;
    public string Specialite     => Row.Specialite;
    public int RefPrice          => Row.RefPrice;
    public int Price             => Row.Price;
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
