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

namespace AVCNDB.WPF;

/// <summary>
/// Application principale AVCNDB WPF
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

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
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));

        // ============================================
        // REPOSITORIES
        // ============================================
        services.AddScoped<IRepository<Medic>, Repository<Medic>>();
        services.AddScoped<IRepository<Dci>, Repository<Dci>>();
        services.AddScoped<IRepository<Labos>, Repository<Labos>>();
        services.AddScoped<IRepository<Families>, Repository<Families>>();
        services.AddScoped<IRepository<Interact>, Repository<Interact>>();
        services.AddScoped<IRepository<Stock>, Repository<Stock>>();
        services.AddScoped<IRepository<Formes>, Repository<Formes>>();
        services.AddScoped<IRepository<Voies>, Repository<Voies>>();
        services.AddScoped<IRepository<Presents>, Repository<Presents>>();

        // ============================================
        // SERVICES
        // ============================================
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddScoped<IExcelService, ExcelService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<IStockService, StockService>();

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
        services.AddTransient<StockViewModel>();
        services.AddTransient<SettingsViewModel>();

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
        services.AddTransient<StockView>();
        services.AddTransient<SettingsView>();
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
