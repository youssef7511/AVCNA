using AVCNDB.WPF.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AVCNDB.WPF.Services;

public class DatabaseDiagnosticsService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IConfiguration _configuration;

    public DatabaseDiagnosticsService(IDbContextFactory<AppDbContext> contextFactory, IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _configuration = configuration;
    }

    public async Task LogDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var useRemoteDb = _configuration.GetValue<bool>("AppSettings:UseRemoteDatabase");
        var connectionName = useRemoteDb ? "RemoteConnection" : "DefaultConnection";
        var connectionString = _configuration.GetConnectionString(connectionName);
        Log.Information("DB connection string ({ConnectionName}): {ConnectionString}", connectionName, MaskPassword(connectionString));

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            Log.Information("EF provider: {ProviderName}", db.Database.ProviderName);

            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            Log.Information("DB CanConnect: {CanConnect}", canConnect);

            if (!canConnect)
            {
                return;
            }

            var databaseName = await db.Database.SqlQueryRaw<string>("SELECT DATABASE();")
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);
            Log.Information("DB current schema (SELECT DATABASE()): {DatabaseName}", databaseName);

            var version = await db.Database.SqlQueryRaw<string>("SELECT VERSION();")
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);
            Log.Information("DB version (SELECT VERSION()): {Version}", version);

            var medicCount = await db.Medics.CountAsync(cancellationToken);
            var dciCount = await db.Dcis.CountAsync(cancellationToken);
            var labosCount = await db.Labos.CountAsync(cancellationToken);
            var familyCount = await db.Families.CountAsync(cancellationToken);

            Log.Information(
                "DB counts => medic: {MedicCount}, dci: {DciCount}, labos: {LabosCount}, family: {FamilyCount}",
                medicCount,
                dciCount,
                labosCount,
                familyCount);

            var sample = await db.Medics.AsNoTracking()
                .OrderBy(m => m.recordid)
                .Select(m => new { m.recordid, m.itemname, m.barcode, m.isactive })
                .Take(3)
                .ToListAsync(cancellationToken);
            Log.Information("DB medic sample (first 3): {@Sample}", sample);

            if (databaseName != null && medicCount == 0 && dciCount == 0 && labosCount == 0 && familyCount == 0)
            {
                var tableNames = new[] { "medic", "dci", "labos", "family", "formes", "interact", "presents", "stock", "voie" };
                foreach (var t in tableNames)
                {
                    var exists = await db.Database.SqlQueryRaw<int>(
                            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = {0} AND TABLE_NAME = {1};",
                            databaseName,
                            t)
                        .FirstOrDefaultAsync(cancellationToken);

                    Log.Information("DB table exists check => {Table}: {Exists}", t, exists > 0);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DB diagnostics failed");
        }
    }

    private static string? MaskPassword(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        try
        {
            var builder = new global::MySqlConnector.MySqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrEmpty(builder.Password))
            {
                builder.Password = "***";
            }

            return builder.ConnectionString;
        }
        catch
        {
            return connectionString;
        }
    }
}
