using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CI.Data;
using CI.Models;

namespace CI.Controllers
{
    [Authorize]
    public class StatistiquesSuggestionsController : Controller
    {
        private readonly AppDbContext _db;

        public StatistiquesSuggestionsController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult GetFilters()
        {
            var departements = _db.Departements.OrderBy(d => d.Nom)
                .Select(d => new { d.Id, d.Nom, d.Headcount }).ToList();
            var scopes = _db.Scopes.OrderBy(s => s.Nom == "Other" ? 1 : 0).ThenBy(s => s.Nom)
                .Select(s => new { s.Id, s.Nom, s.TargetPercent }).ToList();
            var statuts = Enum.GetValues<StatutInitiative>()
                .Select(s => new { value = s.ToString(), label = s.GetDisplayName() });
            var moisList = Enum.GetValues<Mois>()
                .Select(m => new { value = m.ToString(), label = m.GetDisplayName() });

            return Json(new { departements, scopes, statuts, moisList });
        }

        [HttpGet]
        public async Task<IActionResult> GetObjectivesData()
        {
            var departements = await _db.Departements.OrderBy(d => d.Nom).ToListAsync();
            var annualTargets = await _db.DepartementAnnualObjectives.ToListAsync();
            var monthlyTargets = await _db.DepartementMonthlyObjectives.ToListAsync();
            var scopes = await _db.Scopes.OrderBy(s => s.Nom == "Other" ? 1 : 0).ThenBy(s => s.Nom).ToListAsync();
            var statutObjectives = await _db.StatutObjectives.ToListAsync();
            var scopeDeptTargets = await _db.DepartementScopeObjectives.ToListAsync();

            var deptResult = departements.Select(d => new
            {
                d.Id,
                d.Nom,
                d.Headcount,
                annualTargets = annualTargets.Where(a => a.DepartementId == d.Id)
                    .ToDictionary(a => a.Year.ToString(), a => a.TargetPercent),
                monthlyTargets = monthlyTargets.Where(m => m.DepartementId == d.Id)
                    .ToDictionary(m => m.Year + "-" + m.Mois, m => m.TargetPercent)
            });

            var scopeObjectives = scopes.ToDictionary(s => s.Nom, s => s.TargetPercent);

            var statutObjectivesList = statutObjectives.Select(o => new
            {
                statut = o.Statut.ToString(),
                year = o.Year,
                mois = o.Mois.ToString(),
                targetCount = o.TargetCount
            });

            var scopeDeptResult = scopeDeptTargets.Select(o => new
            {
                departementId = o.DepartementId,
                scopeId = o.ScopeId,
                year = o.Year,
                mois = o.Mois.ToString(),
                targetCount = o.TargetCount
            });

            return Json(new { departments = deptResult, scopeObjectives, statutObjectives = statutObjectivesList, scopeDeptTargets = scopeDeptResult });
        }

        public class DeptObjectiveRow
        {
            public int DepartementId { get; set; }
            public int Headcount { get; set; }
            public int AnnualTarget { get; set; }
            public Dictionary<string, int> MonthlyTargets { get; set; } = new();
        }
        public class SaveDeptObjectivesInput
        {
            public int Year { get; set; }
            public List<DeptObjectiveRow> Departments { get; set; } = new();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDepartmentObjectives([FromBody] SaveDeptObjectivesInput input)
        {
            foreach (var row in input.Departments)
            {
                var dept = await _db.Departements.FindAsync(row.DepartementId);
                if (dept == null) continue;
                dept.Headcount = row.Headcount;

                var annual = await _db.DepartementAnnualObjectives
                    .FirstOrDefaultAsync(a => a.DepartementId == row.DepartementId && a.Year == input.Year);
                if (annual == null)
                {
                    annual = new DepartementAnnualObjective { DepartementId = row.DepartementId, Year = input.Year };
                    _db.DepartementAnnualObjectives.Add(annual);
                }
                annual.TargetPercent = row.AnnualTarget;

                foreach (var kvp in row.MonthlyTargets)
                {
                    if (!Enum.TryParse<Mois>(kvp.Key, out var moisEnum)) continue;
                    var monthly = await _db.DepartementMonthlyObjectives.FirstOrDefaultAsync(
                        m => m.DepartementId == row.DepartementId && m.Year == input.Year && m.Mois == moisEnum);
                    if (monthly == null)
                    {
                        monthly = new DepartementMonthlyObjective { DepartementId = row.DepartementId, Year = input.Year, Mois = moisEnum };
                        _db.DepartementMonthlyObjectives.Add(monthly);
                    }
                    monthly.TargetPercent = kvp.Value;
                }
            }
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        public class ScopeTargetInput { public int ScopeId { get; set; } public int TargetPercent { get; set; } }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScopeObjectives([FromBody] List<ScopeTargetInput> input)
        {
            foreach (var item in input)
            {
                var scope = await _db.Scopes.FindAsync(item.ScopeId);
                if (scope != null) scope.TargetPercent = item.TargetPercent;
            }
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        public class StatutTargetInput { public string Statut { get; set; } = ""; public int TargetCount { get; set; } }
        public class SaveStatutInput
        {
            public int Year { get; set; }
            public string Mois { get; set; } = "";
            public List<StatutTargetInput> Targets { get; set; } = new();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStatutObjectives([FromBody] SaveStatutInput input)
        {
            if (!Enum.TryParse<Mois>(input.Mois, out var moisEnum))
                return Json(new { success = false, message = "Mois invalide." });

            foreach (var item in input.Targets)
            {
                if (!Enum.TryParse<StatutInitiative>(item.Statut, out var statutEnum)) continue;
                var obj = await _db.StatutObjectives.FirstOrDefaultAsync(s =>
                    s.Statut == statutEnum && s.Year == input.Year && s.Mois == moisEnum);
                if (obj == null)
                {
                    obj = new StatutObjective { Statut = statutEnum, Year = input.Year, Mois = moisEnum };
                    _db.StatutObjectives.Add(obj);
                }
                obj.TargetCount = item.TargetCount;
            }
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        public class HeadcountInput { public int DepartementId { get; set; } public int Headcount { get; set; } }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveHeadcounts([FromBody] List<HeadcountInput> input)
        {
            foreach (var item in input)
            {
                var dept = await _db.Departements.FindAsync(item.DepartementId);
                if (dept != null) dept.Headcount = item.Headcount;
            }
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        public class ScopeDeptTargetInput
        {
            public int DepartementId { get; set; }
            public int ScopeId { get; set; }
            public int TargetCount { get; set; }
        }
        public class SaveScopeDeptInput
        {
            public int Year { get; set; }
            public string Mois { get; set; } = "";
            public List<ScopeDeptTargetInput> Targets { get; set; } = new();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDepartementScopeObjectives([FromBody] SaveScopeDeptInput input)
        {
            if (!Enum.TryParse<Mois>(input.Mois, out var moisEnum))
                return Json(new { success = false, message = "Mois invalide." });

            foreach (var item in input.Targets)
            {
                var existing = await _db.DepartementScopeObjectives.FirstOrDefaultAsync(o =>
                    o.DepartementId == item.DepartementId && o.ScopeId == item.ScopeId &&
                    o.Year == input.Year && o.Mois == moisEnum);

                if (existing == null)
                {
                    existing = new DepartementScopeObjective
                    {
                        DepartementId = item.DepartementId,
                        ScopeId = item.ScopeId,
                        Year = input.Year,
                        Mois = moisEnum
                    };
                    _db.DepartementScopeObjectives.Add(existing);
                }
                existing.TargetCount = item.TargetCount;
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}