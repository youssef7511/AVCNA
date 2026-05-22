using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Services;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;
using AVCNDB.WPF.Views;
using BCrypt.Net;
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
                config.AddJsonFile("secrets.json", optional: true, reloadOnChange: false);
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
        services.AddTransient<IRepository<User>, Repository<User>>();
        services.AddTransient<IRepository<Medic>, Repository<Medic>>();
        services.AddTransient<IRepository<Dci>, Repository<Dci>>();
        services.AddTransient<IRepository<Labos>, Repository<Labos>>();
        services.AddTransient<IRepository<Families>, Repository<Families>>();
        services.AddTransient<IRepository<Interact>, Repository<Interact>>();
        services.AddTransient<IRepository<Stock>, Repository<Stock>>();
        services.AddTransient<IRepository<Formes>, Repository<Formes>>();
        services.AddTransient<IRepository<Voies>, Repository<Voies>>();
        services.AddTransient<IRepository<Specialites>, Repository<Specialites>>();
        services.AddTransient<IRepository<Presents>, Repository<Presents>>();
        services.AddTransient<IRepository<Poso>, Repository<Poso>>();
        services.AddTransient<IRepository<Catveic>, Repository<Catveic>>();

        // ============================================
        // AUTH SERVICES (Sprint 1)
        // ============================================
        services.AddSingleton<ISessionService, SessionService>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddSingleton<RememberMeService>();

        // ============================================
        // SERVICES
        // ============================================
        services.AddSingleton<HttpClient>(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(60) });
        services.AddTransient<IOpenRouterService, OpenRouterService>();
        services.AddSingleton<IMLPfeService, MLPfeService>();

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
        services.AddTransient<IUnknownDataDetectionService, FuzzyDetectionService>();
        services.AddTransient<IEditionFileService, EditionFileService>();

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
        services.AddTransient<MedicUpsertDialogViewModel>();
        services.AddTransient<InteractionsViewModel>();
        services.AddTransient<DenominationViewModel>();
        services.AddTransient<PrixViewModel>();
        services.AddTransient<MonographieViewModel>();
        services.AddTransient<FormesListViewModel>();
        services.AddTransient<VoiesListViewModel>();
        services.AddTransient<SpecialitesListViewModel>();
        services.AddTransient<PresentsListViewModel>();
        services.AddTransient<PosoListViewModel>();
        services.AddTransient<StockViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DatabaseViewModel>();
        services.AddTransient<LibraryShellViewModel>();
        services.AddTransient<ToolsViewModel>();
        services.AddSingleton<EditionFileViewModel>();
        services.AddTransient<SimilaritySearchViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ChangePasswordDialogViewModel>();

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
        services.AddTransient<MedicUpsertDialog>();
        services.AddTransient<MonographiePreviewWindow>();
        services.AddTransient<InteractionsView>();
        services.AddTransient<DenominationView>();
        services.AddTransient<PrixView>();
        services.AddTransient<MonographieView>();
        services.AddTransient<FormesListView>();
        services.AddTransient<VoiesListView>();
        services.AddTransient<SpecialitesListView>();
        services.AddTransient<PresentsListView>();
        services.AddTransient<PosoListView>();
        services.AddTransient<StockView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<DatabaseView>();
        services.AddTransient<LibraryView>();
        services.AddTransient<MovementsView>();
        services.AddTransient<SimilaritySearchDialog>();
        services.AddTransient<ToolsView>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<ChangePasswordDialog>();
    }

    private void SetDefaultCulture()
    {
        var culture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Default WPF ShutdownMode is OnLastWindowClose. With the auth gate we
        // open a LoginWindow as a modal dialog, then close it, THEN open MainWindow.
        // Between those two steps WPF sees "all windows closed" and schedules a
        // shutdown — which fires before MainWindow.Show() can paint, so the app
        // appears to silently exit after the user clicks "Connexion".
        // Force explicit shutdown during the gate; flip back to OnMainWindowClose
        // once MainWindow is on screen.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            await _host.StartAsync();

            Log.Information("Application AVCNDB démarrée");

            await ApplyPendingSchemaMigrationsAsync();

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

            // Apply saved theme BEFORE the window paints, so dark mode users
            // don't see a white flash before the palette is muted in-place.
            Services.GetRequiredService<IThemeService>().Initialize();

            // ── Auth gate (Sprint 1) ─────────────────────────────────────────
            var rememberMe = Services.GetRequiredService<RememberMeService>();
            var auth       = Services.GetRequiredService<IAuthService>();
            var session    = Services.GetRequiredService<ISessionService>();

            if (rememberMe.TryLoad(out var token))
            {
                var user = await auth.SignInFromTokenAsync(token.Username);
                if (user != null)
                    session.SetCurrentUser(user);
                else
                    rememberMe.Clear();
            }

            if (!session.IsAuthenticated)
            {
                var login = Services.GetRequiredService<LoginWindow>();
                var ok = login.ShowDialog() == true;
                if (!ok)
                {
                    Shutdown();
                    return;
                }
            }

            if (session.CurrentUser!.must_change_password)
            {
                var change = Services.GetRequiredService<ChangePasswordDialog>();
                var ok = change.ShowDialog() == true;
                if (!ok)
                {
                    // User closed or cancelled the forced password-change dialog.
                    // Do not allow entry into the app with must_change_password still set.
                    session.Clear();
                    rememberMe.Clear();
                    Shutdown();
                    return;
                }
            }
            // ────────────────────────────────────────────────────────────────

            var mainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();

            // Now that MainWindow is on screen, restore the normal "close-to-exit"
            // behaviour: when the user closes MainWindow the app terminates.
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup failed");
            MessageBox.Show(
                $"Échec du démarrage de l'application.\n\n{ex.Message}",
                "Erreur fatale",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private async Task ApplyPendingSchemaMigrationsAsync()
    {
        try
        {
            var factory = Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var ctx = await factory.CreateDbContextAsync();
            var conn = ctx.Database.GetDbConnection();
            await conn.OpenAsync();
            foreach (var col in new[] { "voie1", "voie2" })
            {
                using var check = conn.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'interact' AND COLUMN_NAME = @col";
                var p = check.CreateParameter();
                p.ParameterName = "@col";
                p.Value = col;
                check.Parameters.Add(p);
                var count = Convert.ToInt32(await check.ExecuteScalarAsync());
                if (count == 0)
                {
                    using var alter = conn.CreateCommand();
                    alter.CommandText = $"ALTER TABLE interact ADD COLUMN {col} VARCHAR(100) NOT NULL DEFAULT ''";
                    await alter.ExecuteNonQueryAsync();
                    Log.Information("Added column interact.{Column}", col);
                }
            }

            // Widen description / mecanisme / conduite on interact from VARCHAR to TEXT.
            // Older schema imposed VARCHAR(500/200/500); AI responses occasionally
            // overflowed and were silently truncated. TEXT removes the ceiling.
            foreach (var (col, expectedType) in new[]
            {
                ("description", "text"),
                ("mecanisme",   "text"),
                ("conduite",    "text"),
            })
            {
                using var checkType = conn.CreateCommand();
                checkType.CommandText = "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'interact' AND COLUMN_NAME = @col";
                var p = checkType.CreateParameter();
                p.ParameterName = "@col"; p.Value = col;
                checkType.Parameters.Add(p);
                var currentType = (await checkType.ExecuteScalarAsync())?.ToString()?.ToLowerInvariant();
                if (currentType != null && currentType != expectedType)
                {
                    using var alter = conn.CreateCommand();
                    alter.CommandText = $"ALTER TABLE interact MODIFY COLUMN {col} TEXT NOT NULL";
                    await alter.ExecuteNonQueryAsync();
                    Log.Information("Widened interact.{Column} from {Old} to TEXT", col, currentType);
                }
            }

            // Widen level on interact to VARCHAR(30) (was 20) so "Contre-indiqué" fits cleanly.
            using (var checkLevel = conn.CreateCommand())
            {
                checkLevel.CommandText = "SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'interact' AND COLUMN_NAME = 'level'";
                var rawLen = await checkLevel.ExecuteScalarAsync();
                var currentLen = (rawLen != null && rawLen != DBNull.Value) ? Convert.ToInt32(rawLen) : 0;
                if (currentLen > 0 && currentLen < 30)
                {
                    using var alterLevel = conn.CreateCommand();
                    alterLevel.CommandText = "ALTER TABLE interact MODIFY COLUMN level VARCHAR(30) NOT NULL DEFAULT ''";
                    await alterLevel.ExecuteNonQueryAsync();
                    Log.Information("Widened interact.level from VARCHAR({Old}) to VARCHAR(30)", currentLen);
                }
            }

            // Provenance columns: source ("manual" / "ai") + model (AI model name when ai).
            // Lets the UI mark AI-sourced rows and supports future re-validation.
            foreach (var (col, ddl) in new[]
            {
                ("source", "ALTER TABLE interact ADD COLUMN source VARCHAR(20) NOT NULL DEFAULT 'manual'"),
                ("model",  "ALTER TABLE interact ADD COLUMN model VARCHAR(100) NOT NULL DEFAULT ''"),
            })
            {
                using var check = conn.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'interact' AND COLUMN_NAME = @col";
                var p = check.CreateParameter();
                p.ParameterName = "@col"; p.Value = col;
                check.Parameters.Add(p);
                var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
                if (!exists)
                {
                    using var alter = conn.CreateCommand();
                    alter.CommandText = ddl;
                    await alter.ExecuteNonQueryAsync();
                    Log.Information("Added column interact.{Column}", col);
                }
            }

            // Sprint 5 — Monographie Markdown column on medic
            using (var checkMono = conn.CreateCommand())
            {
                checkMono.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'medic' AND COLUMN_NAME = 'monographie'";
                var monoCount = Convert.ToInt32(await checkMono.ExecuteScalarAsync());
                if (monoCount == 0)
                {
                    using var alterMono = conn.CreateCommand();
                    // LONGTEXT cannot have a literal DEFAULT in MySQL; add as nullable,
                    // then backfill existing rows with empty string so the non-nullable
                    // C# property (string monographie = "") never reads NULL.
                    alterMono.CommandText = "ALTER TABLE medic ADD COLUMN monographie LONGTEXT NULL";
                    await alterMono.ExecuteNonQueryAsync();

                    using var backfill = conn.CreateCommand();
                    backfill.CommandText = "UPDATE medic SET monographie = '' WHERE monographie IS NULL";
                    var rows = await backfill.ExecuteNonQueryAsync();
                    Log.Information("Added column medic.monographie (LONGTEXT) and backfilled {Rows} rows", rows);
                }
            }

            // Sprint 1 — User table + default admin seed
            try
            {
                using var createUser = conn.CreateCommand();
                createUser.CommandText = @"
CREATE TABLE IF NOT EXISTS user (
  recordid INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(60) NOT NULL,
  displayname VARCHAR(120) NOT NULL DEFAULT '',
  password_hash VARCHAR(120) NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  must_change_password TINYINT(1) NOT NULL DEFAULT 0,
  last_login DATETIME NULL,
  addedat DATETIME NULL,
  updatedat DATETIME NULL,
  UNIQUE KEY uk_user_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";
                await createUser.ExecuteNonQueryAsync();
                Log.Information("User table ensured (CREATE TABLE IF NOT EXISTS).");

                using var countUsers = conn.CreateCommand();
                countUsers.CommandText = "SELECT COUNT(*) FROM user";
                var userCount = Convert.ToInt32(await countUsers.ExecuteScalarAsync());
                if (userCount == 0)
                {
                    var defaultHash = BCrypt.Net.BCrypt.HashPassword("admin", workFactor: 11);
                    using var seedAdmin = conn.CreateCommand();
                    seedAdmin.CommandText = @"
INSERT INTO user (username, displayname, password_hash, must_change_password, addedat)
VALUES ('admin', 'Administrateur', @hash, 1, NOW())";
                    var ph = seedAdmin.CreateParameter();
                    ph.ParameterName = "@hash";
                    ph.Value = defaultHash;
                    seedAdmin.Parameters.Add(ph);
                    await seedAdmin.ExecuteNonQueryAsync();
                    Log.Warning("Default admin seeded with password 'admin' — CHANGE IT IMMEDIATELY after first login.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "User table migration failed (non-fatal)");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Schema migration check failed (non-fatal)");
        }
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
