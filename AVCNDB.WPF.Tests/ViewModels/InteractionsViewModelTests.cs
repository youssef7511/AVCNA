using System.Collections.ObjectModel;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Tests.ViewModels;

public class InteractionsViewModelTests
{
    private static InteractionsViewModel BuildVm(
        out Mock<IRepository<Interact>> interactRepo,
        out Mock<IRepository<Medic>> medicRepo,
        out Mock<IOpenRouterService> openRouter,
        out Mock<IDialogService> dialog,
        out Mock<IPdfService> pdf,
        out Mock<IMLPfeService> mlPfe)
    {
        interactRepo = new Mock<IRepository<Interact>>();
        medicRepo    = new Mock<IRepository<Medic>>();
        openRouter   = new Mock<IOpenRouterService>();
        dialog       = new Mock<IDialogService>();
        pdf          = new Mock<IPdfService>();
        mlPfe        = new Mock<IMLPfeService>();

        return new InteractionsViewModel(
            interactRepo.Object,
            medicRepo.Object,
            openRouter.Object,
            dialog.Object,
            pdf.Object,
            mlPfe.Object);
    }

    [Fact]
    public void Creation_SetsCleanInitialState()
    {
        var vm = BuildVm(out _, out _, out _, out _, out _, out _);

        vm.SelectedDrugA.Should().BeNull();
        vm.SelectedDrugB.Should().BeNull();
        vm.LocalInteractions.Should().BeEmpty();
        vm.CanAnalyze.Should().BeFalse();
        vm.HasResults.Should().BeFalse();
        vm.NoResults.Should().BeTrue();
        vm.DeepAnalysisHasPendingResult.Should().BeFalse();
        vm.IsDeepAnalysisRunning.Should().BeFalse();

        vm.RunDrugASearchCommand.Should().NotBeNull();
        vm.RunDrugBSearchCommand.Should().NotBeNull();
        vm.AnalyzeWithAiCommand.Should().NotBeNull();
        vm.ApproveAndSaveInteractionCommand.Should().NotBeNull();
        vm.DiscardDeepAnalysisCommand.Should().NotBeNull();
        vm.ClearDrugACommand.Should().NotBeNull();
        vm.ClearDrugBCommand.Should().NotBeNull();
        vm.ExportPdfCommand.Should().NotBeNull();
        vm.LaunchMlPfeCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task DrugASearch_EmptyText_ProducesEmptyResults_AndDoesNotCallRepository()
    {
        var vm = BuildVm(out _, out var medicRepo, out _, out _, out _, out _);

        vm.DrugASearchText = "";
        await vm.RunDrugASearchCommand.ExecuteAsync(null);

        vm.DrugASearchResults.Should().BeEmpty();
        medicRepo.Verify(
            r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Medic, bool>>>()),
            Times.Never);
    }

