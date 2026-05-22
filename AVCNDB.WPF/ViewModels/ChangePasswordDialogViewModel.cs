using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>ViewModel du dialogue de changement de mot de passe.</summary>
public partial class ChangePasswordDialogViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyPropertyChangedFor(nameof(PasswordsMatch))]
    [NotifyPropertyChangedFor(nameof(ShowMatchOk))]
    [NotifyPropertyChangedFor(nameof(ShowMatchMismatch))]
    [NotifyPropertyChangedFor(nameof(ShowLengthHint))]
    [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyPropertyChangedFor(nameof(PasswordsMatch))]
    [NotifyPropertyChangedFor(nameof(ShowMatchOk))]
    [NotifyPropertyChangedFor(nameof(ShowMatchMismatch))]
    [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string? _changeError;

    [ObservableProperty]
    private bool _hasChangeError;

    /// <summary>Déclenché quand le dialogue doit se fermer.</summary>
    public event Action<bool>? RequestClose;

    public bool PasswordsMatch =>
        !string.IsNullOrEmpty(NewPassword) && NewPassword == ConfirmPassword;

    public bool CanSubmit =>
        !string.IsNullOrEmpty(CurrentPassword)
        && NewPassword.Length >= 8
        && PasswordsMatch;

    /// <summary>Show the green "passwords match" hint: both filled and equal.</summary>
    public bool ShowMatchOk =>
        !string.IsNullOrEmpty(NewPassword)
        && !string.IsNullOrEmpty(ConfirmPassword)
        && NewPassword == ConfirmPassword;

    /// <summary>Show the red "passwords don't match" hint: both filled but unequal.</summary>
    public bool ShowMatchMismatch =>
        !string.IsNullOrEmpty(NewPassword)
        && !string.IsNullOrEmpty(ConfirmPassword)
        && NewPassword != ConfirmPassword;

    /// <summary>Show the orange "too short" hint: new password typed but below 8 chars.</summary>
    public bool ShowLengthHint =>
        !string.IsNullOrEmpty(NewPassword) && NewPassword.Length < 8;

    public ChangePasswordDialogViewModel(
        IAuthService authService,
        ISessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task ChangePasswordAsync()
    {
        ChangeError = null;
        HasChangeError = false;

        await ExecuteAsync(async () =>
        {
            var userId = _sessionService.CurrentUser?.recordid ?? 0;
            var ok = await _authService.ChangePasswordAsync(userId, CurrentPassword, NewPassword);

            // Clear all password fields immediately on both paths to avoid
            // retaining plaintext values in memory longer than necessary.
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;

            if (!ok)
            {
                ChangeError = "Mot de passe actuel incorrect ou erreur interne.";
                HasChangeError = true;
                return;
            }

            // Mirror the DB flip so we don't re-trigger the dialog on next session.CurrentUser read.
            if (_sessionService.CurrentUser != null)
                _sessionService.CurrentUser.must_change_password = false;

            RequestClose?.Invoke(true);
        });
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
