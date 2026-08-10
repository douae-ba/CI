using System.ComponentModel.DataAnnotations;

namespace CI.Models
{
    public enum Mois
    {
        [Display(Name = "Janvier")] Janvier = 1,
        [Display(Name = "Février")] Fevrier,
        [Display(Name = "Mars")] Mars,
        [Display(Name = "Avril")] Avril,
        [Display(Name = "Mai")] Mai,
        [Display(Name = "Juin")] Juin,
        [Display(Name = "Juillet")] Juillet,
        [Display(Name = "Août")] Aout,
        [Display(Name = "Septembre")] Septembre,
        [Display(Name = "Octobre")] Octobre,
        [Display(Name = "Novembre")] Novembre,
        [Display(Name = "Décembre")] Decembre
    }

    public enum StatutInitiative
    {
        [Display(Name = "Appliqué")] Applique,
        [Display(Name = "Non appliqué")] NonApplique,
    }
    public enum Evaluation
    {
        [Display(Name = "Applicable")] Applicable,
        [Display(Name = "Non Applicable")] NonApplicable,
    }
}