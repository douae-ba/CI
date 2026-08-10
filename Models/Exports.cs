namespace CI.Models
{
    public class Exports
    {
        public int Id { get; set; }

        public string? SourceFileUrl { get; set; }

        public string? CibleFileUrl { get; set; }

        
        public DateTime DateExport { get; set; }

        public int NbSugg { get; set; }

        public int Year { get; set; }

    }
}