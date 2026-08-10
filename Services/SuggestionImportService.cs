using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using CI.Data;
using CI.Models;

namespace CI.Services
{
    public class SuggestionImportService
    {
        private readonly AppDbContext _db;

        public SuggestionImportService(AppDbContext db)
        {
            _db = db;
        }

        private static readonly Dictionary<string, Mois> MonthMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["january"] = Models.Mois.Janvier,
            ["jan"] = Models.Mois.Janvier,
            ["february"] = Models.Mois.Fevrier,
            ["feb"] = Models.Mois.Fevrier,
            ["march"] = Models.Mois.Mars,
            ["mar"] = Models.Mois.Mars,
            ["april"] = Models.Mois.Avril,
            ["apr"] = Models.Mois.Avril,
            ["may"] = Models.Mois.Mai,
            ["june"] = Models.Mois.Juin,
            ["jun"] = Models.Mois.Juin,
            ["july"] = Models.Mois.Juillet,
            ["jul"] = Models.Mois.Juillet,
            ["august"] = Models.Mois.Aout,
            ["aug"] = Models.Mois.Aout,
            ["september"] = Models.Mois.Septembre,
            ["sep"] = Models.Mois.Septembre,
            ["october"] = Models.Mois.Octobre,
            ["oct"] = Models.Mois.Octobre,
            ["november"] = Models.Mois.Novembre,
            ["nov"] = Models.Mois.Novembre,
            ["december"] = Models.Mois.Decembre,
            ["dec"] = Models.Mois.Decembre
        };

