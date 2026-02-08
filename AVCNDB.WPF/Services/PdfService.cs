using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Service de génération de rapports PDF
/// </summary>
public class PdfService : IPdfService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public PdfService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        
        // Configuration de la licence QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task GenerateMedicReportAsync(int medicId, string outputPath)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var medic = await context.Medics.FindAsync(medicId);
        if (medic == null)
            throw new InvalidOperationException($"Médicament introuvable (ID: {medicId})");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Segoe UI"));

                page.Header().Element(c => ComposeHeader(c, "Fiche Médicament"));
                page.Content().Element(c => ComposeMedicContent(c, medic));
                page.Footer().Element(ComposeFooter);
            });
        });

        await Task.Run(() => document.GeneratePdf(outputPath));
    }

    public async Task GenerateMedicListReportAsync(IEnumerable<int> medicIds, string outputPath)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var medics = await context.Medics
            .Where(m => medicIds.Contains(m.recordid))
            .ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                page.Header().Element(c => ComposeHeader(c, "Liste des Médicaments"));
                page.Content().Element(c => ComposeMedicListContent(c, medics));
                page.Footer().Element(ComposeFooter);
            });
        });

        await Task.Run(() => document.GeneratePdf(outputPath));
    }

    public async Task GenerateStockReportAsync(string outputPath, bool includeAlerts = true)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var stocks = await context.Stocks.ToListAsync();
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                page.Header().Element(c => ComposeHeader(c, "Rapport de Stock"));
                page.Content().Element(c => ComposeStockContent(c, stocks, includeAlerts));
                page.Footer().Element(ComposeFooter);
            });
        });

        await Task.Run(() => document.GeneratePdf(outputPath));
    }

    public async Task GenerateInteractionReportAsync(IEnumerable<string> dciNames, string outputPath)
    {
        var dciList = dciNames.ToList();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var interactions = await context.Interacts
            .Where(i => dciList.Contains(i.dci1) || dciList.Contains(i.dci2))
            .ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                page.Header().Element(c => ComposeHeader(c, "Rapport d'Interactions Médicamenteuses"));
                page.Content().Element(c => ComposeInteractionContent(c, interactions, dciList));
                page.Footer().Element(ComposeFooter);
            });
        });

        await Task.Run(() => document.GeneratePdf(outputPath));
    }

    public async Task GenerateCustomReportAsync<T>(IEnumerable<T> data, string title, string outputPath) where T : class
    {
        var dataList = data.ToList();
        var properties = typeof(T).GetProperties().Where(p => p.CanRead).Take(8).ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                page.Header().Element(c => ComposeHeader(c, title));
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in properties)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        // En-têtes
                        table.Header(header =>
                        {
                            foreach (var prop in properties)
                            {
                                header.Cell().Background("#1976D2").Padding(5)
                                    .Text(prop.Name).FontColor("#FFFFFF").Bold();
                            }
                        });

                        // Données
                        foreach (var item in dataList)
                        {
                            foreach (var prop in properties)
                            {
                                var value = prop.GetValue(item)?.ToString() ?? "";
                                table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(5)
                                    .Text(value);
                            }
                        }
                    });
                });
                page.Footer().Element(ComposeFooter);
            });
        });

        await Task.Run(() => document.GeneratePdf(outputPath));
    }

    public async Task<byte[]> GenerateMedicReportToBytesAsync(int medicId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var medic = await context.Medics.FindAsync(medicId);
        if (medic == null) return Array.Empty<byte>();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Segoe UI"));

                page.Header().Element(c => ComposeHeader(c, "Fiche Médicament"));
                page.Content().Element(c => ComposeMedicContent(c, medic));
                page.Footer().Element(ComposeFooter);
            });
        });

        return await Task.Run(() => document.GeneratePdf());
    }

    // ============================================
    // MÉTHODES PRIVÉES DE COMPOSITION
    // ============================================

    private static void ComposeHeader(IContainer container, string title)
    {
        container.PaddingBottom(20).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("AVICENNA DB")
                    .FontSize(20).Bold().FontColor("#1976D2");
                col.Item().Text(title)
                    .FontSize(14).FontColor("#757575");
            });

            row.ConstantItem(120).Column(col =>
            {
                col.Item().AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy"))
                    .FontSize(10).FontColor("#757575");
                col.Item().AlignRight().Text(DateTime.Now.ToString("HH:mm"))
                    .FontSize(10).FontColor("#757575");
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().AlignLeft().Text("AVCNDB - Base de données médicamenteuse")
                .FontSize(8).FontColor("#9E9E9E");
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(8).FontColor("#9E9E9E");
                text.CurrentPageNumber().FontSize(8).FontColor("#9E9E9E");
                text.Span(" / ").FontSize(8).FontColor("#9E9E9E");
                text.TotalPages().FontSize(8).FontColor("#9E9E9E");
            });
        });
    }

    private static void ComposeMedicContent(IContainer container, Medic medic)
    {
        container.Column(col =>
        {
            // Nom du médicament
            col.Item().Background("#1976D2").Padding(15)
                .Text(medic.itemname)
                .FontSize(18).Bold().FontColor("#FFFFFF");

            col.Item().PaddingTop(10);

            // DCI
            col.Item().Row(row =>
            {
                row.ConstantItem(120).Text("DCI :").Bold();
                row.RelativeItem().Text(medic.dci ?? "-");
            });

            // Forme
            col.Item().Row(row =>
            {
                row.ConstantItem(120).Text("Forme :").Bold();
                row.RelativeItem().Text(medic.forme ?? "-");
            });

            // Voie
            col.Item().Row(row =>
            {
                row.ConstantItem(120).Text("Voie :").Bold();
                row.RelativeItem().Text(medic.voie ?? "-");
            });

            // Présentation
            col.Item().Row(row =>
            {
                row.ConstantItem(120).Text("Présentation :").Bold();
                row.RelativeItem().Text(medic.present ?? "-");
            });

            // Laboratoire
            col.Item().Row(row =>
            {
                row.ConstantItem(120).Text("Laboratoire :").Bold();
                row.RelativeItem().Text(medic.labo ?? "-");
            });

            col.Item().PaddingTop(15);
            col.Item().LineHorizontal(1).LineColor("#E0E0E0");
            col.Item().PaddingTop(15);

            // Prix
            col.Item().Background("#E3F2FD").Padding(10).Row(row =>
            {
                row.RelativeItem().Text("Prix Public :").Bold();
                row.ConstantItem(100).AlignRight()
                    .Text($"{medic.price / 1000.0:N3} DT")
                    .FontSize(14).Bold().FontColor("#FF5722");
            });

            // Indication si disponible
            if (!string.IsNullOrEmpty(medic.indication))
            {
                col.Item().PaddingTop(15);
                col.Item().Text("Indication :").Bold();
                col.Item().PaddingTop(5).Text(medic.indication);
            }
        });
    }

    private static void ComposeMedicListContent(IContainer container, List<Medic> medics)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(200); // Nom
                columns.RelativeColumn(2);   // DCI
                columns.RelativeColumn(1);   // Forme
                columns.RelativeColumn(1);   // Labo
                columns.ConstantColumn(80);  // Prix
            });

            // En-têtes
            table.Header(header =>
            {
                header.Cell().Background("#1976D2").Padding(8)
                    .Text("Nom").FontColor("#FFFFFF").Bold();
                header.Cell().Background("#1976D2").Padding(8)
                    .Text("DCI").FontColor("#FFFFFF").Bold();
                header.Cell().Background("#1976D2").Padding(8)
                    .Text("Forme").FontColor("#FFFFFF").Bold();
                header.Cell().Background("#1976D2").Padding(8)
                    .Text("Laboratoire").FontColor("#FFFFFF").Bold();
                header.Cell().Background("#1976D2").Padding(8)
                    .Text("Prix (DT)").FontColor("#FFFFFF").Bold();
            });

            // Données
            var isAlternate = false;
            foreach (var medic in medics)
            {
                var bgColor = isAlternate ? "#F5F5F5" : "#FFFFFF";

                table.Cell().Background(bgColor).Padding(6).Text(medic.itemname);
                table.Cell().Background(bgColor).Padding(6).Text(medic.dci ?? "-");
                table.Cell().Background(bgColor).Padding(6).Text(medic.forme ?? "-");
                table.Cell().Background(bgColor).Padding(6).Text(medic.labo ?? "-");
                table.Cell().Background(bgColor).Padding(6).AlignRight()
                    .Text($"{medic.price / 1000.0:N3}");

                isAlternate = !isAlternate;
            }
        });
    }

    private static void ComposeStockContent(IContainer container, List<Stock> stocks, bool includeAlerts)
    {
        container.Column(col =>
        {
            if (includeAlerts)
            {
                var alertStocks = stocks.Where(s => s.quantity < s.minstock).ToList();
                if (alertStocks.Any())
                {
                    col.Item().Background("#FFEBEE").Padding(10).Column(alertCol =>
                    {
                        alertCol.Item().Text("⚠ Alertes de Stock Bas")
                            .FontSize(14).Bold().FontColor("#F44336");
                        alertCol.Item().PaddingTop(5)
                            .Text($"{alertStocks.Count} médicament(s) en rupture ou en dessous du seuil minimum");
                    });
                    col.Item().PaddingTop(15);
                }
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); // Médicament
                    columns.ConstantColumn(80); // Stock
                    columns.ConstantColumn(80); // Min
                    columns.ConstantColumn(100); // Expiration
                    columns.ConstantColumn(80); // Statut
                });

                table.Header(header =>
                {
                    header.Cell().Background("#1976D2").Padding(6)
                        .Text("Médicament").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#1976D2").Padding(6)
                        .Text("Stock").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#1976D2").Padding(6)
                        .Text("Min").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#1976D2").Padding(6)
                        .Text("Expiration").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#1976D2").Padding(6)
                        .Text("Statut").FontColor("#FFFFFF").Bold();
                });

                foreach (var stock in stocks)
                {
                    var bgColor = stock.quantity < stock.minstock ? "#FFEBEE" : "#FFFFFF";
                    var statusColor = stock.quantity < stock.minstock ? "#F44336" : "#4CAF50";
                    var status = stock.quantity < stock.minstock ? "Alerte" : "OK";

                    table.Cell().Background(bgColor).Padding(5).Text(stock.medicname);
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text(stock.quantity.ToString());
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text(stock.minstock.ToString());
                    table.Cell().Background(bgColor).Padding(5)
                        .Text(stock.expirydate?.ToString("dd/MM/yyyy") ?? "-");
                    table.Cell().Background(bgColor).Padding(5)
                        .Text(status).FontColor(statusColor).Bold();
                }
            });
        });
    }

    private static void ComposeInteractionContent(IContainer container, List<Interact> interactions, List<string> dciList)
    {
        container.Column(col =>
        {
            col.Item().Text("DCI analysées :").Bold();
            col.Item().Text(string.Join(", ", dciList));
            col.Item().PaddingTop(15);

            if (!interactions.Any())
            {
                col.Item().Background("#E8F5E9").Padding(15)
                    .Text("Aucune interaction trouvée entre les DCI sélectionnées.")
                    .FontColor("#4CAF50");
            }
            else
            {
                foreach (var interaction in interactions)
                {
                    var levelColor = interaction.level?.ToLower() switch
                    {
                        "contre-indication" => "#F44336",
                        "association déconseillée" => "#FF9800",
                        "précaution d'emploi" => "#FFC107",
                        _ => "#2196F3"
                    };

                    col.Item().Border(1).BorderColor("#E0E0E0").Padding(10).Column(intCol =>
                    {
                        intCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{interaction.dci1} + {interaction.dci2}").Bold();
                            row.ConstantItem(150).AlignRight()
                                .Text(interaction.level ?? "Non spécifié")
                                .FontColor(levelColor).Bold();
                        });
                        if (!string.IsNullOrEmpty(interaction.description))
                        {
                            intCol.Item().PaddingTop(5).Text(interaction.description);
                        }
                        if (!string.IsNullOrEmpty(interaction.conduite))
                        {
                            intCol.Item().PaddingTop(5).Text($"Conduite : {interaction.conduite}")
                                .FontColor("#757575");
                        }
                    });
                    col.Item().PaddingTop(8);
                }
            }
        });
    }
}
