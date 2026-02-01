using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Models;
using Microsoft.EntityFrameworkCore;

namespace AVCNDB.WPF.Tests.Helpers;

/// <summary>
/// Factory pour créer des contextes de base de données en mémoire pour les tests
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Crée un nouveau contexte de base de données en mémoire
    /// </summary>
    public static AppDbContext CreateInMemoryContext(string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Crée un contexte avec des données de test préremplies
    /// </summary>
    public static AppDbContext CreateSeededContext()
    {
        var context = CreateInMemoryContext();
        SeedTestData(context);
        return context;
    }

    /// <summary>
    /// Remplit la base avec des données de test
    /// </summary>
    private static void SeedTestData(AppDbContext context)
    {
        // DCI
        var dcis = new List<Dci>
        {
            new() { recordid = 1, itemname = "Paracétamol" },
            new() { recordid = 2, itemname = "Ibuprofène" },
            new() { recordid = 3, itemname = "Amoxicilline" },
            new() { recordid = 4, itemname = "Acide clavulanique" },
            new() { recordid = 5, itemname = "Métformine" }
        };
        context.Dcis.AddRange(dcis);

        // Familles
        var families = new List<Families>
        {
            new() { recordid = 1, itemname = "Antalgiques" },
            new() { recordid = 2, itemname = "Anti-inflammatoires" },
            new() { recordid = 3, itemname = "Antibiotiques" },
            new() { recordid = 4, itemname = "Antidiabétiques" }
        };
        context.Families.AddRange(families);

        // Laboratoires
        var labos = new List<Labos>
        {
            new() { recordid = 1, itemname = "Sanofi", subvalue = "France" },
            new() { recordid = 2, itemname = "Pfizer", subvalue = "USA" },
            new() { recordid = 3, itemname = "Pharma 5", subvalue = "Maroc" }
        };
        context.Labos.AddRange(labos);

        // Formes
        var formes = new List<Formes>
        {
            new() { recordid = 1, itemname = "Comprimé" },
            new() { recordid = 2, itemname = "Gélule" },
            new() { recordid = 3, itemname = "Sirop" },
            new() { recordid = 4, itemname = "Injectable" }
        };
        context.Formes.AddRange(formes);

        // Médicaments
        var medics = new List<Medic>
        {
            new()
            {
                recordid = 1,
                itemname = "Doliprane 500mg",
                barcode = "3400935262585",
                dci1 = "Paracétamol",
                family = "Antalgiques",
                labo = "Sanofi",
                forme = "Comprimé",
                dose1 = "500mg",
                price = 1550,
                refprice = 1200,
                pediatric = 0
            },
            new()
            {
                recordid = 2,
                itemname = "Doliprane 1000mg",
                barcode = "3400935262592",
                dci1 = "Paracétamol",
                family = "Antalgiques",
                labo = "Sanofi",
                forme = "Comprimé",
                dose1 = "1000mg",
                price = 2200,
                refprice = 1800,
                pediatric = 0
            },
            new()
            {
                recordid = 3,
                itemname = "Advil 200mg",
                barcode = "3400936001234",
                dci1 = "Ibuprofène",
                family = "Anti-inflammatoires",
                labo = "Pfizer",
                forme = "Comprimé",
                dose1 = "200mg",
                price = 2850,
                pediatric = 0
            },
            new()
            {
                recordid = 4,
                itemname = "Amoxil 500mg",
                barcode = "3400936002345",
                dci1 = "Amoxicilline",
                family = "Antibiotiques",
                labo = "Pfizer",
                forme = "Gélule",
                dose1 = "500mg",
                price = 4500,
                pediatric = 0
            },
            new()
            {
                recordid = 5,
                itemname = "Augmentin 1g",
                barcode = "3400936003456",
                dci1 = "Amoxicilline",
                dci2 = "Acide clavulanique",
                family = "Antibiotiques",
                labo = "Sanofi",
                forme = "Comprimé",
                dose1 = "875mg",
                dose2 = "125mg",
                price = 8500,
                pediatric = 0
            }
        };
        context.Medics.AddRange(medics);

        // Interactions
        var interactions = new List<Interact>
        {
            new()
            {
                recordid = 1,
                dci1 = "Ibuprofène",
                dci2 = "Amoxicilline",
                level = "Modérée",
                mecanisme = "Additivité des effets"
            }
        };
        context.Interacts.AddRange(interactions);

        // Stock
        var stocks = new List<Stock>
        {
            new()
            {
                recordid = 1,
                medicid = 1,
                medicname = "Doliprane 500mg",
                quantity = 50,
                minstock = 20,
                batchno = "LOT001",
                expirydate = DateTime.Now.AddMonths(12)
            },
            new()
            {
                recordid = 2,
                medicid = 2,
                medicname = "Doliprane 1000mg",
                quantity = 5,  // Stock bas
                minstock = 20,
                batchno = "LOT002",
                expirydate = DateTime.Now.AddMonths(6)
            },
            new()
            {
                recordid = 3,
                medicid = 3,
                medicname = "Advil 200mg",
                quantity = 100,
                minstock = 30,
                batchno = "LOT003",
                expirydate = DateTime.Now.AddDays(20)  // Expire bientôt
            }
        };
        context.Stocks.AddRange(stocks);

        context.SaveChanges();
    }

    /// <summary>
    /// Crée un médicament de test
    /// </summary>
    public static Medic CreateTestMedic(int id = 1, string name = "Test Medic")
    {
        return new Medic
        {
            recordid = id,
            itemname = name,
            barcode = $"340093{id:D7}",
            dci1 = "Test DCI",
            family = "Test Family",
            labo = "Test Labo",
            forme = "Comprimé",
            dose1 = "500mg",
            price = 2500
        };
    }

    /// <summary>
    /// Crée une DCI de test
    /// </summary>
    public static Dci CreateTestDci(int id = 1, string name = "Test DCI")
    {
        return new Dci
        {
            recordid = id,
            itemname = name
        };
    }
}
