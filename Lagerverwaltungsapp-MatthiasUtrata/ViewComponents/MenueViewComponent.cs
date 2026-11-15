using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.ViewComponents
{
    // Das Hauptmenü; die Menüpunkte hängen davon ab, ob die Person Admin ist und ob sie einen Raum hat.
    public class MenueViewComponent : ViewComponent
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor, der den Datenbankkontext und den Lagerservice übernimmt.
        /// </summary>
        /// <param name="context">Der Datenbankkontext.</param>
        /// <param name="lagerService">Der Service für Zuständigkeiten.</param>
        public MenueViewComponent(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die die Menüpunkte und die Zahl offener Freigaben für die angemeldete Person ermittelt.
        /// Sie liest die Zuständigkeit bei jedem Aufruf neu, damit eine Änderung sofort gilt.
        /// </summary>
        /// <param name="ansicht">"Default" für die Seitenleiste, "TabLeiste" für die Tab-Leiste.</param>
        /// <returns>Das Menü in der gewählten Ansicht.</returns>
        public async Task<IViewComponentResult> InvokeAsync(string ansicht = "Default")
        {
            var personID = UserClaimsPrincipal.GetPersonID();
            if (personID == null)
            {
                // Nicht angemeldet, daher kein Menü.
                return Content(string.Empty);
            }

            var raeume = await _lagerService.RaeumeDerPersonAsync(personID.Value);

            var menue = new MenueViewModel
            {
                IstAdmin = UserClaimsPrincipal.IsInRole("Admin"),
                RaumIDs = raeume.Select(r => r.ID).ToList()
            };

            if (menue.ZeigtFreigaben)
            {
                // Die offenen Lagerbewegungen, die die Person freigeben muss.
                menue.OffeneUebernahmen = await _context.Lagerbewegung
                    .Where(Lagerbewegung.IstOffen)
                    .Where(Lagerbewegung.FreigabeDurch(personID.Value, menue.IstAdmin))
                    .CountAsync();
            }

            return View(ansicht, menue);
        }
    }
}
