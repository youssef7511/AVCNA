using System.Windows;
using System.Windows.Controls;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class DenominationView : UserControl
{
    public DenominationView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Notify the ViewModel when a cell is manually edited.
    /// Also enforces sequential dose/unit fill order.
    /// </summary>
    private void DataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit)
            return;

        var medic = e.Row.Item as Medic;
        var header = e.Column.Header?.ToString();
        if (medic == null || header == null)
            return;

        // Enforce sequential fill: cannot fill doseN/uN unless dose(N-1)/u(N-1) are populated
        if (IsBlockedBySequentialRule(medic, header, e))
            return;

        // Validate ComboBox columns — value must come from the library
        if (DataContext is DenominationViewModel vm)
        {
            if (e.EditingElement is ComboBox combo)
            {
                var newText = combo.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(newText))
                {
                    var allowedList = header switch
                    {
                        "Forme" => vm.FormesList,
                        "Présent." => vm.PresentsList,
                        "Labo" => vm.LabosList,
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

            vm.NotifyCellEdited(medic, header);
        }
    }

    /// <summary>
    /// Returns true (and cancels the edit) if the user tries to fill a dose/unit pair
    /// when the previous pair is still empty.
    /// Rule: dose1/u1 → dose2/u2 → dose3/u3 → dose4/u4 must be filled in order.
    /// </summary>
    private static bool IsBlockedBySequentialRule(Medic m, string header, DataGridCellEditEndingEventArgs e)
    {
        // Determine which previous dose is required before editing a higher dose
        string? prevDose = header switch
        {
            "Dose 2" or "U2" => m.dose1,
            "Dose 3" or "U3" => m.dose2,
            "Dose 4" or "U4" => m.dose3,
            _ => null
        };

        if (prevDose is null)
            return false; // dose1/u1 or non-dose column — always allowed

        if (!string.IsNullOrWhiteSpace(prevDose))
            return false; // previous dose is filled — allowed

        // Cancel the edit and clear the value
        e.Cancel = true;

        int requiredPair = header switch
        {
            "Dose 2" or "U2" => 1,
            "Dose 3" or "U3" => 2,
            "Dose 4" or "U4" => 3,
            _ => 0
        };

        MessageBox.Show(
            $"Veuillez d'abord remplir Dose {requiredPair} avant de modifier cette colonne.",
            "Ordre séquentiel requis",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        return true;
    }
}
