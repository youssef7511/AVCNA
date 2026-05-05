using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace AVCNDB.WPF.Views;

public enum MessageDialogType { Info, Success, Warning, Error }

public partial class CustomMessageDialog : Window
{
    private bool _result;

    private static readonly Dictionary<MessageDialogType, (Color strip, PackIconKind icon)> _themes = new()
    {
        [MessageDialogType.Info]    = (Color.FromRgb(0x19, 0x76, 0xD2), PackIconKind.InformationOutline),
        [MessageDialogType.Success] = (Color.FromRgb(0x2E, 0x7D, 0x32), PackIconKind.CheckCircleOutline),
        [MessageDialogType.Warning] = (Color.FromRgb(0xE6, 0x5C, 0x00), PackIconKind.AlertOutline),
        [MessageDialogType.Error]   = (Color.FromRgb(0xC6, 0x28, 0x28), PackIconKind.CloseCircleOutline),
    };

    private CustomMessageDialog(string title, string message, MessageDialogType type, bool hasCancel)
    {
        InitializeComponent();

        var (stripColor, iconKind) = _themes[type];
        StripBorder.Background = new SolidColorBrush(stripColor);
        StripIcon.Kind = iconKind;
        TitleText.Text = title;
        MessageText.Text = message;

        PrimaryButton.Content = hasCancel ? "Oui" : "OK";
        PrimaryButton.Background = new SolidColorBrush(stripColor);

        if (hasCancel)
        {
            SecondaryButton.Visibility = Visibility.Visible;
            SecondaryButton.Content = "Non";
        }

        Owner = Application.Current.MainWindow;
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        _result = true;
        Close();
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        _result = false;
        Close();
    }

    public static bool Show(string title, string message, MessageDialogType type = MessageDialogType.Info, bool hasCancel = false)
    {
        var dialog = new CustomMessageDialog(title, message, type, hasCancel);
        dialog.ShowDialog();
        return dialog._result;
    }
}
