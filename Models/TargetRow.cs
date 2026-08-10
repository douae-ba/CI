namespace CI.Services
{
    public class TargetRow
    {
        public string Plant { get; set; } = "";
        public string SuggestionNumber { get; set; } = "";
        public DateTime? RegisterDate { get; set; }
        public string RegisterMonth { get; set; } = "";
        public string ImprovementType { get; set; } = "";
        public string Department { get; set; } = "";
        public string AttackedLosses { get; set; } = "";
        public string DelayStatus { get; set; } = "";
        public string CurrentStatus { get; set; } = "";
        public string ClosureStatus { get; set; } = "";
        public decimal TotalSaving { get; set; }
    }
}