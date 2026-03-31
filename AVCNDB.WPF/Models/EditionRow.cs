using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Flag d'action pour chaque ligne du fichier d'édition
/// </summary>
public enum ActionFlag
{
    None = 0,
    AjouterNew = 1,
    MarquerSuppression = 2,
    Nouveau = 3,
    Reinitialiser = 4,
    Affecte = 5,
    Desaffecte = 6
}

/// <summary>
/// Statut de la ligne dans le fichier d'édition
/// </summary>
public enum RowStatus
{
    Active = 0,
    Inactive = 1,
    New = 2,
    Modified = 3
}

/// <summary>
/// Modèle in-memory pour une ligne du fichier d'édition.
/// Miroir des colonnes de la table Medic + champs d'édition supplémentaires.
/// NON persisté en base de données.
/// </summary>
public class EditionRow : INotifyPropertyChanged
{
    // ============================================
    // IDENTIFICATION
    // ============================================
    private int _lineNumber;
    public int LineNumber
    {
        get => _lineNumber;
        set => SetField(ref _lineNumber, value);
    }

    private string _medicId = string.Empty;
    public string MedicId
    {
        get => _medicId;
        set => SetField(ref _medicId, value);
    }

    private string _pctCode = string.Empty;
    public string PctCode
    {
        get => _pctCode;
        set => SetField(ref _pctCode, value);
    }

    // ============================================
    // DÉNOMINATION
    // ============================================
    private string _itemName = string.Empty;
    public string ItemName
    {
        get => _itemName;
        set => SetField(ref _itemName, value);
    }

    private string _shortName = string.Empty;
    public string ShortName
    {
        get => _shortName;
        set => SetField(ref _shortName, value);
    }

    // ============================================
    // COMPOSITION (DCI)
    // ============================================
    private string _dci1 = string.Empty;
    public string Dci1
    {
        get => _dci1;
        set => SetField(ref _dci1, value);
    }

    private string _dci2 = string.Empty;
    public string Dci2
    {
        get => _dci2;
        set => SetField(ref _dci2, value);
    }

    private string _dci3 = string.Empty;
    public string Dci3
    {
        get => _dci3;
        set => SetField(ref _dci3, value);
    }

    private string _dci4 = string.Empty;
    public string Dci4
    {
        get => _dci4;
        set => SetField(ref _dci4, value);
    }

    private string _dciAssociation = string.Empty;
    public string DciAssociation
    {
        get => _dciAssociation;
        set => SetField(ref _dciAssociation, value);
    }

    // ============================================
    // FORME & VOIE
    // ============================================
    private string _forme = string.Empty;
    public string Forme
    {
        get => _forme;
        set => SetField(ref _forme, value);
    }

    private string _voie = string.Empty;
    public string Voie
    {
        get => _voie;
        set => SetField(ref _voie, value);
    }

    // ============================================
    // CLASSIFICATION
    // ============================================
    private string _tableau = string.Empty;
    public string Tableau
    {
        get => _tableau;
        set => SetField(ref _tableau, value);
    }

    private string _veic = string.Empty;
    public string Veic
    {
        get => _veic;
        set => SetField(ref _veic, value);
    }

    private string _labo = string.Empty;
    public string Labo
    {
        get => _labo;
        set => SetField(ref _labo, value);
    }

    private string _fam1 = string.Empty;
    public string Fam1
    {
        get => _fam1;
        set => SetField(ref _fam1, value);
    }

    private string _fam2 = string.Empty;
    public string Fam2
    {
        get => _fam2;
        set => SetField(ref _fam2, value);
    }

    private string _fam3 = string.Empty;
    public string Fam3
    {
        get => _fam3;
        set => SetField(ref _fam3, value);
    }

    private string _specialite = string.Empty;
    public string Specialite
    {
        get => _specialite;
        set => SetField(ref _specialite, value);
    }

    // ============================================
    // TARIFICATION
    // ============================================
    private int _refPrice;
    public int RefPrice
    {
        get => _refPrice;
        set => SetField(ref _refPrice, value);
    }

    private int _price;
    public int Price
    {
        get => _price;
        set => SetField(ref _price, value);
    }

    private int _isAp;
    public int IsAp
    {
        get => _isAp;
        set => SetField(ref _isAp, value);
    }

    private int _isRemboursable;
    public int IsRemboursable
    {
        get => _isRemboursable;
        set => SetField(ref _isRemboursable, value);
    }

    // ============================================
    // ÉDITION — Champs supplémentaires
    // ============================================
    private ActionFlag _actionFlag = ActionFlag.None;
    public ActionFlag ActionFlag
    {
        get => _actionFlag;
        set
        {
            if (SetField(ref _actionFlag, value))
                OnPropertyChanged(nameof(ActionLabel));
        }
    }

    private RowStatus _rowStatus = RowStatus.Active;
    public RowStatus RowStatus
    {
        get => _rowStatus;
        set => SetField(ref _rowStatus, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>
    /// Liste des noms de champs détectés comme inconnus par le moteur ML
    /// </summary>
    public List<string> UnknownFields { get; set; } = new();

    /// <summary>
    /// Indique si la ligne contient au moins un champ inconnu
    /// </summary>
    public bool HasUnknownFields => UnknownFields.Count > 0;

    /// <summary>
    /// Libellé de l'action affiché dans la colonne Action du DataGrid
    /// </summary>
    public string ActionLabel => ActionFlag switch
    {
        ActionFlag.AjouterNew => "A ajouter",
        ActionFlag.MarquerSuppression => "Suppression",
        ActionFlag.Nouveau => "Nouveau",
        ActionFlag.Reinitialiser => "Réinitialisé",
        ActionFlag.Affecte => "Affecté",
        ActionFlag.Desaffecte => "Désaffecté",
        _ => string.Empty
    };

    /// <summary>
    /// Référence au recordid du Medic correspondant (si existant en base)
    /// </summary>
    public int? OriginalMedicRecordId { get; set; }

    /// <summary>
    /// Indique si le prix a changé par rapport à la base
    /// </summary>
    private bool _hasPriceChanged;
    public bool HasPriceChanged
    {
        get => _hasPriceChanged;
        set => SetField(ref _hasPriceChanged, value);
    }

    // ============================================
    // INotifyPropertyChanged
    // ============================================
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Notifie le changement de HasUnknownFields (à appeler après modification de UnknownFields)
    /// </summary>
    public void NotifyUnknownFieldsChanged()
    {
        OnPropertyChanged(nameof(UnknownFields));
        OnPropertyChanged(nameof(HasUnknownFields));
    }
}
