using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using CI.Data;
using CI.Models;

namespace CI.Services
{
    public class TargetExportService
    {
        private readonly AppDbContext _db;
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public TargetExportService(AppDbContext db)
        {
            _db = db;
        }

        private static readonly Dictionary<Mois, string> MoisToEnglish = new()
        {
            [Mois.Janvier] = "January",
            [Mois.Fevrier] = "February",
            [Mois.Mars] = "March",
            [Mois.Avril] = "April",
            [Mois.Mai] = "May",
            [Mois.Juin] = "June",
            [Mois.Juillet] = "July",
            [Mois.Aout] = "August",
            [Mois.Septembre] = "September",
            [Mois.Octobre] = "October",
            [Mois.Novembre] = "November",
            [Mois.Decembre] = "December"
        };

        public async Task<ExportResult> ExportSelectedAsync(string filePath, List<int> suggestionIds, string sheetName = "Morocco")
        {
            await _fileLock.WaitAsync();
            try
            {
                var result = new ExportResult();

                if (!File.Exists(filePath))
                {
                    result.Errors.Add($"Fichier introuvable : {filePath}");
                    return result;
                }

                if (suggestionIds == null || suggestionIds.Count == 0)
                {
                    result.Errors.Add("Aucune suggestion sélectionnée.");
                    return result;
                }

                var suggestions = await _db.Suggestions
                    .Include(s => s.Scope)
                    .Include(s => s.Departement)
                    .Where(s => suggestionIds.Contains(s.Id))
                    .ToListAsync();

                XLWorkbook workbook;
                try
                {
                    workbook = new XLWorkbook(filePath);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Impossible d'ouvrir le fichier cible : {ex.Message}");
                    return result;
                }

                using (workbook)
                {
                    if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var ws))
                    {
                        result.Errors.Add($"Feuille '{sheetName}' introuvable dans le fichier cible.");
                        return result;
                    }

                    int headerRowNum = -1;
                    for (int r = 1; r <= 10; r++)
                    {
                        var row = ws.Row(r);
                        if (row.CellsUsed().Any(c => NormalizeHeader(c.GetString()) == "suggestion number"))
                        {
                            headerRowNum = r;
                            break;
                        }
                    }

                    if (headerRowNum == -1)
                    {
                        result.Errors.Add("Impossible de localiser la ligne d'en-têtes (colonne 'Suggestion Number' introuvable).");
                        return result;
                    }

                    var headerRow = ws.Row(headerRowNum);
                    var colIndex = new Dictionary<string, int>();
                    foreach (var cell in headerRow.CellsUsed())
                    {
                        var name = NormalizeHeader(cell.GetString());
                        if (!string.IsNullOrEmpty(name)) colIndex[name] = cell.Address.ColumnNumber;
                    }

                    int Col(string label)
                    {
                        var normalized = NormalizeHeader(label);
                        if (colIndex.TryGetValue(normalized, out var exact)) return exact;
                        var partial = colIndex.FirstOrDefault(kvp => kvp.Key.Contains(normalized) || normalized.Contains(kvp.Key));
                        return partial.Key != null ? partial.Value : -1;
                    }

                    int cPlant = Col("Plant");
                    int cProjectNum = Col("Project Number");
                    int cSuggNum = Col("Suggestion Number");
                    int cRegisterDate = Col("Register Date");
                    int cRegisterMonth = Col("Register Month");
                    int cImprovementType = Col("Improvement Type");
                    int cPlannedRandom = Col("Planned/Random");
                    int cImplArea = Col("Implementation Area");
                    int cAttackedLosses = Col("Attacked Losses");
                    int cExplanation = Col("Project Explanation");
                    int cLeader = Col("Proje Leader");
                    int cPlannedClosureDate = Col("Planned Closure Date");
                    int cProjectClosureDate = Col("Project Closure Date");
                    int cVariableOverhead = Col("Variable Overhead");
                    int cClosureStatus = Col("Closure Status");
                    int cDepartement = Col("Project Leader Department");

                    var missing = new[]
                    {
                        (cPlant, "Plant"), (cProjectNum, "Project Number"), (cSuggNum, "Suggestion Number"),
                        (cRegisterDate, "Register Date"), (cRegisterMonth, "Register Month"),
                        (cImprovementType, "Improvement Type"), (cPlannedRandom, "Planned/Random"),
                        (cImplArea, "Implementation Area"), (cAttackedLosses, "Attacked Losses"),
                        (cExplanation, "Project Explanation"), (cLeader, "Proje Leader"),
                        (cPlannedClosureDate, "Planned Closure Date"), (cProjectClosureDate, "Project Closure Date"),
                        (cVariableOverhead, "Variable Overhead"), (cClosureStatus, "Closure Status"), (cDepartement, "Project Leader Department")
                    }.Where(x => x.Item1 < 1).Select(x => x.Item2).ToList();

                    if (missing.Any())
                    {
                        result.Errors.Add("Colonnes introuvables : " + string.Join(", ", missing));
                        return result;
                    }

                    // Une ligne est considérée "occupée" uniquement si "Project Explanation" contient du texte —
                    // fiable quel que soit le type de suggestion (Kaizen/A3/VAVE), contrairement au format du numéro de projet.
                    bool RowHasRealData(int row)
                    {
                        if (cExplanation < 1) return false;
                        var text = ws.Cell(row, cExplanation).GetString().Trim();
                        return !string.IsNullOrEmpty(text);
                    }

                    var scanLimit = ws.LastRowUsed()?.RowNumber() ?? headerRowNum;
                    int lastProjectNumber = 0;
                    int lastDataRow = headerRowNum;

                    for (int r = headerRowNum + 1; r <= scanLimit; r++)
                    {
                        if (RowHasRealData(r))
                            lastDataRow = r;

                        var val = ws.Cell(r, cProjectNum).GetString();
                        var match = Regex.Match(val, @"KZ-(\d+)", RegexOptions.IgnoreCase);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out var n) && n > lastProjectNumber)
                            lastProjectNumber = n;
                    }

