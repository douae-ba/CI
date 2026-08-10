namespace CI.Services
{
    public class ExportResult
    {
        public int Exported { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}