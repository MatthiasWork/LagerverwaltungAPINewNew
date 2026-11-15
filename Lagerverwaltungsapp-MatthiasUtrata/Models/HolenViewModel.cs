using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Das ViewModel zum Holen von Geräten aus einem Lager in den eigenen Raum.
    public class HolenViewModel
    {
        // Das Lager, aus dem geholt wird.
        [Required(ErrorMessage = "Bitte ein Lager auswählen.")]
        [Display(Name = "Lager")]
        public string? VonRaumID { get; set; }

        // Der eigene Raum, in den geholt wird.
        [Required(ErrorMessage = "Bitte einen Raum auswählen.")]
        [Display(Name = "Holen nach")]
        public string? NachRaumID { get; set; }

        [Required(ErrorMessage = "Bitte eine Bewegungsart auswählen.")]
        [Display(Name = "Bewegungsart")]
        public int? BewegungsartID { get; set; }

        // Die eingegebenen Mengen je Gegenstand (Schlüssel ist die GegenstandID); leer oder 0 heißt "nicht holen".
        public Dictionary<int, int?> Mengen { get; set; } = new Dictionary<int, int?>();

        // Ab hier nur für die Anzeige; diese Werte kommen nicht aus dem Formular und werden nicht validiert.

        // Der eigene Raum, in den geholt wird.
        [ValidateNever]
        public Raum NachRaum { get; set; } = null!;

        // Alle Räume der Person.
        [ValidateNever]
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        // Alle Lager, aus denen geholt werden kann.
        [ValidateNever]
        public List<Raum> Lager { get; set; } = new List<Raum>();

        // Das gewählte Lager oder null, wenn es keines gibt.
        [ValidateNever]
        public Raum? VonRaum { get; set; }

        // Der Bestand des gewählten Lagers.
        [ValidateNever]
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        [ValidateNever]
        public SelectList Bewegungsarten { get; set; } = null!;
    }
}
