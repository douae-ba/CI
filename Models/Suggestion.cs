using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CI.Models
{
    public class Suggestion
    {
        public int Id { get; set; }

        public string Plant { get; set; } = "Morocco";

        public string NumeroSuggestion { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateSuggestion { get; set; }

        public int ScopeId { get; set; }
        public Scope Scope { get; set; }

        public string ZoneLigne { get; set; }

        public string Situation { get; set; }

        [Required]
        public string Description { get; set; }

        public int DepartementId { get; set; }
        public Departement Departement { get; set; }

        public string Responsable { get; set; }

        public StatutInitiative Statut { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateMiseEnOeuvre { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Economies { get; set; }

        public string KaizenNumber { get; set; }

        public Mois? Mois { get; set; }

        public bool Kaizen { get; set; }

        public bool ModifieManuellement { get; set; }
        public DateTime? DateModificationManuelle { get; set; }

        public bool ExporteVersGlobal { get; set; }
        public DateTime? DateExportGlobal { get; set; }

        public int? SourceRowNo { get; set; }
    }
}