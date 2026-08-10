using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CI.Data;
using CI.Models;

namespace CI.Controllers
{
    [Authorize]
    public class InitiativesController : Controller
    {
        private readonly AppDbContext _db;

        public InitiativesController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Departements = await _db.Departements.OrderBy(d => d.Nom).ToListAsync();
            ViewBag.Scopes = await _db.Scopes.OrderBy(s => s.Nom == "Other" ? 1 : 0).ThenBy(s => s.Nom).ToListAsync();
            ViewBag.Statuts = Enum.GetValues<StatutInitiative>();
            ViewBag.MoisList = Enum.GetValues<Mois>();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _db.Suggestions
                .Include(s => s.Scope)
                .Include(s => s.Departement)
                .OrderByDescending(s => s.DateSuggestion)
                .Select(s => new
                {
                    s.Id,
                    s.NumeroSuggestion,
                    s.Plant,
                    dateSuggestion = s.DateSuggestion,
                    s.ScopeId,
                    scopeName = s.Scope.Nom,
                    s.ZoneLigne,
                    s.Situation,
                    s.Description,
                    s.DepartementId,
                    departementName = s.Departement.Nom,
                    s.Responsable,
                    statut = s.Statut.ToString(),
                    dateMiseEnOeuvre = s.DateMiseEnOeuvre,
                    s.Economies,
                    s.KaizenNumber,
                    mois = s.Mois.HasValue ? s.Mois.Value.ToString() : null,
                    s.Kaizen,
                    s.ModifieManuellement,
                    s.ExporteVersGlobal
                })
                .ToListAsync();

            return Json(data);
        }

        public class SuggestionInput
        {
            public int? Id { get; set; }
            public string Plant { get; set; } = "Morocco";
            public DateTime? DateSuggestion { get; set; }
            public DateTime? DateMiseEnOeuvre { get; set; }
            public int ScopeId { get; set; }
            public int DepartementId { get; set; }
            public string Responsable { get; set; } = "";
            public string Mois { get; set; }
            public string Statut { get; set; }
            public string ZoneLigne { get; set; } = "";
            public decimal Economies { get; set; }
            public bool Kaizen { get; set; }
            public string KaizenNumber { get; set; } = "";
            public string Situation { get; set; } = "";
            public string Description { get; set; } = "";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromBody] SuggestionInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Description))
                return Json(new { success = false, message = "Description requise." });

            Suggestion suggestion;
            bool isNewSuggestion = !input.Id.HasValue;

            if (!isNewSuggestion)
            {
                suggestion = await _db.Suggestions.FindAsync(input.Id.Value);
                if (suggestion == null)
                    return Json(new { success = false, message = "Initiative introuvable." });
            }
            else
            {
                // Numéro temporaire — sera remplacé une fois l'Id connu, après le premier SaveChanges
                suggestion = new Suggestion { NumeroSuggestion = "TEMP" };
                _db.Suggestions.Add(suggestion);
            }

            suggestion.Plant = input.Plant;
            suggestion.DateSuggestion = input.DateSuggestion;
            suggestion.DateMiseEnOeuvre = input.DateMiseEnOeuvre;
            suggestion.ScopeId = input.ScopeId;
            suggestion.DepartementId = input.DepartementId;
            suggestion.Responsable = input.Responsable;
            suggestion.ZoneLigne = input.ZoneLigne;
            suggestion.Economies = input.Economies;
            suggestion.Kaizen = input.Kaizen;
            suggestion.KaizenNumber = input.KaizenNumber;
            suggestion.Situation = input.Situation;
            suggestion.Description = input.Description;

            if (Enum.TryParse<StatutInitiative>(input.Statut, out var statutParsed))
                suggestion.Statut = statutParsed;

            if (!string.IsNullOrEmpty(input.Mois) && Enum.TryParse<Models.Mois>(input.Mois, out var moisParsed))
                suggestion.Mois = moisParsed;
            else
                suggestion.Mois = null;

            suggestion.ModifieManuellement = true;
            suggestion.DateModificationManuelle = DateTime.Now;

            await _db.SaveChangesAsync();

            // Maintenant que l'Id est connu, on génère le vrai numéro
            if (isNewSuggestion)
            {
                suggestion.NumeroSuggestion = $"{DateTime.Now.Year}-S{suggestion.Id}";
                await _db.SaveChangesAsync();
            }

            return Json(new { success = true, id = suggestion.Id, numeroSuggestion = suggestion.NumeroSuggestion });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            var suggestion = await _db.Suggestions.FindAsync(id);
            if (suggestion == null)
                return Json(new { success = false, message = "Introuvable." });

            _db.Suggestions.Remove(suggestion);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetFilters()
        {
            var departements = await _db.Departements.OrderBy(d => d.Nom)
                .Select(d => new { d.Id, d.Nom }).ToListAsync();
            var scopes = await _db.Scopes.OrderBy(s => s.Nom == "Other" ? 1 : 0).ThenBy(s => s.Nom)
                .Select(s => new { s.Id, s.Nom }).ToListAsync();

            return Json(new { departements, scopes });
        }
    }
}