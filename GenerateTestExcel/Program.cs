using ClosedXML.Excel;

// ============================================================
// Generates test_fuzzy_detection.xlsx for manual testing of the
// Mouvement / Edition File page fuzzy detection pipeline.
//
// Each row is annotated with what the fuzzy service SHOULD detect.
// ============================================================

var wb = new XLWorkbook();
var ws = wb.AddWorksheet("Edition Test");

// ── Headers (matching EditionFileService BuildColumnMap aliases) ──
var headers = new[]
{
    "Code PCT", "Dénomination", "D.C.I", "DCI-Association",
    "Forme", "Tab.", "VEiC", "Labo.", "Famille",
    "Spécialité", "Voie", "Prix-Réf", "Prix", "Remb.", "A.P."
};

for (int i = 0; i < headers.Length; i++)
{
    var cell = ws.Cell(1, i + 1);
    cell.Value = headers[i];
    cell.Style.Font.Bold = true;
    cell.Style.Fill.BackgroundColor = XLColor.CornflowerBlue;
    cell.Style.Font.FontColor = XLColor.White;
}

// ── Row data ──
// Columns: PctCode, Denomination, DCI, DCI-Assoc, Forme, Tab, VEiC, Labo, Famille, Specialite, Voie, PrixRef, Prix, Remb, AP
var rows = new object[][]
{
    // ── ROW 1: ALL CORRECT — exact matches from DB library tables ──
    new object[] { "PCT001", "Doliprane 500mg Comprimé Boîte de 20",
        "PAR", "", "Comprimé", "A", "", "Sanofi", "Antalgiques",
        "Cardiologie", "Orale", 1200, 3500, 1, 0 },

    // ── ROW 2: ALL CORRECT — another valid combination ──
    new object[] { "PCT002", "Advil 200mg Comprimé Boîte de 20",
        "IBU", "", "Comprimé", "A", "", "Pfizer", "Anti-inflammatoires",
        "Dermatologie", "Orale", 0, 5500, 0, 0 },

    // ── ROW 3: TYPO DCI — "PARACETAMOL" (uppercase no accent → score ~91, KNOWN) ──
    new object[] { "PCT003", "Efferalgan 1000mg Comprimé Boîte de 8",
        "PARACETAMOL", "", "Comprimé", "A", "", "Sanofi", "Antalgiques",
        "Cardiologie", "Orale", 1500, 4200, 1, 0 },

    // ── ROW 4: TYPO LABO — "Sanofii" (extra letter → score ~92, KNOWN) ──
    new object[] { "PCT004", "Doliprane 1000mg Sachet Boîte de 8",
        "PAR", "", "Sachet dose", "A", "", "Sanofii", "Antalgiques",
        "Pneumologie", "Orale", 1200, 4500, 1, 0 },

    // ── ROW 5: UNKNOWN LABO — "BioNTech" (not in library → UNKNOWN) ──
    new object[] { "PCT005", "Comirnaty 30µg Injectable Dose unique",
        "MTZ", "", "Injectable", "", "", "BioNTech", "Antiviraux",
        "Pneumologie", "Intramusculaire", 0, 0, 0, 1 },

    // ── ROW 6: UNKNOWN DCI — "Xylozantrin" (invented → UNKNOWN) ──
    new object[] { "PCT006", "Xylozantrin 250mg Gélule Boîte de 10",
        "Xylozantrin", "", "Gélule", "C", "", "Novartis", "Antibiotiques",
        "Cardiologie", "Orale", 3000, 8500, 1, 1 },

    // ── ROW 7: UNKNOWN FORME — "Patch cutané" ≠ "Patch" (DB has "Patch") ──
    new object[] { "PCT007", "Nicotinell 14mg/24h Patch 7",
        "LOS", "", "Patch cutané", "", "", "GSK", "Dermatologie",
        "Dermatologie", "Transdermique", 5000, 12000, 0, 0 },

    // ── ROW 8: MULTIPLE UNKNOWNS — fake DCI + fake Labo + fake Famille ──
    new object[] { "PCT008", "FakeMed 500mg Comprimé Boîte de 30",
        "Flurbiproflex", "", "Comprimé", "B", "", "PharmaCorp Intl.",
        "Rhumatologie", "Endocrinologie", "Orale", 2000, 6500, 1, 0 },

    // ── ROW 9: CLOSE TYPO — "Comprime" (no accent → KNOWN), "Antihistaminniques" (double n → borderline) ──
    new object[] { "PCT009", "Zyrtec 10mg Comprimé Boîte de 7",
        "LOR", "", "Comprime", "", "", "Johnson & Johnson",
        "Antihistaminniques", "Pneumologie", "Orale", 0, 7200, 0, 0 },

    // ── ROW 10: PRICE CHANGE test — different prix than DB for same data pattern ──
    new object[] { "PCT010", "Metformine 500mg Comprimé Boîte de 30",
        "MET", "", "Comprimé", "D", "", "Merck", "Antidiabétiques",
        "Endocrinologie", "Orale", 0, 9999, 1, 0 },

    // ── ROW 11: EMPTY DCI — empty = treated as KNOWN (no false positive) ──
    new object[] { "PCT011", "Bétadine 10% Solution Flacon 125ml",
        "", "", "Sirop", "", "", "Sanofi", "Dermatologie",
        "Dermatologie", "Topique", 0, 3200, 0, 0 },

    // ── ROW 12: UNKNOWN VOIE — "Intrapéritonéale" (not in voie table → UNKNOWN) ──
    new object[] { "PCT012", "Dialysat PD4 Solution 2L",
        "CT", "", "Injectable", "", "", "Roche", "Gastro-protecteurs",
        "Cardiologie", "Intrapéritonéale", 0, 45000, 1, 1 },

    // ── ROW 13: UNKNOWN SPECIALITÉ — "Ophtalmologie" (not in specialites → UNKNOWN) ──
    new object[] { "PCT013", "Tropicamide 0.5% Collyre Flacon 5ml",
        "AT", "", "Collyre", "", "", "Novartis", "Vitamines",
        "Ophtalmologie", "Ophtalmique", 0, 6800, 0, 0 },

    // ── ROW 14: CLOSE TYPOS — "Comprimee" (→ ~94 KNOWN), "Pfiser" (→ ~83 KNOWN), "Antalgique" no s (→ ~97 KNOWN) ──
    new object[] { "PCT014", "Aspirine 500mg Comprimé Boîte de 20",
        "AS", "", "Comprimee", "", "", "Pfiser", "Antalgique",
        "Cardiologie", "Orale", 0, 4100, 1, 0 },

    // ── ROW 15: ALL UNKNOWN — completely invented data (DCI, DCI-Assoc, Forme, Labo, Famille, Specialite, Voie) ──
    new object[] { "PCT015", "Produit Inventé 999mg",
        "Zorbitex", "Monazine", "Nanopill", "Z", "", "InventedLab SA",
        "Neurochirurgie", "Robotique", "Intracérébrale", 10000, 99999, 1, 1 },
};

