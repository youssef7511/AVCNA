using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Tests.ViewModels;

/// <summary>
/// Pins the contract that clearing a famille field to null on a Medic
/// flows through SaveAsync into IRepository&lt;Medic&gt;.UpdateAsync without
/// being filtered, defaulted, or swallowed. Acts as a regression guard
/// for the FilterableComboBox clear-on-empty fix.
///
/// Field chosen: fam2 — ValidateAll() has no Required rule for fam2,
/// so setting it to null does not block the save path.
/// fam1 is equally non-required, but fam2 is the more typical "secondary
/// family" field that a user would clear via the combobox.
/// </summary>
public class MedicUpsertDialogClearFieldTests
{
    private static MedicUpsertDialogViewModel BuildVm(
        out Mock<IRepository<Medic>> medicRepo,
        out Mock<IDialogService> dialog)
    {
        medicRepo = new Mock<IRepository<Medic>>();

        var familyRepo  = new Mock<IRepository<Families>>();
        var laboRepo    = new Mock<IRepository<Labos>>();
        var dciRepo     = new Mock<IRepository<Dci>>();
        var formeRepo   = new Mock<IRepository<Formes>>();
        var presentRepo = new Mock<IRepository<Presents>>();
        var voieRepo    = new Mock<IRepository<Voies>>();
        var catveicRepo = new Mock<IRepository<Catveic>>();
        var interactRepo = new Mock<IRepository<Interact>>();
        var openRouter  = new Mock<IOpenRouterService>();
        dialog          = new Mock<IDialogService>();

        // MedicSyncService requires IDbContextFactory<AppDbContext> + ILogger.
        // We use a mock factory whose CreateDbContextAsync will throw (no real DB),
        // but SaveAsync swallows the sync error in: try { await _syncService... } catch { }
        var dbContextFactory = new Mock<IDbContextFactory<AppDbContext>>();
        dbContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("test-no-db"));

        var syncService = new MedicSyncService(
            dbContextFactory.Object,
            NullLogger<MedicSyncService>.Instance);

        return new MedicUpsertDialogViewModel(
            medicRepo.Object,
            familyRepo.Object,
            laboRepo.Object,
            dciRepo.Object,
            formeRepo.Object,
            presentRepo.Object,
            voieRepo.Object,
            catveicRepo.Object,
            interactRepo.Object,
            openRouter.Object,
            dialog.Object,
            syncService);
    }

    [Fact]
    public async Task ClearingFam2ToNull_PersistsNullViaUpdateAsync()
    {
        var vm = BuildVm(out var medicRepo, out var dialog);

        // Confirm dialogs return true so SaveAsync proceeds past the prompt.
        dialog.Setup(d => d.ShowConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(true);
        dialog.Setup(d => d.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(Task.CompletedTask);
        dialog.Setup(d => d.ShowWarningAsync(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(Task.CompletedTask);

        Medic? captured = null;
        medicRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Medic>()))
            .Callback<Medic>(m => captured = m)
            .Returns(Task.CompletedTask);

        // Arrange a Medic in edit mode with fam2 originally set, then cleared.
        // This is what the fixed FilterableComboBox will produce:
        // SelectedValue = null pushed into the bound property.
        // fam2 has no Required rule in ValidateAll, so null passes validation.
        var medic = new Medic
        {
            recordid = 7,
            itemname = "DOLIPRANE 500MG",   // Required — must be non-empty
            pctcode  = "12345",
            fam1     = "ANTALGIQUE",
            fam2     = null,                  // cleared — the value the fixed control produces
            dci1     = "PARACETAMOL",         // Required — must be non-empty
            voie     = "Orale"
        };

        // Use reflection to set IsEditMode and Medic.
        // Both are [ObservableProperty]-generated properties; reflection still fires
        // the partial change handler (OnMedicChanged → ValidateAll), which is desirable.
        vm.GetType().GetProperty("IsEditMode")!.SetValue(vm, true);
        vm.GetType().GetProperty("Medic")!.SetValue(vm, medic);

        await vm.SaveCommand.ExecuteAsync(null);

        // If fam2 null were silently swallowed (e.g., defaulted to string.Empty
        // before UpdateAsync), this assertion would fail — pinning the regression.
        captured.Should().NotBeNull("UpdateAsync should have been called in edit mode");
        captured!.fam2.Should().BeNull("null famille must reach the repository unchanged");
    }
}
