using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CI.Data;
using CI.Models;
using CI.Services;

namespace CI.Controllers
{
    [Authorize]
    public class StatistiquesGlobaleController : Controller
    {
        private readonly AppDbContext _db;
        private readonly TargetStatsService _statsService;

        public StatistiquesGlobaleController(AppDbContext db, TargetStatsService statsService)
        {
            _db = db;
            _statsService = statsService;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            var allTargetPaths = await _db.Exports
                .Where(e => !string.IsNullOrEmpty(e.CibleFileUrl))
                .Select(e => e.CibleFileUrl!)
                .Distinct()
                .ToListAsync();

            if (allTargetPaths.Count == 0)
                return Json(new { success = false, message = "Aucun fichier cible configuré pour l'instant. Va sur Export / CI Plan et renseigne le chemin.", rows = new object[0], warnings = new string[0] });

            var allRows = new List<TargetRow>();
            var warnings = new List<string>();

            foreach (var path in allTargetPaths)
            {
                try
                {
                    var cached = TargetStatsCache.TryGet(path);
                    List<TargetRow> rows;

                    if (cached != null)
                    {
                        rows = cached;
                    }
                    else
                    {
                        rows = _statsService.GetRows(path);
                        TargetStatsCache.Set(path, rows);

                        // Sauvegarde/rafraîchit la copie de secours en base à chaque lecture réussie du fichier
                        await BackupToDatabase(path, rows);
                    }

                    allRows.AddRange(rows);
                }
                catch (Exception ex)
                {
                    // Fichier introuvable ou illisible -> on bascule sur la dernière copie connue en base
                    var backupRows = await _db.TargetRecords
                        .Where(t => t.SourceFilePath == path)
                        .ToListAsync();

                    if (backupRows.Any())
                    {
                        warnings.Add($"{path} : fichier inaccessible ({ex.Message}) — affichage de la dernière copie enregistrée.");
                        allRows.AddRange(backupRows.Select(t => new TargetRow
                        {
                            Plant = t.Plant,
                            SuggestionNumber = t.SuggestionNumber,
                            RegisterDate = t.RegisterDate,
                            RegisterMonth = t.RegisterMonth,
                            ImprovementType = t.ImprovementType,
                            Department = t.Department,
                            AttackedLosses = t.AttackedLosses,
                            DelayStatus = t.DelayStatus,
                            CurrentStatus = t.CurrentStatus,
                            ClosureStatus = t.ClosureStatus,
                            TotalSaving = t.TotalSaving
                        }));
                    }
                    else
                    {
                        warnings.Add($"{path} : {ex.Message} (aucune copie de secours disponible en base)");
                    }
                }
            }

            var mapped = allRows.Select(r => new
            {
                plant = r.Plant,
                suggestionNumber = r.SuggestionNumber,
                registerDate = r.RegisterDate,
                registerMonth = r.RegisterMonth,
                improvementType = r.ImprovementType,
                department = r.Department,
                attackedLosses = r.AttackedLosses,
                delayStatus = r.DelayStatus,
                currentStatus = r.CurrentStatus,
                closureStatus = r.ClosureStatus,
                totalSaving = r.TotalSaving
            });

            return Json(new { success = true, rows = mapped, warnings });
        }

        private async Task BackupToDatabase(string path, List<TargetRow> rows)
        {
            var existing = _db.TargetRecords.Where(t => t.SourceFilePath == path);
            _db.TargetRecords.RemoveRange(existing);

            foreach (var r in rows)
            {
                _db.TargetRecords.Add(new TargetRecord
                {
                    SourceFilePath = path,
                    Plant = r.Plant,
                    SuggestionNumber = r.SuggestionNumber,
                    RegisterDate = r.RegisterDate,
                    RegisterMonth = r.RegisterMonth,
                    ImprovementType = r.ImprovementType,
                    Department = r.Department,
                    AttackedLosses = r.AttackedLosses,
                    DelayStatus = r.DelayStatus,
                    CurrentStatus = r.CurrentStatus,
                    ClosureStatus = r.ClosureStatus,
                    TotalSaving = r.TotalSaving
                });
            }

            await _db.SaveChangesAsync();
        }
    }
}