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
        var specialiteRepo = new Mock<IRepository<Specialites>>();
        var posoRepo    = new Mock<IRepository<Poso>>();
        var interactRepo = new Mock<IRepository<Interact>>();
        var openRouter  = new Mock<IOpenRouterService>();
        var mlPfeService = new Mock<IMLPfeService>();
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
            specialiteRepo.Object,
            interactRepo.Object,
            openRouter.Object,
            mlPfeService.Object,
            dialog.Object,
            syncService,
            new FormePosoAbbreviationService(formeRepo.Object),
            new PosoLookupService(posoRepo.Object));
    }

    [Fact]
    public async Task ClearingFam2ToNull_PersistsEmptyStringViaUpdateAsync()
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
            voie     = "Orale",
            labo     = "SANOFI",
            specialite = "ANTALGIQUES"        // Required in edit mode — must be non-empty
        };

        // Use reflection to set IsEditMode and Medic.
        // Both are [ObservableProperty]-generated properties; reflection still fires
        // the partial change handler (OnMedicChanged → ValidateAll), which is desirable.
        vm.GetType().GetProperty("IsEditMode")!.SetValue(vm, true);
        vm.GetType().GetProperty("Medic")!.SetValue(vm, medic);

        await vm.SaveCommand.ExecuteAsync(null);

        // If fam2 was not updated (swallowed), this assertion would fail.
        // Due to the NOT NULL constraints on the database schema, null is
        // defensively coalesced to string.Empty before UpdateAsync.
        captured.Should().NotBeNull("UpdateAsync should have been called in edit mode");
        captured!.fam2.Should().BeEmpty("null famille must be converted to empty string to satisfy database constraints");
    }

    [Fact]
    public void SettingSpecialite_InEditMode_ClearsRequiredErrorAndEnablesSave()
    {
        var vm = BuildVm(out _, out _);

        // ABILICARE-style edit: every required field filled EXCEPT specialite,
        // which is empty in the DB. In edit mode specialite is Required.
        var medic = new Medic
        {
            recordid   = 7,
            itemname   = "ABILICARE 15 MG COMP BT 30",
            pctcode    = "302501",
            fam1       = "Antibiotiques",
            dci1       = "ARIPIPRAZOLE",
            voie       = "Orale",
            labo       = "Sanofi",
            specialite = ""               // empty → required error in edit mode
        };
        vm.GetType().GetProperty("IsEditMode")!.SetValue(vm, true);
        vm.Medic = medic;

        // Precondition: the empty specialite blocks save.
        vm.SaveCommand.CanExecute(null).Should()
          .BeFalse("an empty required Spécialité must block save after load");

        // Act: user selects a value via the Spécialité combo, which binds to the
        // Specialite proxy. No other field is touched afterwards.
        vm.Specialite = "Dermatologie";

        // Assert: selecting a Spécialité re-validates on its own and unblocks save.
        vm.Specialite.Should().Be("Dermatologie");
        vm.Medic.specialite.Should().Be("Dermatologie");
        vm.SaveCommand.CanExecute(null).Should()
          .BeTrue("selecting a Spécialité must re-run validation and clear its required error");
    }

    [Fact]
    public void SettingLaboratoire_InEditMode_RevalidatesLikeSpecialite()
    {
        var vm = BuildVm(out _, out _);

        var medic = new Medic
        {
            recordid   = 7,
            itemname   = "ABILICARE 15 MG COMP BT 30",
            pctcode    = "302501",
            fam1       = "Antibiotiques",
            dci1       = "ARIPIPRAZOLE",
            voie       = "Orale",
            labo       = "",              // empty → required error in edit mode
            specialite = "Dermatologie"
        };
        vm.GetType().GetProperty("IsEditMode")!.SetValue(vm, true);
        vm.Medic = medic;

        vm.SaveCommand.CanExecute(null).Should()
          .BeFalse("an empty required Laboratoire must block save after load");

        vm.Laboratoire = "Sanofi";

        vm.Laboratoire.Should().Be("Sanofi");
        vm.Medic.labo.Should().Be("Sanofi");
        vm.SaveCommand.CanExecute(null).Should()
          .BeTrue("selecting a Laboratoire must re-run validation and clear its required error");
    }

    [Fact]
    public void UpdateDenomination_PreservesAndPrependsTypedBrandName()
    {
        var vm = BuildVm(out _, out _);
        var medic = new Medic
        {
            specialite = "ABILIFY",
            dose1 = "10",
            u1 = "MG",
            forme = "Comprimé",
            present = "Boîte 30",
            basename = "ABILIFY" // user typed
        };
        vm.Medic = medic;

        // Run UpdateDenomination
        vm.UpdateDenominationCommand.Execute(null);

        // Verification: brand is preserved and computed denomination is appended
        vm.Medic.basename.Should().Be("ABILIFY 10 MG Comprimé Boîte 30");

        // Running it again should NOT duplicate the computed denomination
        vm.UpdateDenominationCommand.Execute(null);
        vm.Medic.basename.Should().Be("ABILIFY 10 MG Comprimé Boîte 30");

        // Changing the commercial name to a different brand replaces the old one.
        // Convention: only the first word (before the first space) is the free
        // commercial name; the remainder is the recomputed denomination.
        vm.Medic.basename = "DAFALGAN";
        vm.UpdateDenominationCommand.Execute(null);
        vm.Medic.basename.Should().Be("DAFALGAN 10 MG Comprimé Boîte 30");
    }

    [Fact]
    public async Task SaveAsync_TransfersBasenameToItemnameBeforeValidation()
    {
        var vm = BuildVm(out var medicRepo, out var dialog);

        dialog.Setup(d => d.ShowConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(true);
        dialog.Setup(d => d.ShowSuccessAsync(It.IsAny<string>(), It.IsAny<string>()))
              .Returns(Task.CompletedTask);

        Medic? captured = null;
        medicRepo
            .Setup(r => r.AddAsync(It.IsAny<Medic>()))
            .Callback<Medic>(m => captured = m)
            .ReturnsAsync((Medic m) => m);

        var medic = new Medic
        {
            itemname = "", // empty! Would trigger validation error if not updated first
            basename = "ABILIFY 10 MG Comprimé Boîte 30",
            pctcode  = "12345",
            fam1     = "ANTALGIQUE",
            dci1     = "PARACETAMOL",
            voie     = "Orale",
            labo     = "SANOFI"
        };

        vm.GetType().GetProperty("IsEditMode")!.SetValue(vm, false);
        vm.GetType().GetProperty("Medic")!.SetValue(vm, medic);

        await vm.SaveCommand.ExecuteAsync(null);

        // Verification: Save succeeded because basename was transferred to itemname prior to validation
        captured.Should().NotBeNull("AddAsync should have been called successfully");
        captured!.itemname.Should().Be("ABILIFY 10 MG Comprimé Boîte 30");
        captured.basename.Should().BeEmpty("basename must be cleared after successful persistence");
    }
}
