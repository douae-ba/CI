namespace CI.Models
{
    public class StatutObjective
    {
        public int Id { get; set; }
        public StatutInitiative Statut { get; set; }
        public int Year { get; set; }
        public Mois Mois { get; set; }
        public int TargetCount { get; set; }
    }
}