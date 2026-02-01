using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Tests.ViewModels;

/// <summary>
/// Tests unitaires pour HomeViewModel
/// </summary>
public class HomeViewModelTests
{
    [Fact]
    public void HomeViewModel_Creation_SetsInitialValues()
    {
        // Arrange
        var medicRepositoryMock = new Mock<IRepository<Medic>>();
        var dciRepositoryMock = new Mock<IRepository<Dci>>();
        var laboRepositoryMock = new Mock<IRepository<Labos>>();
        var stockServiceMock = new Mock<IStockService>();
        var navigationServiceMock = new Mock<INavigationService>();

        medicRepositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Medic, bool>>?>()))
            .ReturnsAsync(0);
        dciRepositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Dci, bool>>?>()))
            .ReturnsAsync(0);
        laboRepositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Labos, bool>>?>()))
            .ReturnsAsync(0);
        stockServiceMock.Setup(s => s.GetTotalAlertsCountAsync())
            .ReturnsAsync(0);
        stockServiceMock.Setup(s => s.GetLowStockAlertsAsync())
            .ReturnsAsync(new List<StockAlertItem>());

        // Act
        var viewModel = new HomeViewModel(
            medicRepositoryMock.Object,
            dciRepositoryMock.Object,
            laboRepositoryMock.Object,
            stockServiceMock.Object,
            navigationServiceMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.TotalMedics.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void NavigateToMedicsCommand_Exists()
    {
        // Arrange
        var medicRepositoryMock = new Mock<IRepository<Medic>>();
        var dciRepositoryMock = new Mock<IRepository<Dci>>();
        var laboRepositoryMock = new Mock<IRepository<Labos>>();
        var stockServiceMock = new Mock<IStockService>();
        var navigationServiceMock = new Mock<INavigationService>();

        medicRepositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Medic, bool>>?>()))
            .ReturnsAsync(0);
        dciRepositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Dci, bool>>?>()))
            .ReturnsAsync(0);
        laboRepositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Labos, bool>>?>()))
            .ReturnsAsync(0);
        stockServiceMock.Setup(s => s.GetTotalAlertsCountAsync())
            .ReturnsAsync(0);
        stockServiceMock.Setup(s => s.GetLowStockAlertsAsync())
            .ReturnsAsync(new List<StockAlertItem>());

        var viewModel = new HomeViewModel(
            medicRepositoryMock.Object,
            dciRepositoryMock.Object,
            laboRepositoryMock.Object,
            stockServiceMock.Object,
            navigationServiceMock.Object);

        // Assert
        viewModel.NavigateToMedicsCommand.Should().NotBeNull();
    }
}
