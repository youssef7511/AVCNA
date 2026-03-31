using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour afficher les détails d'un médicament
/// Fiche médicament complète et esthétique
/// </summary>
public partial class MedicDetailViewModel : ViewModelBase
{
    private readonly IRepository<Medic> _repository;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IPdfService _pdfService;

    [ObservableProperty]
    private Medic? _medic;

    [ObservableProperty]
    private bool _isPediatric;

    [ObservableProperty]
    private bool _isGeneric;

    [ObservableProperty]
    private bool _isControlled;

    [ObservableProperty]
    private bool _isReimbursable;

    [ObservableProperty]
    private string _formattedPrice = "—";

    [ObservableProperty]
    private string _formattedRefPrice = "—";

    [ObservableProperty]
    private string _formattedPctPrice = "—";

    [ObservableProperty]
    private string _formattedNetPrice = "—";

    [ObservableProperty]
    private string _veicDescription = string.Empty;

    [ObservableProperty]
    private bool _hasNotes;

    [ObservableProperty]
    private bool _hasDci2;

    [ObservableProperty]
    private bool _hasDci3;

    [ObservableProperty]
    private bool _hasDci4;

    [ObservableProperty]
    private string _statusText = "Actif";

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private string _formattedTimbre = "—";

    [ObservableProperty]
    private string _formattedPamount = "—";

    [ObservableProperty]
    private bool _hasPosology;

    [ObservableProperty]
    private bool _isOtc;

    [ObservableProperty]
    private bool _hasSpecialite;

    [ObservableProperty]
    private string _formattedAddedDate = "—";

    [ObservableProperty]
    private string _formattedUpdatedDate = "—";

    public MedicDetailViewModel(
        IRepository<Medic> repository,
        INavigationService navigationService,
        IDialogService dialogService,
        IPdfService pdfService)
    {
        _repository = repository;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _pdfService = pdfService;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        if (parameter is int medicId)
        {
            _ = LoadMedicAsync(medicId);
        }
    }

    public override void OnNavigatedFrom() { }

    private async Task LoadMedicAsync(int medicId)
    {
        await ExecuteAsync(async () =>
        {
            Medic = await _repository.GetByIdAsync(medicId);
            
            if (Medic != null)
            {
                // Flags
                IsPediatric = Medic.pediatric == 1;
                IsGeneric = Medic.isap == 1;
                IsControlled = !string.IsNullOrEmpty(Medic.tableau) && Medic.tableau != "0";
                IsReimbursable = Medic.isic == 1;
                IsActive = Medic.isactive == 1;
                StatusText = IsActive ? "Actif" : "Inactif";

                // Prix formatés (stockés en millièmes)
                FormattedPrice = Medic.price > 0 ? $"{Medic.price / 1000.0:N3} DT" : "—";
                FormattedRefPrice = Medic.refprice > 0 ? $"{Medic.refprice / 1000.0:N3} DT" : "—";
                FormattedPctPrice = Medic.pctprice > 0 ? $"{Medic.pctprice / 1000.0:N3} DT" : "—";
                FormattedNetPrice = Medic.netprice > 0 ? $"{Medic.netprice / 1000.0:N3} DT" : "—";

                // DCI composition flags
                HasDci2 = !string.IsNullOrWhiteSpace(Medic.dci2);
                HasDci3 = !string.IsNullOrWhiteSpace(Medic.dci3);
                HasDci4 = !string.IsNullOrWhiteSpace(Medic.dci4);

                // Conduite
                VeicDescription = GetVeicDescription(Medic.veic);
                HasNotes = !string.IsNullOrWhiteSpace(Medic.indication);
                HasPosology = !string.IsNullOrWhiteSpace(Medic.posology);
                IsOtc = Medic.isotc == 1;
                HasSpecialite = !string.IsNullOrWhiteSpace(Medic.specialite);

                // Prix supplémentaires
                FormattedTimbre = Medic.timbrepct > 0 ? $"{Medic.timbrepct / 1000.0:N3} DT" : "—";
                FormattedPamount = Medic.pamount > 0 ? $"{Medic.pamount / 1000.0:N3} DT" : "—";

                // Dates
                FormattedAddedDate = Medic.addedat?.ToString("dd/MM/yyyy") ?? "—";
                FormattedUpdatedDate = Medic.updatedat?.ToString("dd/MM/yyyy") ?? "—";
            }
        }, "Chargement de la fiche...");
    }

    private static string GetVeicDescription(string veic)
    {
        return veic?.ToUpper() switch
        {
            "0" or "" or null => "Pas de restrictions",
            "1" => "⚠️ Niveau 1 - Vigilance requise",
            "2" => "⚠️ Niveau 2 - Vigilance accrue",
            "3" => "🚫 Niveau 3 - Incompatible avec la conduite",
            _ => veic
        };
    }

    [RelayCommand]
    private async Task Edit()
    {
        if (Medic == null) return;

        try
        {
            _navigationService.NavigateTo<MedicEditViewModel>(Medic.recordid);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Erreur",
                $"Impossible d'ouvrir le formulaire d'édition du médicament.\n\n{ex.Message}");
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        if (Medic == null) return;

        var filePath = _dialogService.ShowSaveFileDialog(
            "PDF Files|*.pdf",
            $"Fiche_{Medic.itemname.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}",
            "Exporter la fiche PDF");

        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                await _pdfService.GenerateMedicReportAsync(Medic.recordid, filePath);

                if (File.Exists(filePath))
                {
                    await _dialogService.ShowSuccessAsync("Export réussi", 
                        $"Fiche médicament exportée vers :\n{filePath}");
                }
                else
                {
                    await _dialogService.ShowErrorAsync("Erreur",
                        "Le fichier PDF n'a pas pu être créé. Vérifiez les permissions du dossier.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Erreur d'export PDF",
                    $"Impossible de générer le PDF :\n{ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (Medic == null) return;

        var tempPath = Path.Combine(Path.GetTempPath(), $"Fiche_{Medic.recordid}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        
        try
        {
            await _pdfService.GenerateMedicReportAsync(Medic.recordid, tempPath);
            
            if (File.Exists(tempPath))
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(processInfo);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Erreur",
                    "Impossible de générer le PDF pour l'impression.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Erreur d'impression",
                $"Impossible de préparer l'impression :\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CopyToClipboardAsync()
    {
        if (Medic == null) return;

        var text = $"""
            NOM: {Medic.itemname}
            DCI: {Medic.dci}
            FORME: {Medic.forme}
            VOIE: {Medic.voie}
            PRÉSENTATION: {Medic.present}
            LABORATOIRE: {Medic.labo}
            PRIX: {FormattedPrice}
            """;

        System.Windows.Clipboard.SetText(text);
        await _dialogService.ShowInfoAsync("Copié", "Informations copiées dans le presse-papiers.");
    }
}
