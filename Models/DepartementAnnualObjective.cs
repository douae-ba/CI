namespace CI.Models
{
    public class DepartementAnnualObjective
    {
        public int Id { get; set; }
        public int DepartementId { get; set; }
        public Departement Departement { get; set; } = null!;
        public int Year { get; set; }
        public int TargetPercent { get; set; }
    }
}