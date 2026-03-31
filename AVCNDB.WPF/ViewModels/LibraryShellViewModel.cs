using CommunityToolkit.Mvvm.ComponentModel;

namespace AVCNDB.WPF.ViewModels;

public partial class LibraryShellViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _selectedTabIndex;

    public LibraryShellViewModel(
        DciListViewModel dciListViewModel,
        FamiliesListViewModel familiesListViewModel,
        LabosListViewModel labosListViewModel,
        FormesListViewModel formesListViewModel,
        VoiesListViewModel voiesListViewModel,
        SpecialitesListViewModel specialitesListViewModel,
        PresentsListViewModel presentsListViewModel,
        PosoListViewModel posoListViewModel)
    {
        DciListViewModel = dciListViewModel;
        FamiliesListViewModel = familiesListViewModel;
        LabosListViewModel = labosListViewModel;
        FormesListViewModel = formesListViewModel;
        VoiesListViewModel = voiesListViewModel;
        SpecialitesListViewModel = specialitesListViewModel;
        PresentsListViewModel = presentsListViewModel;
        PosoListViewModel = posoListViewModel;
    }

    public DciListViewModel DciListViewModel { get; }
    public FamiliesListViewModel FamiliesListViewModel { get; }
    public LabosListViewModel LabosListViewModel { get; }
    public FormesListViewModel FormesListViewModel { get; }
    public VoiesListViewModel VoiesListViewModel { get; }
    public SpecialitesListViewModel SpecialitesListViewModel { get; }
    public PresentsListViewModel PresentsListViewModel { get; }
    public PosoListViewModel PosoListViewModel { get; }
}
