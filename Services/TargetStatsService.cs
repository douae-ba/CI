using System.Globalization;
using ClosedXML.Excel;

namespace CI.Services
{
    public class TargetStatsService
    {
        public List<TargetRow> GetRows(string filePath, string sheetName = "Morocco")
        {
            var rows = new List<TargetRow>();

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Fichier introuvable : {filePath}");

            using var workbook = new XLWorkbook(filePath);
            if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var ws))
                throw new Exception($"Feuille '{sheetName}' introuvable dans le fichier cible.");

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
                throw new Exception("Impossible de localiser la ligne d'en-têtes.");

            var headerRow = ws.Row(headerRowNum);
            var colIndex = new Dictionary<string, int>();
            foreach (var cell in headerRow.CellsUsed())
            {
                var name = NormalizeHeader(cell.GetString());
                if (!string.IsNullOrEmpty(name) && !colIndex.ContainsKey(name))
                    colIndex[name] = cell.Address.ColumnNumber;
            }

            int Col(string label)
            {
                var normalized = NormalizeHeader(label);
                if (colIndex.TryGetValue(normalized, out var exact)) return exact;
                var partial = colIndex.FirstOrDefault(kvp => kvp.Key.Contains(normalized) || normalized.Contains(kvp.Key));
                return partial.Key != null ? partial.Value : -1;
            }

            int cPlant = Col("Plant");
            int cSuggNum = Col("Suggestion Number");
            int cRegisterDate = Col("Register Date");
            int cRegisterMonth = Col("Register Month");
            int cImprovementType = Col("Improvement Type");
            int cDepartment = Col("Project Leader Department");
            int cAttackedLosses = Col("Attacked Losses");
            int cDelayStatus = Col("Delay Status");
            int cCurrentStatus = Col("Current Status");
            int cClosureStatus = Col("Closure Status");
            int cTotalSaving = Col("Total Saving");

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRowNum;

            for (int r = headerRowNum + 1; r <= lastRow; r++)
            {
                try
                {
                    var row = ws.Row(r);
                    var suggNum = GetString(row, cSuggNum);
                    if (string.IsNullOrWhiteSpace(suggNum))
                        continue;

                    rows.Add(new TargetRow
                    {
                        Plant = GetString(row, cPlant),
                        SuggestionNumber = suggNum,
                        RegisterDate = ParseDate(row, cRegisterDate),
                        RegisterMonth = GetString(row, cRegisterMonth),
                        ImprovementType = GetString(row, cImprovementType),
                        Department = GetString(row, cDepartment),
                        AttackedLosses = GetString(row, cAttackedLosses),
                        DelayStatus = GetString(row, cDelayStatus),
                        CurrentStatus = GetString(row, cCurrentStatus),
                        ClosureStatus = GetString(row, cClosureStatus),
                        TotalSaving = ParseAmount(row, cTotalSaving)
                    });
                }
                catch
                {
                    continue;
                }
            }

            return rows;
        }

        private static string NormalizeHeader(string raw) =>
            string.Join(" ", (raw ?? "").Replace("\n", " ").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

        private static string GetString(IXLRow row, int col)
        {
            if (col < 1) return "";
            try
            {
                var cell = row.Cell(col);
                return cell.IsEmpty() ? "" : cell.GetString().Trim();
            }
            catch
            {
                return "";
            }
        }

        private static DateTime? ParseDate(IXLRow row, int col)
        {
            if (col < 1) return null;
            try
            {
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
            catch
            {
                return null;
            }
        }

        private static decimal ParseAmount(IXLRow row, int col)
        {
            if (col < 1) return 0;
            try
            {
                var cell = row.Cell(col);
                if (cell.IsEmpty()) return 0;

                try
                {
                    if (cell.DataType == XLDataType.Number)
                        return (decimal)cell.GetDouble();
                }
                catch { /* on retente en texte ci-dessous */ }

                var raw = cell.GetString().Replace("€", "").Replace(" ", "").Trim();
                int dotCount = raw.Count(c => c == '.');
                if (dotCount > 1)
                {
                    int lastDot = raw.LastIndexOf('.');
                    raw = raw.Substring(0, lastDot).Replace(".", "") + raw.Substring(lastDot);
                }
                else if (dotCount == 0 && raw.Contains(','))
                {
                    raw = raw.Replace(",", ".");
                }
                return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}