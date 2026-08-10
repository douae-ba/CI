namespace CI.Services
{
    public class ImportResult
    {
        public int TotalRowsInFile { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int SkippedEmpty { get; set; }
        public int SkippedProtected { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}