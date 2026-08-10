using CI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CI.Data;
using CI.Services;

namespace CI.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly SuggestionImportService _importService;
        private readonly TargetExportService _exportService;

        public HomeController(AppDbContext db, SuggestionImportService importService, TargetExportService exportService)
        {
            _db = db;
            _importService = importService;
            _exportService = exportService;
        }

        public async Task<IActionResult> Index()
        {
            int currentYear = DateTime.Now.Year;
            var currentYearExport = await _db.Exports.FirstOrDefaultAsync(e => e.Year == currentYear);

            ViewBag.LastSourcePath = currentYearExport?.SourceFileUrl ?? "";
            ViewBag.LastTargetPath = currentYearExport?.CibleFileUrl ?? "";
            ViewBag.LastSyncDate = currentYearExport?.DateExport;
            ViewBag.CurrentYear = currentYear;

            return View();
        }

        public IActionResult Privacy() { return View(); }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncFromSource([FromForm] string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                return Json(new { success = false, message = "Chemin du fichier vide." });

            try
            {
                var result = await _importService.ImportFromFileAsync(sourcePath);

                int currentYear = DateTime.Now.Year;
                var exportRecord = await _db.Exports.FirstOrDefaultAsync(e => e.Year == currentYear);
                if (exportRecord == null)
                {
                    exportRecord = new Exports { Year = currentYear };
                    _db.Exports.Add(exportRecord);
                }
                exportRecord.SourceFileUrl = sourcePath;
                exportRecord.DateExport = DateTime.Now;
                exportRecord.NbSugg = await _db.Suggestions.CountAsync();
                await _db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    totalRows = result.TotalRowsInFile,
                    created = result.Created,
                    updated = result.Updated,
                    skipped = result.SkippedEmpty,
                    skippedProtected = result.SkippedProtected,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Erreur serveur : " + detail });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SuggestionCount()
        {
            var count = await _db.Suggestions.CountAsync();
            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportToTarget([FromForm] string targetPath, [FromForm] string idsJson)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
                return Json(new { success = false, message = "Chemin du fichier cible vide." });

            List<int> ids;
            try
            {
                ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(idsJson) ?? new List<int>();
            }
            catch
            {
                return Json(new { success = false, message = "Sélection invalide." });
            }

            if (ids.Count == 0)
                return Json(new { success = false, message = "Aucune suggestion sélectionnée. Va d'abord dans Initiatives, coche les lignes voulues." });

            try
            {
                var result = await _exportService.ExportSelectedAsync(targetPath, ids);

                int currentYear = DateTime.Now.Year;
                var exportRecord = await _db.Exports.FirstOrDefaultAsync(e => e.Year == currentYear);
                if (exportRecord == null)
                {
                    exportRecord = new Exports { Year = currentYear };
                    _db.Exports.Add(exportRecord);
                }
                exportRecord.CibleFileUrl = targetPath;
                exportRecord.DateExport = DateTime.Now;
                await _db.SaveChangesAsync();
                CI.Services.TargetStatsCache.Invalidate(targetPath);

                return Json(new
                {
                    success = result.Errors.Count == 0 || result.Exported > 0,
                    exported = result.Exported,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Erreur serveur : " + detail });
            }
        }
    }
}