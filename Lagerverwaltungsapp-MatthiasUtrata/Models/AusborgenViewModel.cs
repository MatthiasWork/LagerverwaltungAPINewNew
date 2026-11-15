using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class AusborgenViewModel
    {
        // Der Raum, aus dem verliehen wird.
        public string? VonRaumID { get; set; }

        // Der Raum, an den verliehen wird.
        [Required(ErrorMessage = "Bitte einen Raum auswählen.")]
        [Display(Name = "Verleihen an Raum")]
        public string? NachRaumID { get; set; }

        [Required(ErrorMessage = "Bitte eine Bewegungsart auswählen.")]
        [Display(Name = "Bewegungsart")]
        public int? BewegungsartID { get; set; }

        // Die eingegebenen Mengen je Gegenstand (Schlüssel ist die GegenstandID); leer oder 0 heißt "nicht verleihen".
        public Dictionary<int, int?> Mengen { get; set; } = new Dictionary<int, int?>();

        // Ab hier nur für die Anzeige; diese Werte kommen nicht aus dem Formular und werden nicht validiert.

        // Der Raum, aus dem verliehen wird.
        [ValidateNever]
        public Raum VonRaum { get; set; } = null!;

        // Die Räume, aus denen die Person verleihen darf.
        [ValidateNever]
        public List<Raum> VonRaeume { get; set; } = new List<Raum>();

        // Die IDs der Räume, für die die angemeldete Person verantwortlich ist.
        [ValidateNever]
        public List<string> EigeneRaumIDs { get; set; } = new List<string>();

        // Der Bestand des Von-Raums.
        [ValidateNever]
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        // Die möglichen Zielräume.
        [ValidateNever]
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        [ValidateNever]
        public SelectList Bewegungsarten { get; set; } = null!;
    }
}
