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
    private bool _isControlled;

    [ObservableProperty]
    private string _formattedPrice = "0.000 DT";

    [ObservableProperty]
    private string _veicDescription = string.Empty;

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
                IsPediatric = Medic.pediatric == 1;
                IsControlled = !string.IsNullOrEmpty(Medic.tableau);
                FormattedPrice = $"{Medic.price / 1000.0:N3} DT";
                VeicDescription = GetVeicDescription(Medic.veic);
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
    private void Edit()
    {
        if (Medic != null)
        {
            _navigationService.NavigateTo<MedicEditViewModel>(Medic.recordid);
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
            await ExecuteAsync(async () =>
            {
                await _pdfService.GenerateMedicReportAsync(Medic.recordid, filePath);
                await _dialogService.ShowSuccessAsync("Export réussi", 
                    $"Fiche médicament exportée vers :\n{filePath}");
            }, "Génération du PDF...");
        }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (Medic == null) return;

        // Génération temporaire puis ouverture pour impression
        var tempPath = Path.Combine(Path.GetTempPath(), $"Fiche_{Medic.recordid}.pdf");
        
        await ExecuteAsync(async () =>
        {
            await _pdfService.GenerateMedicReportAsync(Medic.recordid, tempPath);
            
            // Ouvrir le PDF dans le lecteur par défaut
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processInfo);
        }, "Préparation de l'impression...");
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
