using Microsoft.EntityFrameworkCore;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.DAL;

/// <summary>
/// Contexte de base de données Entity Framework Core
/// Gère la connexion à MySQL/MariaDB et les DbSets
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // ============================================
    // MÉDICAMENTS & COMPOSITIONS
    // ============================================
    public DbSet<Medic> Medics { get; set; } = null!;
    public DbSet<Dci> Dcis { get; set; } = null!;
    public DbSet<Families> Families { get; set; } = null!;
    public DbSet<Labos> Labos { get; set; } = null!;
    public DbSet<Formes> Formes { get; set; } = null!;
    public DbSet<Voies> Voies { get; set; } = null!;
    public DbSet<Presents> Presents { get; set; } = null!;
    public DbSet<Poso> Posos { get; set; } = null!;

    // ============================================
    // SPÉCIALITÉS & CLASSIFICATIONS
    // ============================================
    public DbSet<Specialites> Specialites { get; set; } = null!;
    public DbSet<Specmedic> Specmedics { get; set; } = null!;
    public DbSet<Cim10> Cim10s { get; set; } = null!;
    public DbSet<Catveic> Catveics { get; set; } = null!;

    // ============================================
    // INTERACTIONS & CONTRE-INDICATIONS
    // ============================================
    public DbSet<Interact> Interacts { get; set; } = null!;
    public DbSet<Cilib> Cilibs { get; set; } = null!;
    public DbSet<Cilist> Cilists { get; set; } = null!;
    public DbSet<Citypes> Citypes { get; set; } = null!;

    // ============================================
    // GÉOGRAPHIE
    // ============================================
    public DbSet<Gouvern> Gouverns { get; set; } = null!;
    public DbSet<Localites> Localites { get; set; } = null!;

    // ============================================
    // PROFESSIONNELS DE SANTÉ
    // ============================================
    public DbSet<Drugstores> Drugstores { get; set; } = null!;
    public DbSet<Associates> Associates { get; set; } = null!;
    public DbSet<Biologists> Biologists { get; set; } = null!;
    public DbSet<Dentists> Dentists { get; set; } = null!;
    public DbSet<Radiologues> Radiologues { get; set; } = null!;

    // ============================================
    // STOCK
    // ============================================
    public DbSet<Stock> Stocks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================
        // CONFIGURATION DES INDEX
        // ============================================
        
        // Index sur Medic pour recherche rapide
        modelBuilder.Entity<Medic>()
            .HasIndex(m => m.itemname)
            .HasDatabaseName("IX_Medic_ItemName");
        
        modelBuilder.Entity<Medic>()
            .HasIndex(m => m.dci)
            .HasDatabaseName("IX_Medic_Dci");
        
        modelBuilder.Entity<Medic>()
            .HasIndex(m => m.barcode)
            .HasDatabaseName("IX_Medic_Barcode");
        
        modelBuilder.Entity<Medic>()
            .HasIndex(m => m.labo)
            .HasDatabaseName("IX_Medic_Labo");

        // Index sur DCI
        modelBuilder.Entity<Dci>()
            .HasIndex(d => d.itemname)
            .HasDatabaseName("IX_Dci_ItemName");

        // Index sur Families
        modelBuilder.Entity<Families>()
            .HasIndex(f => f.itemname)
            .HasDatabaseName("IX_Families_ItemName");

        // Index sur Labos
        modelBuilder.Entity<Labos>()
            .HasIndex(l => l.itemname)
            .HasDatabaseName("IX_Labos_ItemName");

        // Index sur Stock pour alertes
        modelBuilder.Entity<Stock>()
            .HasIndex(s => s.medicid)
            .HasDatabaseName("IX_Stock_MedicId");
        
        modelBuilder.Entity<Stock>()
            .HasIndex(s => s.expirydate)
            .HasDatabaseName("IX_Stock_ExpiryDate");

        // Index sur Interactions
        modelBuilder.Entity<Interact>()
            .HasIndex(i => i.dci1)
            .HasDatabaseName("IX_Interact_Dci1");
        
        modelBuilder.Entity<Interact>()
            .HasIndex(i => i.dci2)
            .HasDatabaseName("IX_Interact_Dci2");

        // ============================================
        // CONFIGURATION DES VALEURS PAR DÉFAUT
        // ============================================
        
        modelBuilder.Entity<Medic>()
            .Property(m => m.isactive)
            .HasDefaultValue(1);

        modelBuilder.Entity<Drugstores>()
            .Property(d => d.isactive)
            .HasDefaultValue(1);

        modelBuilder.Entity<Associates>()
            .Property(a => a.isactive)
            .HasDefaultValue(1);
    }

    /// <summary>
    /// Sauvegarde les changements avec mise à jour automatique des dates d'audit
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Sauvegarde les changements de manière asynchrone avec mise à jour des dates d'audit
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Met à jour automatiquement les timestamps d'audit
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<ITrackable>();
        var now = DateTime.Now;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.addedat = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.updatedat = now;
            }
        }
    }
}
