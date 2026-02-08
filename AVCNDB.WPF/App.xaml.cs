using System.Globalization;
using System.IO;
using System.Windows;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Services;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;
using AVCNDB.WPF.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows.Threading;
using System.Threading;

namespace AVCNDB.WPF;

/// <summary>
/// Application principale AVCNDB WPF
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    private static int _isHandlingUnhandledUiException;

    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        // Configuration de la culture par défaut (séparateur décimal = point)
        SetDefaultCulture();

        // Configuration de Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/avcndb-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Configuration de l'hôte avec DI
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(context.Configuration, services);
            })
            .Build();

        Services = _host.Services;

        // Safety net: avoid hard crashes on unhandled UI exceptions
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Prevent infinite cascades if showing the dialog triggers another UI exception.
        if (Interlocked.Exchange(ref _isHandlingUnhandledUiException, 1) == 1)
        {
            e.Handled = true;
            return;
        }

        try
        {
            Log.Error(e.Exception, "Unhandled UI exception");
        }
        catch
        {
            // ignore logging failures
        }

        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AVCNDB",
                "logs");
            Directory.CreateDirectory(logDir);

            var logPath = Path.Combine(logDir, $"ui-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:O}]\n{e.Exception}\n\n");
        }
        catch
        {
            // ignore file IO failures
        }

        try
        {
            MessageBox.Show(
                $"Une erreur inattendue s'est produite.\n\n{e.Exception.Message}",
                "Erreur",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // ignore UI failures
        }

        e.Handled = true;

        Interlocked.Exchange(ref _isHandlingUnhandledUiException, 0);
    }

    private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        // ============================================
        // CONFIGURATION
        // ============================================
        services.AddSingleton(configuration);

        // ============================================
        // LOGGING (Serilog)
        // ============================================
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddSerilog(dispose: true);
        });

        // ============================================
        // DATABASE CONTEXT
        // ============================================
        var useRemoteDb = configuration.GetValue<bool>("AppSettings:UseRemoteDatabase");
        var connectionName = useRemoteDb ? "RemoteConnection" : "DefaultConnection";
        var connectionString = configuration.GetConnectionString(connectionName);
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
        
        // Use DbContextFactory for thread-safe DbContext creation
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, mySqlOptions =>
            {
                mySqlOptions.EnableRetryOnFailure();
            }));
        
        // Also register DbContext for cases where scoped access is needed
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, mySqlOptions =>
            {
                mySqlOptions.EnableRetryOnFailure();
            }),
            ServiceLifetime.Transient);

        // ============================================
        // REPOSITORIES
        // ============================================
        services.AddTransient<IRepository<Medic>, Repository<Medic>>();
        services.AddTransient<IRepository<Dci>, Repository<Dci>>();
        services.AddTransient<IRepository<Labos>, Repository<Labos>>();
        services.AddTransient<IRepository<Families>, Repository<Families>>();
        services.AddTransient<IRepository<Interact>, Repository<Interact>>();
        services.AddTransient<IRepository<Stock>, Repository<Stock>>();
        services.AddTransient<IRepository<Formes>, Repository<Formes>>();
        services.AddTransient<IRepository<Voies>, Repository<Voies>>();
        services.AddTransient<IRepository<Presents>, Repository<Presents>>();

        // ============================================
        // SERVICES
        // ============================================
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<DatabaseDiagnosticsService>();
        services.AddSingleton<MedicSyncService>();
        services.AddTransient<IExcelService, ExcelService>();
        services.AddTransient<IPdfService, PdfService>();
        services.AddTransient<IStockService, StockService>();
        services.AddTransient(typeof(IStrictExcelSyncService<>), typeof(StrictExcelSyncService<>));

        // ============================================
        // VIEWMODELS
        // ============================================
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<DciListViewModel>();
        services.AddTransient<FamiliesListViewModel>();
        services.AddTransient<LabosListViewModel>();
        services.AddTransient<MedicListViewModel>();
        services.AddTransient<MedicDetailViewModel>();
        services.AddTransient<MedicEditViewModel>();
        services.AddTransient<InteractionsViewModel>();
        services.AddTransient<FormesListViewModel>();
        services.AddTransient<VoiesListViewModel>();
        services.AddTransient<StockViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DatabaseViewModel>();
        services.AddTransient<LibraryShellViewModel>();

        // ============================================
        // VIEWS
        // ============================================
        services.AddTransient<MainWindow>();
        services.AddTransient<HomeView>();
        services.AddTransient<DciListView>();
        services.AddTransient<FamiliesListView>();
        services.AddTransient<LabosListView>();
        services.AddTransient<MedicListView>();
        services.AddTransient<MedicDetailView>();
        services.AddTransient<MedicEditView>();
        services.AddTransient<InteractionsView>();
        services.AddTransient<FormesListView>();
        services.AddTransient<VoiesListView>();
        services.AddTransient<StockView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<DatabaseView>();
        services.AddTransient<LibraryView>();
        services.AddTransient<MovementsView>();
        services.AddTransient<ToolsView>();
    }

    private void SetDefaultCulture()
    {
        var culture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        
        Log.Information("Application AVCNDB démarrée");

        try
        {
            var diagnostics = Services.GetService<DatabaseDiagnosticsService>();
            if (diagnostics != null)
            {
                await diagnostics.LogDiagnosticsAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Startup diagnostics failed");
        }

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("Application AVCNDB fermée");
        Log.CloseAndFlush();

        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }

    /// <summary>
    /// Résout un service depuis le conteneur DI
    /// </summary>
    public static T GetService<T>() where T : class
    {
        return Services.GetRequiredService<T>();
    }
}
