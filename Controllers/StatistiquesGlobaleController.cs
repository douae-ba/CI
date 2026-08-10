using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CI.Data;
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
                    if (cached != null)
                    {
                        allRows.AddRange(cached);
                    }
                    else
                    {
                        var freshRows = _statsService.GetRows(path);
                        TargetStatsCache.Set(path, freshRows);
                        allRows.AddRange(freshRows);
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"{path} : {ex.Message}");
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
    }
}