using System.Windows;

namespace AVCNDB.WPF.Views;

/// <summary>
/// Fenêtre d'aperçu HTML de la monographie d'un médicament.
/// Embarque un contrôle WebView2 et affiche le HTML rendu par
/// <see cref="AVCNDB.WPF.Services.MonographieHtmlRenderer"/>.
/// </summary>
public partial class MonographiePreviewWindow : Window
{
    private string? _pendingHtml;
    private bool _initialized;

    public MonographiePreviewWindow()
    {
        InitializeComponent();
        WebView.CoreWebView2InitializationCompleted += OnCoreInitialized;
        _ = WebView.EnsureCoreWebView2Async();
    }

    /// <summary>
    /// Charge le HTML dans la WebView. Si la WebView n'est pas encore initialisée,
    /// le HTML est mémorisé et affiché dès l'initialisation terminée.
    /// </summary>
    public void LoadHtml(string html)
    {
        if (_initialized)
            WebView.NavigateToString(html);
        else
            _pendingHtml = html;
    }

    private void OnCoreInitialized(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess) return;
        _initialized = true;
        if (_pendingHtml != null)
        {
            WebView.NavigateToString(_pendingHtml);
            _pendingHtml = null;
        }
    }
}
