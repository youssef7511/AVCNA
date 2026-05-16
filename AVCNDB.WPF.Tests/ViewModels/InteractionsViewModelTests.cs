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
}
