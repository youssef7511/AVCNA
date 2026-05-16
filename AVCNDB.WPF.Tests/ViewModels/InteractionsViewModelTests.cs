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
}
