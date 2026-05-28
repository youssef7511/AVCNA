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

    // ============================================
    // AUTHENTIFICATION
    // ============================================
    public DbSet<User> Users { get; set; } = null!;

    // ============================================
    // FICHIER D'ÉDITION
    // ============================================
    public DbSet<EditionFileSession> EditionFileSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================
        // GLOBAL QUERY FILTERS (soft-delete)
        // ============================================
        modelBuilder.Entity<Medic>()
            .HasQueryFilter(m => m.deletedat == null);
        modelBuilder.Entity<Dci>()
            .HasQueryFilter(e => e.deletedat == null);
        modelBuilder.Entity<Families>()
            .HasQueryFilter(e => e.deletedat == null);
        modelBuilder.Entity<Labos>()
            .HasQueryFilter(e => e.deletedat == null);
        modelBuilder.Entity<Formes>()
            .HasQueryFilter(e => e.deletedat == null);
        modelBuilder.Entity<Voies>()
            .HasQueryFilter(e => e.deletedat == null);
        modelBuilder.Entity<Specialites>()
            .HasQueryFilter(e => e.deletedat == null);
        modelBuilder.Entity<Presents>()
            .HasQueryFilter(e => e.deletedat == null);

        // ============================================
        // FOREIGN KEY RELATIONSHIPS
        // ============================================
        modelBuilder.Entity<Stock>()
            .HasIndex(s => s.medicid)
            .HasDatabaseName("IX_Stock_MedicId");

        modelBuilder.Entity<Cilist>()
            .HasIndex(c => c.medicid)
            .HasDatabaseName("IX_Cilist_MedicId");

        modelBuilder.Entity<Specmedic>()
            .HasIndex(s => s.medicid)
            .HasDatabaseName("IX_Specmedic_MedicId");

        modelBuilder.Entity<Localites>()
            .HasIndex(l => l.gouvernid)
            .HasDatabaseName("IX_Localites_GouvernId");

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
        // FK STRICTE : Poso.posoform → Formes.itemname
        // ============================================
        // Seule relation à clé étrangère stricte du modèle : tous les autres liens
        // medic ↔ tables de référence sont logiques (par dénomination) et gérés
        // par MedicSyncService. Ici, l'organisme exige que la cohérence soit
        // garantie au niveau base de données. Voir migration dans App.xaml.cs.
        modelBuilder.Entity<Formes>()
            .HasIndex(f => f.itemname)
            .IsUnique()
            .HasDatabaseName("UK_Formes_ItemName");

        modelBuilder.Entity<Poso>()
            .HasOne<Formes>()
            .WithMany()
            .HasPrincipalKey(f => f.itemname)
            .HasForeignKey(p => p.posoform)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_Poso_Forme");

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

        // ============================================
        // FICHIER D'ÉDITION — INDEX
        // ============================================
        modelBuilder.Entity<EditionFileSession>()
            .HasIndex(e => e.status)
            .HasDatabaseName("IX_EditionFileSession_Status");

        modelBuilder.Entity<EditionFileSession>()
            .HasIndex(e => e.addedat)
            .HasDatabaseName("IX_EditionFileSession_AddedAt");
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
