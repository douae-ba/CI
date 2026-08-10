namespace CI.Models
{
    public class DepartementScopeObjective
    {
        public int Id { get; set; }
        public int DepartementId { get; set; }
        public Departement Departement { get; set; } = null!;
        public int ScopeId { get; set; }
        public Scope Scope { get; set; } = null!;
        public int Year { get; set; }
        public Mois Mois { get; set; }
        public int TargetCount { get; set; }
    }
}