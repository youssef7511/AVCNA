using System.Collections.Generic;
using System.Reflection;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace AVCNDB.WPF.Tests.ViewModels;

public class EditionFileViewModelTests
{
    private EditionFileViewModel CreateVm(
        Mock<IEditionFileService>? editionMock = null,
        Mock<IDialogService>? dialogMock = null)
    {
        editionMock ??= new Mock<IEditionFileService>();
        dialogMock ??= new Mock<IDialogService>();
        return new EditionFileViewModel(
            editionMock.Object,
            dialogMock.Object,
            new Mock<IRepository<Formes>>().Object,
            new Mock<IRepository<Labos>>().Object,
            new Mock<IRepository<Families>>().Object,
            new Mock<IRepository<Voies>>().Object,
            new Mock<IRepository<Specialites>>().Object);
    }

    private void SetAllRows(EditionFileViewModel vm, List<EditionRowViewModel> rows)
    {
        var field = typeof(EditionFileViewModel)
            .GetField("_allRows", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(vm, rows);
    }

    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var vm = CreateVm();

        vm.Should().NotBeNull();
        vm.Rows.Should().BeEmpty();
        vm.FilterType.Should().Be("Tous");
        vm.SelectedRow.Should().BeNull();
        vm.TotalRowCount.Should().Be(0);
        vm.UnknownRowCount.Should().Be(0);
        vm.ApprovedRowCount.Should().Be(0);
        vm.CurrentFilePath.Should().BeEmpty();
    }

    [Fact]
    public void Commands_AreNotNull()
    {
        var vm = CreateVm();

        vm.ImportCommand.Should().NotBeNull();
        vm.ApproveRowCommand.Should().NotBeNull();
        vm.RejectRowCommand.Should().NotBeNull();
        vm.MarkAsNewCommand.Should().NotBeNull();
        vm.MarkForDeletionCommand.Should().NotBeNull();
        vm.ResetRowCommand.Should().NotBeNull();
        vm.OpenLibraryManagerCommand.Should().NotBeNull();
        vm.SimilaritySearchCommand.Should().NotBeNull();
        vm.ExportCommand.Should().NotBeNull();
    }

    [Fact]
    public void FilterType_Affectes_FiltersCorrectly()
    {
        var vm = CreateVm();

        var row1 = new EditionRowViewModel(new EditionRow { ActionFlag = ActionFlag.Affecte });
        var row2 = new EditionRowViewModel(new EditionRow { ActionFlag = ActionFlag.AjouterNew });
        var row3 = new EditionRowViewModel(new EditionRow { ActionFlag = ActionFlag.Affecte });

        SetAllRows(vm, new List<EditionRowViewModel> { row1, row2, row3 });

        vm.FilterType = "Affectés";

        vm.Rows.Should().HaveCount(2);
        vm.Rows.Should().AllSatisfy(r => r.ActionFlag.Should().Be(ActionFlag.Affecte));
    }

    [Fact]
    public void FilterType_Tous_ShowsAllRows()
    {
        var vm = CreateVm();

        SetAllRows(vm, new List<EditionRowViewModel>
        {
            new(new EditionRow { ActionFlag = ActionFlag.Affecte }),
            new(new EditionRow { ActionFlag = ActionFlag.AjouterNew }),
            new(new EditionRow { ActionFlag = ActionFlag.None })
        });

        // Change away from "Tous" first, then back to trigger filter
        vm.FilterType = "Affectés";
        vm.FilterType = "Tous";

        vm.Rows.Should().HaveCount(3);
    }

    [Fact]
    public void FilterType_ChangementsDePrix_FiltersCorrectly()
    {
        var vm = CreateVm();

        SetAllRows(vm, new List<EditionRowViewModel>
        {
            new(new EditionRow { HasPriceChanged = true }),
            new(new EditionRow { HasPriceChanged = false }),
            new(new EditionRow { HasPriceChanged = true })
        });

        vm.FilterType = "Changements de prix";

        vm.Rows.Should().HaveCount(2);
    }

    [Fact]
    public void FilterType_AvecAP_FiltersCorrectly()
    {
        var vm = CreateVm();

        SetAllRows(vm, new List<EditionRowViewModel>
        {
            new(new EditionRow { IsAp = 1 }),
            new(new EditionRow { IsAp = 0 }),
            new(new EditionRow { IsAp = 1 })
        });

        vm.FilterType = "Avec A.P";

        vm.Rows.Should().HaveCount(2);
    }
}
