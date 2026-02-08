using CommunityToolkit.Mvvm.ComponentModel;

namespace AVCNDB.WPF.ViewModels;

public partial class LibraryShellViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectedTabIndex;

    public LibraryShellViewModel(
        DciListViewModel dciListViewModel,
        FamiliesListViewModel familiesListViewModel,
        LabosListViewModel labosListViewModel,
        FormesListViewModel formesListViewModel,
        VoiesListViewModel voiesListViewModel)
    {
        DciListViewModel = dciListViewModel;
        FamiliesListViewModel = familiesListViewModel;
        LabosListViewModel = labosListViewModel;
        FormesListViewModel = formesListViewModel;
        VoiesListViewModel = voiesListViewModel;
    }

    public DciListViewModel DciListViewModel { get; }
    public FamiliesListViewModel FamiliesListViewModel { get; }
    public LabosListViewModel LabosListViewModel { get; }
    public FormesListViewModel FormesListViewModel { get; }
    public VoiesListViewModel VoiesListViewModel { get; }
}