for (int r = 0; r < rows.Length; r++)
{
    var data = rows[r];
    int rowNum = r + 2;
    for (int c = 0; c < data.Length; c++)
    {
        ws.Cell(rowNum, c + 1).Value = data[c] switch
        {
            int n => n,
            string s => s,
            _ => data[c]?.ToString() ?? ""
        };
    }
}

// ── Add a "LEGEND" sheet explaining each row ──
var legend = wb.AddWorksheet("Légende - Résultats attendus");
legend.Cell(1, 1).Value = "Ligne";
legend.Cell(1, 2).Value = "Scénario de test";
legend.Cell(1, 3).Value = "Champs attendus INCONNUS (bleu gras)";
legend.Cell(1, 4).Value = "Champs attendus CONNUS (normaux)";
legend.Row(1).Style.Font.Bold = true;
legend.Row(1).Style.Fill.BackgroundColor = XLColor.LightGreen;

var legendData = new[]
{
    new[] { "1",  "Tout correct (valeurs exactes DB)",          "Aucun",                                         "Tous" },
    new[] { "2",  "Tout correct (autre combinaison)",           "Aucun",                                         "Tous" },
    new[] { "3",  "DCI en majuscules sans accent",              "Aucun (score ~91)",                              "DCI → PAR matched via fuzzy" },
    new[] { "4",  "Labo avec typo «Sanofii»",                  "Aucun (score ~92)",                              "Labo → Sanofi matched via fuzzy" },
    new[] { "5",  "Labo inconnu «BioNTech»",                   "Labo",                                          "DCI, Forme, Famille, Voie" },
    new[] { "6",  "DCI inventé «Xylozantrin»",                 "Dci",                                           "Forme, Labo, Famille" },
    new[] { "7",  "Forme «Patch cutané» vs DB «Patch»",        "Forme (score dépend de la longueur)",            "DCI, Labo, Famille, Voie" },
    new[] { "8",  "Multiple inconnus (DCI+Labo+Famille)",       "Dci, Labo, Fam1",                               "Forme, Specialite, Voie" },
    new[] { "9",  "Typo «Comprime» + «Antihistaminniques»",    "Fam1 possible (score ~95 proche seuil)",         "Forme → Comprimé (score ~94)" },
    new[] { "10", "Changement de prix (prix différent)",        "Aucun",                                         "Tous — ligne marquée prix modifié" },
    new[] { "11", "DCI vide (champ optionnel)",                 "Aucun (vide = connu par design)",                "Tous" },
    new[] { "12", "Voie «Intrapéritonéale» (pas en DB)",        "Voie",                                          "DCI, Forme, Labo, Famille" },
    new[] { "13", "Spécialité «Ophtalmologie» (pas en DB)",     "Specialite",                                    "DCI, Forme, Labo, Famille, Voie" },
    new[] { "14", "Typos proches: Comprimee/Pfiser/Antalgique", "Aucun ou Forme (score dépend du seuil 80)",     "La plupart → corrigés par fuzzy" },
    new[] { "15", "TOUT inventé — stress test",                 "Dci, DciAssociation, Forme, Labo, Fam1, Specialite, Voie", "Aucun" },
};

for (int i = 0; i < legendData.Length; i++)
{
    for (int c = 0; c < legendData[i].Length; c++)
    {
        legend.Cell(i + 2, c + 1).Value = legendData[i][c];
    }
}

legend.Columns().AdjustToContents();
ws.Columns().AdjustToContents();

// Save next to the WPF project
var outputPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "test_fuzzy_detection.xlsx");
outputPath = Path.GetFullPath(outputPath);
wb.SaveAs(outputPath);
Console.WriteLine($"DONE → {outputPath}");
