using Microsoft.Win32;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Views;

namespace AVCNDB.WPF.Services;

public class DialogService : IDialogService
{
    public Task ShowInfoAsync(string title, string message)
    {
        CustomMessageDialog.Show(title, message, MessageDialogType.Info);
        return Task.CompletedTask;
    }

    public Task ShowSuccessAsync(string title, string message)
    {
        CustomMessageDialog.Show(title, message, MessageDialogType.Success);
        return Task.CompletedTask;
    }

    public Task ShowWarningAsync(string title, string message)
    {
        CustomMessageDialog.Show(title, message, MessageDialogType.Warning);
        return Task.CompletedTask;
    }

    public Task ShowErrorAsync(string title, string message)
    {
        CustomMessageDialog.Show(title, message, MessageDialogType.Error);
        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmAsync(string title, string message)
    {
        var result = CustomMessageDialog.Show(title, message, MessageDialogType.Info, hasCancel: true);
        return Task.FromResult(result);
    }

    public Task<string?> ShowInputAsync(string title, string message, string defaultValue = "")
    {
        return Task.FromResult<string?>(null);
    }

    public string? ShowOpenFileDialog(string filter, string title = "Ouvrir un fichier")
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Title = title,
            CheckFileExists = true,
            CheckPathExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ShowSaveFileDialog(string filter, string defaultFileName = "", string title = "Enregistrer sous")
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            Title = title,
            FileName = defaultFileName,
            CheckPathExists = true,
            OverwritePrompt = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
