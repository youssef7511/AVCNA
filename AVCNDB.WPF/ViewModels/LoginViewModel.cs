using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>ViewModel de la fenêtre de connexion.</summary>
public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;
    private readonly RememberMeService _rememberMeService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private string? _loginError;

    [ObservableProperty]
    private bool _hasLoginError;

    /// <summary>Déclenché quand la fenêtre doit se fermer. true = succès, false = annulé.</summary>
    public event Action<bool>? RequestClose;

    public LoginViewModel(
        IAuthService authService,
        ISessionService sessionService,
        RememberMeService rememberMeService)
    {
        _authService = authService;
        _sessionService = sessionService;
        _rememberMeService = rememberMeService;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        LoginError = null;
        HasLoginError = false;

        await ExecuteAsync(async () =>
        {
            var result = await _authService.SignInAsync(Username, Password);

            // Clear the password field immediately on both paths to avoid
            // retaining the plaintext value in memory longer than necessary.
            Password = string.Empty;

            if (!result.Success)
            {
                LoginError = result.ErrorMessage ?? "Identifiant ou mot de passe incorrect.";
                HasLoginError = true;
                return;
            }

            _sessionService.SetCurrentUser(result.User!);

            if (RememberMe)
            {
                var issued = await _authService.IssueRememberMeTokenAsync(result.User!.recordid);
                if (issued != null)
                    _rememberMeService.SaveForUser(result.User!.username, issued.Token, issued.ExpiresUtc);
            }

            RequestClose?.Invoke(true);
        });
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