    [Fact]
    public async Task DrugASearch_NonEmptyText_QueriesRepository_OrdersAndCaps_Results()
    {
        var vm = BuildVm(out _, out var medicRepo, out _, out _, out _, out _);

        var bulk = Enumerable.Range(1, 75).Select(i => new Medic
        {
            recordid = i,
            itemname = $"DRUG_{i:D3}",
            isactive = 1
        }).ToList();
        medicRepo
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Medic, bool>>>()))
            .ReturnsAsync(bulk);

        vm.DrugASearchText = "DRUG";
        await vm.RunDrugASearchCommand.ExecuteAsync(null);

        vm.DrugASearchResults.Count.Should().Be(50);
        vm.DrugASearchResults.First().itemname.Should().Be("DRUG_001");
        vm.DrugASearchResults.Last().itemname.Should().Be("DRUG_050");
    }

    [Fact]
    public async Task DrugBSearch_EmptyText_ProducesEmptyResults_AndDoesNotCallRepository()
    {
        var vm = BuildVm(out _, out var medicRepo, out _, out _, out _, out _);

        vm.DrugBSearchText = "";
        await vm.RunDrugBSearchCommand.ExecuteAsync(null);

        vm.DrugBSearchResults.Should().BeEmpty();
        medicRepo.Verify(
            r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Medic, bool>>>()),
            Times.Never);
    }

    [Fact]
    public async Task DrugBSearch_NonEmptyText_QueriesRepository_OrdersAndCaps_Results()
    {
        var vm = BuildVm(out _, out var medicRepo, out _, out _, out _, out _);

        var bulk = Enumerable.Range(1, 75).Select(i => new Medic
        {
            recordid = i,
            itemname = $"DRUG_{i:D3}",
            isactive = 1
        }).ToList();
        medicRepo
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Medic, bool>>>()))
            .ReturnsAsync(bulk);

        vm.DrugBSearchText = "DRUG";
        await vm.RunDrugBSearchCommand.ExecuteAsync(null);

        vm.DrugBSearchResults.Count.Should().Be(50);
        vm.DrugBSearchResults.First().itemname.Should().Be("DRUG_001");
        vm.DrugBSearchResults.Last().itemname.Should().Be("DRUG_050");
    }

    [Fact]
    public void ChangingSelectedDrugA_ClearsPendingAi_AndLocalInteractions()
    {
        var vm = BuildVm(out _, out _, out _, out _, out _, out _);
        // Seed dirty state.
        vm.LocalInteractions = new ObservableCollection<Interact> { new() { dci1 = "X", dci2 = "Y" } };
        vm.DeepAnalysisLevel = "Contre-indiqué";
        vm.DeepAnalysisHasPendingResult = true;

        vm.SelectedDrugA = new Medic { recordid = 1, dci1 = "AMOXICILLINE" };

        vm.LocalInteractions.Should().BeEmpty();
        vm.DeepAnalysisHasPendingResult.Should().BeFalse();
        vm.DeepAnalysisLevel.Should().BeEmpty();
    }

    [Fact]
    public void ChangingSelectedDrugB_ClearsPendingAi_AndLocalInteractions()
    {
        var vm = BuildVm(out _, out _, out _, out _, out _, out _);
        vm.LocalInteractions = new ObservableCollection<Interact> { new() { dci1 = "X", dci2 = "Y" } };
        vm.DeepAnalysisHasPendingResult = true;

        vm.SelectedDrugB = new Medic { recordid = 2, dci1 = "PARACETAMOL" };

        vm.LocalInteractions.Should().BeEmpty();
        vm.DeepAnalysisHasPendingResult.Should().BeFalse();
    }

    [Fact]
    public void SelectingSameDrugInBothSlots_ClearsDrugB_AndShowsWarning()
    {
        var vm = BuildVm(out _, out _, out _, out var dialog, out _, out _);
        var drug = new Medic { recordid = 42, itemname = "DOLIPRANE", dci1 = "PARACETAMOL" };

        vm.SelectedDrugA = drug;
        vm.SelectedDrugB = drug;

        vm.SelectedDrugB.Should().BeNull();
        dialog.Verify(d => d.ShowWarningAsync("Médicament identique",
            "Choisissez deux médicaments différents."), Times.Once);
    }

    [Fact]
    public void CanAnalyze_FollowsBothSlotsAndBusyFlag()
    {
        var vm = BuildVm(out _, out _, out _, out _, out _, out _);

        vm.CanAnalyze.Should().BeFalse();

        vm.SelectedDrugA = new Medic { recordid = 1, dci1 = "X" };
        vm.CanAnalyze.Should().BeFalse();

        vm.SelectedDrugB = new Medic { recordid = 2, dci1 = "Y" };
        vm.CanAnalyze.Should().BeTrue();

        vm.IsDeepAnalysisRunning = true;
        vm.CanAnalyze.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeWithAi_PopulatesLocalInteractions_AndPendingAi_OnSuccess()
    {
        var vm = BuildVm(out var interactRepo, out _, out var openRouter, out _, out _, out _);

        vm.SelectedDrugA = new Medic { recordid = 1, dci1 = "AMOXICILLINE", voie = "Orale" };
        vm.SelectedDrugB = new Medic { recordid = 2, dci1 = "METHOTREXATE", voie = "Orale" };

        var localRow = new Interact
        {
            recordid = 10,
            dci1  = "AMOXICILLINE", dci2  = "METHOTREXATE",
            voie1 = "Orale",        voie2 = "Orale",
            level = "Association déconseillée",
            description = "Augmentation de la toxicité du méthotrexate.",
            mecanisme = "Inhibition de la sécrétion tubulaire rénale.",
            conduite = "Surveiller la NFS."
        };
        interactRepo
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Interact, bool>>>()))
            .ReturnsAsync(new[] { localRow });

        openRouter
            .Setup(s => s.AnalyzeInteractionAsync(
                "AMOXICILLINE", "Orale", "METHOTREXATE", "Orale", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InteractionAnalysis(
                "Déconseillé",
                "AI description",
                "AI mecanisme",
                "AI conduite",
                "{ \"level\": \"Déconseillé\" }"));

        await vm.AnalyzeWithAiCommand.ExecuteAsync(null);

        vm.LocalInteractions.Should().ContainSingle(r => r.recordid == 10);
        vm.DeepAnalysisHasPendingResult.Should().BeTrue();
        vm.DeepAnalysisLevel.Should().Be("Déconseillé");
        vm.DeepAnalysisDescription.Should().Be("AI description");
        vm.DeepAnalysisMecanisme.Should().Be("AI mecanisme");
        vm.DeepAnalysisConduite.Should().Be("AI conduite");
        vm.IsDeepAnalysisRunning.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeWithAi_BothDirections_AreReturnedByLocalLookup()
    {
        var vm = BuildVm(out var interactRepo, out _, out var openRouter, out _, out _, out _);

        vm.SelectedDrugA = new Medic { recordid = 1, dci1 = "WARFARINE",  voie = "Orale" };
        vm.SelectedDrugB = new Medic { recordid = 2, dci1 = "ASPIRINE",   voie = "Orale" };

        var stored = new[]
        {
            new Interact { dci1 = "ASPIRINE",  voie1 = "Orale", dci2 = "WARFARINE", voie2 = "Orale", level = "Contre-indication" },
            new Interact { dci1 = "WARFARINE", voie1 = "",      dci2 = "ASPIRINE",  voie2 = "",      level = "Contre-indication" } // legacy wildcard
        };
        interactRepo
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Interact, bool>>>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Interact, bool>> pred) =>
                stored.Where(pred.Compile()).ToList());

        openRouter
            .Setup(s => s.AnalyzeInteractionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InteractionAnalysis("Contre-indiqué", "d", "m", "c", "r"));

        await vm.AnalyzeWithAiCommand.ExecuteAsync(null);

        vm.LocalInteractions.Should().HaveCount(2); // both directions + legacy wildcard
    }

    [Fact]
    public async Task AnalyzeWithAi_MissingDci1OnDrugA_ShowsWarning_AndDoesNotCallService()
    {
        var vm = BuildVm(out var interactRepo, out _, out var openRouter, out var dialog, out _, out _);

        vm.SelectedDrugA = new Medic { recordid = 1, dci1 = "", voie = "Orale" };
        vm.SelectedDrugB = new Medic { recordid = 2, dci1 = "METHOTREXATE", voie = "Orale" };

        await vm.AnalyzeWithAiCommand.ExecuteAsync(null);

        openRouter.Verify(s => s.AnalyzeInteractionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        interactRepo.Verify(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Interact, bool>>>()),
            Times.Never);
        dialog.Verify(d => d.ShowWarningAsync("DCI manquante", It.IsAny<string>()), Times.Once);
    }
}
