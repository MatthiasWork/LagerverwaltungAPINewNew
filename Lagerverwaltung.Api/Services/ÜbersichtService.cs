using Lagerverwaltung.Api.DTOs;

namespace Lagerverwaltung.Api.Services;

// Stellt die Übersicht und das Profil aus den Ergebnissen der anderen Services zusammen
// Dadurch gelten dieselben Berechtigungen wie in den Prozeduren: ein Admin sieht alle Räume, sonst jeder nur die eigenen
public class ÜbersichtService
{
    private const int AnzahlLetzteBewegungen = 5;
    private const int AnzahlLetzteBewegungenImProfil = 8;

    private readonly ArtikelService _artikelService;
    private readonly StandortService _standortService;
    private readonly BuchungService _buchungService;
    private readonly BenutzerService _benutzerService;

    public ÜbersichtService(ArtikelService artikelService, StandortService standortService, BuchungService buchungService,
        BenutzerService benutzerService)
    {
        _artikelService = artikelService;
        _standortService = standortService;
        _buchungService = buchungService;
        _benutzerService = benutzerService;
    }

    /// <summary>
    /// Methode, die die Zahlen für die Übersicht liest: Bestand, Aufteilung nach Kategorie und Raum, offene Freigaben und die letzten Lagerbewegungen
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="istAdmin">True, wenn die angemeldete Person Adminrechte hat</param>
    /// <returns>Gibt die Übersicht zurück</returns>
    public async Task<ÜbersichtDTO> ÜbersichtAsync(int angemeldetID, bool istAdmin)
    {
        var artikel = await _artikelService.SuchenAsync(angemeldetID, null, null, null, null, null, null, null);

        List<RaumByIDLesenDTO> räume;
        if (istAdmin)
        {
            räume = await _standortService.ListeAsync(null, null, null);
        }
        else
        {
            räume = await _standortService.MeineRäumeAsync(angemeldetID);
        }

        var offeneÜbergaben = await _buchungService.OffeneÜbergabenAsync(angemeldetID);
        int offeneFreigaben = offeneÜbergaben.Count(b => b.DarfBestaetigen);
        // Rückgaben nimmt nur ein Admin an
        if (istAdmin)
        {
            var offeneRückgaben = await _buchungService.OffeneRückgabenAsync(angemeldetID);
            offeneFreigaben += offeneRückgaben.Count;
        }

        var bewegungen = await _buchungService.SichtbareBewegungenAsync(angemeldetID, istAdmin, null, null);

        var übersicht = new ÜbersichtDTO
        {
            Verfuegbar = artikel.Sum(a => a.Bestand),
            Unterwegs = artikel.Sum(a => a.Unterwegs),
            OffeneFreigaben = offeneFreigaben,
            BestandJeKategorie = artikel
                .GroupBy(a => new { a.KategorieID, a.Kategorie })
                .Select(g => new KategorieBestandDTO { KategorieID = g.Key.KategorieID, Kategorie = g.Key.Kategorie, Stueck = g.Sum(a => a.Bestand) })
                .Where(k => k.Stueck > 0)
                .OrderByDescending(k => k.Stueck)
                .ThenBy(k => k.Kategorie)
                .ToList(),
            BestandJeRaum = räume
                .Where(r => r.Stueck > 0)
                .OrderByDescending(r => r.Stueck)
                .ThenBy(r => r.ID)
                .Select(r => new RaumBestandDTO { RaumID = r.ID, Raumart = r.Raumart, Stueck = r.Stueck })
                .ToList(),
            // Bei einer abgeschlossenen Bewegung zählt der Zeitpunkt der Bestätigung bzw. Stornierung
            LetzteBewegungen = bewegungen
                .OrderByDescending(b => b.BestaetigtAm ?? b.ErstelltAm)
                .ThenByDescending(b => b.ID)
                .Take(AnzahlLetzteBewegungen)
                .ToList()
        };
        übersicht.Gesamt = übersicht.Verfuegbar + übersicht.Unterwegs;
        return übersicht;
    }

    /// <summary>
    /// Methode, die das Profil einer Person liest: ihre Daten, ihre Räume, ihre offenen und ihre letzten Lagerbewegungen
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="istAdmin">True, wenn die angemeldete Person Adminrechte hat</param>
    /// <param name="id">Die ID der Person, deren Profil gelesen wird</param>
    /// <returns>Gibt das Profil zurück oder null, wenn es die Person nicht gibt</returns>
    public async Task<ProfilDTO?> ProfilAsync(int angemeldetID, bool istAdmin, int id)
    {
        var benutzer = await _benutzerService.PersonLesenAsync(id);
        if (benutzer == null)
        {
            return null;
        }

        var räume = await _standortService.MeineRäumeAsync(id);
        var raumIDs = räume.Select(r => r.ID).ToList();
        var bewegungen = await _buchungService.SichtbareBewegungenAsync(angemeldetID, istAdmin, null, null);

        return new ProfilDTO
        {
            Benutzer = benutzer,
            Raeume = räume,
            OffeneBewegungen = bewegungen
                .Where(b => b.Status == BuchungService.StatusOffen && b.PersonID == id)
                .OrderBy(b => b.ErstelltAm)
                .ThenBy(b => b.ID)
                .ToList(),
            // Eigene Buchungen, alles was in einem Raum der Person angekommen ist und was in einen ihrer Räume zurückgebucht wurde
            LetzteBewegungen = bewegungen
                .Where(b => b.PersonID == id
                    || (b.Status != BuchungService.StatusOffen && raumIDs.Contains(b.NachRaumID))
                    || (b.Status == BuchungService.StatusStorniert && raumIDs.Contains(b.VonRaumID)))
                .OrderByDescending(b => b.BestaetigtAm ?? b.ErstelltAm)
                .ThenByDescending(b => b.ID)
                .Take(AnzahlLetzteBewegungenImProfil)
                .ToList()
        };
    }
}
