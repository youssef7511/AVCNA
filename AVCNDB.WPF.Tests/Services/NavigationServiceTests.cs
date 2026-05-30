using AVCNDB.WPF.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AVCNDB.WPF.Tests.Services;

/// <summary>
/// Pins the contract that GoBack restores the *same* previous view-model instance
/// (so transient list state — e.g. the current page — survives a detail round-trip)
/// instead of re-resolving a fresh instance from the container.
/// </summary>
public class NavigationServiceTests
{
    private sealed class FakeListViewModel { public int CurrentPage { get; set; } = 1; }
    private sealed class FakeDetailViewModel { }

    private static NavigationService BuildNav()
    {
        var provider = new Mock<IServiceProvider>();
        // Simulate transient registrations: a new instance on every resolve.
        provider.Setup(p => p.GetService(typeof(FakeListViewModel))).Returns(() => new FakeListViewModel());
        provider.Setup(p => p.GetService(typeof(FakeDetailViewModel))).Returns(() => new FakeDetailViewModel());
        return new NavigationService(provider.Object);
    }

    [Fact]
    public void GoBack_RestoresSamePreviousInstance_PreservingItsState()
    {
        var nav = BuildNav();

        nav.NavigateTo<FakeListViewModel>();
        var list = (FakeListViewModel)nav.CurrentView!;
        list.CurrentPage = 5;                 // user paged to 5

        nav.NavigateTo<FakeDetailViewModel>(42);  // open a detail
        nav.GoBack();                              // "Retour"

        nav.CurrentView.Should().BeSameAs(list, "GoBack must restore the original list view-model, not a fresh one");
        ((FakeListViewModel)nav.CurrentView!).CurrentPage.Should().Be(5, "the preserved instance keeps its page");
    }
}
