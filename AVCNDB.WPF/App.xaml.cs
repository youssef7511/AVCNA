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
        services.AddTransient<FormePosoAbbreviationService>();
        services.AddTransient<PosoLookupService>();
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
                var user = await auth.SignInFromRememberMeTokenAsync(token.Username, token.Token);
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
  remember_token_hash VARCHAR(120) NULL,
  remember_token_expires_utc DATETIME NULL,
  addedat DATETIME NULL,
  updatedat DATETIME NULL,
  UNIQUE KEY uk_user_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";
                await createUser.ExecuteNonQueryAsync();
                Log.Information("User table ensured (CREATE TABLE IF NOT EXISTS).");

                foreach (var (col, ddl) in new[]
                {
                    ("remember_token_hash", "ALTER TABLE user ADD COLUMN remember_token_hash VARCHAR(120) NULL"),
                    ("remember_token_expires_utc", "ALTER TABLE user ADD COLUMN remember_token_expires_utc DATETIME NULL"),
                })
                {
                    using var check = conn.CreateCommand();
                    check.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'user' AND COLUMN_NAME = @col";
                    var p = check.CreateParameter();
                    p.ParameterName = "@col";
                    p.Value = col;
                    check.Parameters.Add(p);
                    var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
                    if (!exists)
                    {
                        using var alter = conn.CreateCommand();
                        alter.CommandText = ddl;
                        await alter.ExecuteNonQueryAsync();
                        Log.Information("Added column user.{Column}", col);
                    }
                }

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

            // Sprint 3 (révision) — FK stricte Poso.posoform → Formes.itemname
            // (exigence organisme : cohérence garantie au niveau base, pas seulement
            // côté ViewModel).
            //
            // Sprint 6 (révision Option C) :
            //   - formes.posoform RÉ-INTRODUIT : porte le VERBE générique
            //     (ex. Capsule → "avaler", Pommade → "application")
            //   - poso.posoverb AJOUTÉ : snapshot du verbe copié depuis
            //     formes.posoform au moment de la création d'une Posologie
            //   - poso.posoform conserve sa FK vers formes.itemname (= nom de la forme)
            //   - formes.posoname reste supprimée (vestigiale, jamais réintroduite)
            try
            {
                // 1. Drop UNIQUEMENT posoname (vestigial). posoform est restauré juste après.
                using (var checkPosoname = conn.CreateCommand())
                {
                    checkPosoname.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'formes' AND COLUMN_NAME = 'posoname'";
                    var existsPosoname = Convert.ToInt32(await checkPosoname.ExecuteScalarAsync()) > 0;
                    if (existsPosoname)
                    {
                        using var dropCol = conn.CreateCommand();
                        dropCol.CommandText = "ALTER TABLE formes DROP COLUMN posoname";
                        await dropCol.ExecuteNonQueryAsync();
                        Log.Information("Dropped vestigial column formes.posoname");
                    }
                }

                // 1bis. Ré-introduire formes.posoform (verbe générique). Migration
                // idempotente : on n'agit que si la colonne est absente.
                using (var checkFormesPosoform = conn.CreateCommand())
                {
                    checkFormesPosoform.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'formes' AND COLUMN_NAME = 'posoform'";
                    var existsFormesPosoform = Convert.ToInt32(await checkFormesPosoform.ExecuteScalarAsync()) > 0;
                    if (!existsFormesPosoform)
                    {
                        using var addCol = conn.CreateCommand();
                        addCol.CommandText = "ALTER TABLE formes ADD COLUMN posoform VARCHAR(50) NULL";
                        await addCol.ExecuteNonQueryAsync();
                        Log.Information("Re-added formes.posoform (VARCHAR(50) NULL) — verbe générique de la forme");
                    }
                }

                // 1ter. Ajouter poso.posoverb (snapshot du verbe pris depuis formes.posoform
                // au moment où l'admin sélectionne la Forme dans le dialogue Poso).
                using (var checkPosoverb = conn.CreateCommand())
                {
                    checkPosoverb.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'poso' AND COLUMN_NAME = 'posoverb'";
                    var existsPosoverb = Convert.ToInt32(await checkPosoverb.ExecuteScalarAsync()) > 0;
                    if (!existsPosoverb)
                    {
                        using var addCol = conn.CreateCommand();
                        addCol.CommandText = "ALTER TABLE poso ADD COLUMN posoverb VARCHAR(50) NULL";
                        await addCol.ExecuteNonQueryAsync();
                        Log.Information("Added poso.posoverb (VARCHAR(50) NULL) — snapshot du verbe formes.posoform");
                    }
                }

                // 2. Élargir poso.posoform à VARCHAR(50) NULL (était VARCHAR(30) NOT NULL)
                using (var checkType = conn.CreateCommand())
                {
                    checkType.CommandText = "SELECT CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'poso' AND COLUMN_NAME = 'posoform'";
                    using var reader = await checkType.ExecuteReaderAsync();
                    int len = 0; string nullable = "NO";
                    if (await reader.ReadAsync())
                    {
                        var rawLen = reader.GetValue(0);
                        len = (rawLen != DBNull.Value) ? Convert.ToInt32(rawLen) : 0;
                        nullable = reader.GetString(1);
                    }
                    reader.Close();
                    if (len > 0 && (len < 50 || nullable == "NO"))
                    {
                        // Convertir les chaînes vides en NULL avant d'élargir la colonne.
                        using (var blankToNull = conn.CreateCommand())
                        {
                            blankToNull.CommandText = "UPDATE poso SET posoform = NULL WHERE posoform = ''";
                            await blankToNull.ExecuteNonQueryAsync();
                        }
                        using var alterPosoform = conn.CreateCommand();
                        alterPosoform.CommandText = "ALTER TABLE poso MODIFY COLUMN posoform VARCHAR(50) NULL DEFAULT NULL";
                        await alterPosoform.ExecuteNonQueryAsync();
                        Log.Information("Widened poso.posoform to VARCHAR(50) NULL (was VARCHAR({Old}) {Nul})", len, nullable);
                    }
                }

                // 3. Nettoyer les valeurs orphelines (poso.posoform pointant vers une
                //    formes.itemname inexistante) avant d'appliquer la FK.
                using (var cleanup = conn.CreateCommand())
                {
                    cleanup.CommandText = "UPDATE poso SET posoform = NULL WHERE posoform IS NOT NULL AND posoform NOT IN (SELECT itemname FROM formes)";
                    var orphans = await cleanup.ExecuteNonQueryAsync();
                    if (orphans > 0)
                    {
                        Log.Warning("Cleared {Count} orphan poso.posoform value(s) before applying FK", orphans);
                    }
                }

                // 4. Ajouter UNIQUE sur formes.itemname si absent (prérequis de la FK).
                //    On vérifie d'abord l'absence de doublons : sinon on log et on
                //    abandonne sans bloquer le démarrage de l'application.
                using (var checkUnique = conn.CreateCommand())
                {
                    checkUnique.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'formes' AND INDEX_NAME = 'UK_Formes_ItemName'";
                    var hasIndex = Convert.ToInt32(await checkUnique.ExecuteScalarAsync()) > 0;
                    if (!hasIndex)
                    {
                        using var checkDup = conn.CreateCommand();
                        checkDup.CommandText = "SELECT COUNT(*) FROM (SELECT itemname FROM formes GROUP BY itemname HAVING COUNT(*) > 1) AS d";
                        var duplicates = Convert.ToInt32(await checkDup.ExecuteScalarAsync());
                        if (duplicates > 0)
                        {
                            Log.Warning("Cannot add UNIQUE on formes.itemname: {Count} duplicate value(s) detected. Resolve manually then restart.", duplicates);
                        }
                        else
                        {
                            using var addUnique = conn.CreateCommand();
                            addUnique.CommandText = "CREATE UNIQUE INDEX UK_Formes_ItemName ON formes(itemname)";
                            await addUnique.ExecuteNonQueryAsync();
                            Log.Information("Added UNIQUE index UK_Formes_ItemName on formes(itemname)");
                        }
                    }
                }

                // 5. Ajouter la FK stricte si absente (et si UNIQUE est en place).
                using (var checkFk = conn.CreateCommand())
                {
                    checkFk.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = DATABASE() AND CONSTRAINT_NAME = 'FK_Poso_Forme'";
                    var hasFk = Convert.ToInt32(await checkFk.ExecuteScalarAsync()) > 0;
                    if (!hasFk)
                    {
                        using var checkUniqueReady = conn.CreateCommand();
                        checkUniqueReady.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'formes' AND INDEX_NAME = 'UK_Formes_ItemName'";
                        var uniqueReady = Convert.ToInt32(await checkUniqueReady.ExecuteScalarAsync()) > 0;
                        if (uniqueReady)
                        {
                            using var addFk = conn.CreateCommand();
                            addFk.CommandText = "ALTER TABLE poso ADD CONSTRAINT FK_Poso_Forme FOREIGN KEY (posoform) REFERENCES formes(itemname) ON UPDATE CASCADE ON DELETE SET NULL";
                            await addFk.ExecuteNonQueryAsync();
                            Log.Information("Added FK FK_Poso_Forme: poso.posoform -> formes.itemname (ON UPDATE CASCADE, ON DELETE SET NULL)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Poso ↔ Formes FK migration failed (non-fatal)");
            }

            // Ré-introduction de formes.posoform (sémantique nouvelle : abréviation
            // libre saisie par l'admin, servant à l'auto-génération de la dénomination
            // de posologie côté dialogue Médicament — ex. « appl. » pour Pommade).
            try
            {
                using var checkCol = conn.CreateCommand();
                checkCol.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'formes' AND COLUMN_NAME = 'posoform'";
                var exists = Convert.ToInt32(await checkCol.ExecuteScalarAsync()) > 0;
                if (!exists)
                {
                    using var addCol = conn.CreateCommand();
                    addCol.CommandText = "ALTER TABLE formes ADD COLUMN posoform VARCHAR(50) NULL";
                    await addCol.ExecuteNonQueryAsync();
                    Log.Information("Re-added formes.posoform (VARCHAR(50) NULL) — abréviation pour posologie auto-générée");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Re-add formes.posoform migration failed (non-fatal)");
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