                    int nextRow = lastDataRow + 1;

                    foreach (var s in suggestions)
                    {
                        try
                        {
                            int targetRow = nextRow;
                            lastProjectNumber++;

                            ws.Cell(targetRow, cPlant).Value = s.Plant;
                            ws.Cell(targetRow, cProjectNum).Value = $"KZ-{lastProjectNumber}";
                            ws.Cell(targetRow, cSuggNum).Value = s.NumeroSuggestion;

                            if (s.DateSuggestion.HasValue)
                                ws.Cell(targetRow, cRegisterDate).Value = s.DateSuggestion.Value;

                            ws.Cell(targetRow, cRegisterMonth).Value =
                                s.Mois.HasValue && MoisToEnglish.TryGetValue(s.Mois.Value, out var monthEn) ? monthEn : "";

                            ws.Cell(targetRow, cImprovementType).Value = "Quick Kaizen";
                            ws.Cell(targetRow, cPlannedRandom).Value = "Random";
                            ws.Cell(targetRow, cImplArea).Value = s.ZoneLigne;
                            ws.Cell(targetRow, cAttackedLosses).Value = s.Scope?.Nom ?? "";
                            ws.Cell(targetRow, cExplanation).Value = s.Description;
                            ws.Cell(targetRow, cLeader).Value = s.Responsable;
                            ws.Cell(targetRow, cDepartement).Value = s.Departement?.Nom ?? "";

                            if (s.DateMiseEnOeuvre.HasValue)
                            {
                                ws.Cell(targetRow, cPlannedClosureDate).Value = s.DateMiseEnOeuvre.Value;
                                ws.Cell(targetRow, cProjectClosureDate).Value = s.DateMiseEnOeuvre.Value;
                            }

                            ws.Cell(targetRow, cVariableOverhead).Value = s.Economies;

                            ws.Cell(targetRow, cClosureStatus).Value =
                                s.Statut == StatutInitiative.Applique ? "Completed" : "Ongoing";

                            s.ExporteVersGlobal = true;
                            s.DateExportGlobal = DateTime.Now;

                            result.Exported++;
                            nextRow++;
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"{s.NumeroSuggestion} : {ex.Message}");
                        }
                    }

                    try
                    {
                        workbook.Save();
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Impossible d'enregistrer le fichier cible : {ex.Message}");
                        return result;
                    }

                    await _db.SaveChangesAsync();
                    return result;
                }
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private static string NormalizeHeader(string raw) =>
            string.Join(" ", (raw ?? "").Replace("\n", " ").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    }
}