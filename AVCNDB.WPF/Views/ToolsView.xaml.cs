using System.Windows.Input;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class ToolsView : System.Windows.Controls.UserControl
{
    public ToolsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Ferme le panneau statistiques en cliquant sur le fond sombre
    /// </summary>
    private void StatsOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ToolsViewModel vm)
            vm.ShowStats = false;
    }

    /// <summary>
    /// Empêche la fermeture quand on clique sur le panneau lui-même
    /// </summary>
    private void StatsPanel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    /// <summary>
    /// Ferme le panneau statistiques via le bouton Fermer
    /// </summary>
    private void CloseStats_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ToolsViewModel vm)
            vm.ShowStats = false;
    }
}
