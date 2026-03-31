using System.Windows;
using System.Windows.Controls;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class MonographieView : UserControl
{
    public MonographieView()
    {
        InitializeComponent();
    }

    private void DataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit)
            return;

        if (DataContext is not MonographieViewModel vm)
            return;

        var medic = e.Row.Item as Medic;
        var header = e.Column.Header?.ToString();

        // Validate ComboBox columns — value must come from the library
        if (medic != null && header != null && e.EditingElement is ComboBox combo)
        {
            var newText = combo.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(newText))
            {
                var allowedList = header switch
                {
                    "D.C.I" => vm.DciList,
                    "Forme" => vm.FormesList,
                    "Voie" => vm.VoiesList,
                    "Labo" => vm.LabosList,
                    "G.formes" => vm.FormGroupsList,
                    _ => null
                };

                if (allowedList != null && !allowedList.Contains(newText, StringComparer.OrdinalIgnoreCase))
                {
                    e.Cancel = true;
                    MessageBox.Show(
                        $"La valeur \"{newText}\" n'existe pas dans la bibliothèque {header}.\nVeuillez sélectionner une valeur existante.",
                        "Valeur non autorisée",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }
        }

        vm.NotifyCellEdited();
    }
}