        private readonly Dictionary<string, Departement> _departementCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Scope> _scopeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Suggestion> _processedThisRun = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> ScopeRedirects = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Innovator"] = "Other",
            ["Laboratory"] = "Other"
        };

        public async Task<ImportResult> ImportFromFileAsync(string filePath, string sheetName = "Morocco")
        {
            var result = new ImportResult();

            if (!File.Exists(filePath))
            {
                result.Errors.Add($"Fichier introuvable : {filePath}");
                return result;
            }

            await LoadCachesAsync();
            _processedThisRun.Clear();

            XLWorkbook workbook;
            try
            {
                workbook = new XLWorkbook(filePath);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Impossible d'ouvrir le fichier : {ex.Message}");
                return result;
            }

            using (workbook)
            {
                if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var ws))
                {
                    result.Errors.Add($"Feuille '{sheetName}' introuvable dans le fichier.");
                    return result;
                }

                var headerRow = ws.Row(2);
                var colIndex = new Dictionary<string, int>();
                foreach (var cell in headerRow.CellsUsed())
                {
                    var name = NormalizeHeader(cell.GetString());
                    if (!string.IsNullOrEmpty(name)) colIndex[name] = cell.Address.ColumnNumber;
                }

                int Col(string headerLabel) =>
                    colIndex.TryGetValue(NormalizeHeader(headerLabel), out var c) ? c : -1;

                int cNo = Col("No.");
                int cPlant = Col("Plant");
                int cNum = Col("Suggestions Number");
                int cSuggDate = Col("Suggestion Date");
                int cScope = Col("Suggestion Scope");
                int cZone = Col("Implementation Area/Line");
                int cSituation = Col("Current Situation");
                int cDescription = Col("Suggestions Description");
                int cDepartement = Col("Departement");
                int cLeader = Col("Suggestion Leader");
                int cStatus = Col("Status");
                int cImplDate = Col("Implementation Date");
                int cSaving = Col("Saving €");
                int cKaizenNum = Col("Kaizen Number (If there is a profit)");
                int cMonth = Col("Month");
                int cKaizenYN = Col("Kaizen YES/NO");

                var lastRow = ws.LastRowUsed().RowNumber();

                for (int r = 3; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    result.TotalRowsInFile++;

                    string numero = GetString(row, cNum);
                    string description = GetString(row, cDescription);

                    if (string.IsNullOrWhiteSpace(numero) && string.IsNullOrWhiteSpace(description))
                    {
                        result.SkippedEmpty++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(numero))
                    {
                        result.Errors.Add($"Ligne {r} : ignorée, pas de numéro de suggestion.");
                        continue;
                    }

                    try
                    {
                        var scopeName = GetString(row, cScope);
                        var departementName = GetString(row, cDepartement);
                        var statusRaw = GetString(row, cStatus);
                        var monthRaw = GetString(row, cMonth);
                        var kaizenRaw = GetString(row, cKaizenYN);

                        bool isNew;
                        Suggestion suggestion;


                        int? rowNo = null;
                        var noRaw = GetString(row, cNo);
                        if (int.TryParse(noRaw, out var parsedNo)) rowNo = parsedNo;

                        string dedupKey = rowNo.HasValue ? "ROW-" + rowNo.Value : "NUM-" + numero;

                        if (_processedThisRun.TryGetValue(dedupKey, out var alreadySeen))
                        {
                            suggestion = alreadySeen;
                            isNew = false;
                        }
                        else if (rowNo.HasValue)
                        {
                            suggestion = await _db.Suggestions.FirstOrDefaultAsync(s => s.SourceRowNo == rowNo.Value);
                            isNew = suggestion == null;
                            if (isNew) suggestion = new Suggestion { NumeroSuggestion = numero, SourceRowNo = rowNo };
                        }
                        else
                        {
                            // Pas de "No." exploitable sur cette ligne -> on retombe sur l'ancienne méthode, en dernier recours
                            suggestion = await _db.Suggestions.FirstOrDefaultAsync(s => s.NumeroSuggestion == numero && s.SourceRowNo == null);
                            isNew = suggestion == null;
                            if (isNew) suggestion = new Suggestion { NumeroSuggestion = numero };
                        }

                        suggestion.SourceRowNo = rowNo;

                        // Suggestion modifiée manuellement dans l'app -> le fichier Excel ne fait plus autorité pour elle
                        if (!isNew && suggestion.ModifieManuellement)
                        {
                            result.SkippedProtected++;
                            _processedThisRun[dedupKey] = suggestion;
                            continue;
                        }

                        suggestion.Plant = GetString(row, cPlant) is { Length: > 0 } p ? p : "Morocco";
                        suggestion.DateSuggestion = ParseDate(row, cSuggDate);
                        suggestion.DateMiseEnOeuvre = ParseDate(row, cImplDate);
                        suggestion.ZoneLigne = GetString(row, cZone);
                        suggestion.Situation = GetString(row, cSituation);
                        suggestion.Description = description;
                        suggestion.Responsable = GetString(row, cLeader);
                        suggestion.Economies = ParseSaving(GetString(row, cSaving));
                        suggestion.KaizenNumber = GetString(row, cKaizenNum);
                        suggestion.Kaizen = string.Equals(kaizenRaw, "yes", StringComparison.OrdinalIgnoreCase);
                        suggestion.Statut = string.Equals(statusRaw, "applied", StringComparison.OrdinalIgnoreCase)
                            ? StatutInitiative.Applique
                            : StatutInitiative.NonApplique;
                        suggestion.Mois = ParseMonth(monthRaw);

                        var scope = await FindOrCreateScopeAsync(scopeName);
                        var departement = await FindOrCreateDepartementAsync(departementName);
                        suggestion.ScopeId = scope.Id;
                        suggestion.DepartementId = departement.Id;

                        if (isNew)
                        {
                            _db.Suggestions.Add(suggestion);
                            result.Created++;
                        }
                        else if (!_processedThisRun.ContainsKey(numero))
                        {
                            result.Updated++;
                        }

                        _processedThisRun[dedupKey] = suggestion;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Ligne {r} ({numero}) : {ex.Message}");
                    }
                }

                await _db.SaveChangesAsync();
                return result;
            }
        }

        private async Task LoadCachesAsync()
        {
            _departementCache.Clear();
            _scopeCache.Clear();
            foreach (var d in await _db.Departements.ToListAsync())
                _departementCache[d.Nom] = d;
            foreach (var s in await _db.Scopes.ToListAsync())
                _scopeCache[s.Nom] = s;
        }

        private async Task<Departement> FindOrCreateDepartementAsync(string rawName)
        {
            var normalized = NormalizeLabel(rawName);
            if (string.IsNullOrEmpty(normalized)) normalized = "Non spécifié";

            if (_departementCache.TryGetValue(normalized, out var existing))
                return existing;

            var dep = new Departement { Nom = normalized };
            _db.Departements.Add(dep);
            await _db.SaveChangesAsync();
            _departementCache[normalized] = dep;
            return dep;
        }

        private async Task<Scope> FindOrCreateScopeAsync(string rawName)
        {
            var normalized = NormalizeLabel(rawName);
            if (string.IsNullOrEmpty(normalized)) normalized = "Non spécifié";
            if (ScopeRedirects.TryGetValue(normalized, out var redirect)) normalized = redirect;

            if (_scopeCache.TryGetValue(normalized, out var existing))
                return existing;

            var sc = new Scope { Nom = normalized };
            _db.Scopes.Add(sc);
            await _db.SaveChangesAsync();
            _scopeCache[normalized] = sc;
            return sc;
        }

        private static string NormalizeLabel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var trimmed = string.Join(" ", raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
        }

        private static string NormalizeHeader(string raw) =>
            string.Join(" ", (raw ?? "").Replace("\n", " ").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

        private static string GetString(IXLRow row, int col)
        {
            if (col < 1) return "";
            var cell = row.Cell(col);
            return cell.IsEmpty() ? "" : cell.GetString().Trim();
        }

        private static DateTime? ParseDate(IXLRow row, int col)
        {
            if (col < 1) return null;
            var cell = row.Cell(col);
            if (cell.IsEmpty()) return null;

            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();

            var raw = cell.GetString().Trim();
            if (string.IsNullOrEmpty(raw)) return null;

            string[] formats = { "M/d/yyyy", "MM/dd/yyyy", "d/M/yyyy", "dd/MM/yyyy", "dd.MM.yyyy", "yyyy-MM-dd" };
            if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
                return d2;

            return null;
        }

        private static decimal ParseSaving(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;

            var cleaned = raw.Replace("€", "").Replace(" ", "").Trim();
            int dotCount = cleaned.Count(c => c == '.');

            if (dotCount > 1)
            {
                int lastDot = cleaned.LastIndexOf('.');
                cleaned = cleaned.Substring(0, lastDot).Replace(".", "") + cleaned.Substring(lastDot);
            }
            else if (dotCount == 0 && cleaned.Contains(','))
            {
                cleaned = cleaned.Replace(",", ".");
            }

            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0;
        }

        private static Mois? ParseMonth(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var key = raw.Trim();
            return MonthMap.TryGetValue(key, out var m) ? m : null;
        }
    }
}