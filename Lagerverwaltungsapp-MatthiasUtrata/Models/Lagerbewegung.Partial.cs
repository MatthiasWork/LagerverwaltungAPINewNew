using System.Linq.Expressions;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

public partial class Lagerbewegung
{
    /// <summary>
    /// Methode, die die Bedingung für die Lagerbewegungen liefert, die die verantwortliche Person eines Raums freigeben muss.
    /// </summary>
    /// <param name="raumID">Die ID des Raums.</param>
    /// <returns>Die Bedingung für eine Abfrage.</returns>
    public static Expression<Func<Lagerbewegung, bool>> FreigabeFuer(string raumID)
    {
        return (Lagerbewegung l) => (l.NachRaum.PersonID == l.PersonID && l.VonRaumID == raumID)
            || (l.NachRaum.PersonID != l.PersonID && l.NachRaumID == raumID);
    }

    /// <summary>
    /// Methode, die die Bedingung für die Lagerbewegungen liefert, die eine Person freigeben darf.
    /// Ein Admin darf zusätzlich alle für ein Lager freigeben.
    /// </summary>
    /// <param name="personID">Die ID der Person.</param>
    /// <param name="istAdmin">True, wenn die Person Admin ist.</param>
    /// <returns>Die Bedingung für eine Abfrage.</returns>
    public static Expression<Func<Lagerbewegung, bool>> FreigabeDurch(int personID, bool istAdmin)
    {
        // Mit ToLower, damit die Prüfung auf Lager nicht von der Sortierung der Datenbank abhängt.
        return (Lagerbewegung l) => (l.NachRaum.PersonID == l.PersonID
                && (l.VonRaum.PersonID == personID
                    || (istAdmin && l.VonRaum.Raumart.Name.ToLower().Contains(Raumart.Lager))))
            || (l.NachRaum.PersonID != l.PersonID
                && (l.NachRaum.PersonID == personID
                    || (istAdmin && l.NachRaum.Raumart.Name.ToLower().Contains(Raumart.Lager))));
    }

    /// <summary>
    /// Methode, die die Bedingung für die Lagerbewegungen liefert, die für einen Raum angefragt wurden und von dessen Person zurückgezogen werden können.
    /// </summary>
    /// <param name="raumID">Die ID des Raums.</param>
    /// <returns>Die Bedingung für eine Abfrage.</returns>
    public static Expression<Func<Lagerbewegung, bool>> AngefragtFuer(string raumID)
    {
        return (Lagerbewegung l) => (l.NachRaum.PersonID == l.PersonID && l.NachRaumID == raumID)
            || (l.NachRaum.PersonID != l.PersonID && l.VonRaumID == raumID);
    }

    /// <summary>
    /// Gibt an, ob die Lagerbewegung noch offen ist.
    /// </summary>
    public bool Offen
    {
        get
        {
            return BestaetigtAm == null;
        }
    }

    /// <summary>
    /// Gibt an, ob die Lagerbewegung abgelehnt oder zurückgezogen wurde. Die Bewegungsart muss geladen sein.
    /// </summary>
    public bool Storniert
    {
        get
        {
            return BestaetigtAm != null && Bewegungsart?.Name == Models.Bewegungsart.Storniert;
        }
    }

    /// <summary>
    /// Gibt an, ob die Lagerbewegung eine Korrektur des Bestands ist. Die Bewegungsart muss geladen sein.
    /// </summary>
    public bool IstKorrektur
    {
        get
        {
            return Bewegungsart?.Name == Models.Bewegungsart.Korrektur;
        }
    }

    /// <summary>
    /// Gibt an, ob die Lagerbewegung eine Rückgabe an das Umbuchungslager ist. Die Bewegungsart muss geladen sein.
    /// </summary>
    public bool IstRueckgabe
    {
        get
        {
            return Bewegungsart?.Name == Models.Bewegungsart.Rückgabe;
        }
    }

    /// <summary>
    /// Gibt an, ob die Person des Nach-Raums die Lagerbewegung angefragt hat. Der Nach-Raum muss geladen sein.
    /// </summary>
    public bool VomNachRaumAngefragt
    {
        get
        {
            return NachRaum.PersonID == PersonID;
        }
    }

    /// <summary>
    /// Der Raum, dessen verantwortliche Person freigibt. Von-Raum und Nach-Raum müssen geladen sein.
    /// </summary>
    public Raum FreigabeRaum
    {
        get
        {
            if (VomNachRaumAngefragt)
            {
                return VonRaum;
            }
            return NachRaum;
        }
    }

    /// <summary>
    /// Der Raum, für den angefragt wurde und dessen verantwortliche Person zurückziehen darf. Von-Raum und Nach-Raum müssen geladen sein.
    /// </summary>
    public Raum AnfrageRaum
    {
        get
        {
            if (VomNachRaumAngefragt)
            {
                return NachRaum;
            }
            return VonRaum;
        }
    }

    /// <summary>
    /// Methode, die prüft, ob eine Person die Lagerbewegung freigeben (bestätigen oder ablehnen) darf.
    /// Von-Raum und Nach-Raum müssen mit ihrer Raumart geladen sein.
    /// </summary>
    /// <param name="personID">Die ID der Person.</param>
    /// <param name="istAdmin">True, wenn die Person Admin ist.</param>
    /// <returns>True, wenn die Person freigeben darf.</returns>
    public bool DarfFreigeben(int personID, bool istAdmin)
    {
        return FreigabeRaum.DarfBuchen(personID, istAdmin);
    }

    /// <summary>
    /// Methode, die prüft, ob eine Person die Lagerbewegung zurückziehen darf.
    /// Von-Raum und Nach-Raum müssen mit ihrer Raumart geladen sein.
    /// </summary>
    /// <param name="personID">Die ID der Person.</param>
    /// <param name="istAdmin">True, wenn die Person Admin ist.</param>
    /// <returns>True, wenn die Person zurückziehen darf.</returns>
    public bool DarfZurueckziehen(int personID, bool istAdmin)
    {
        return AnfrageRaum.DarfBuchen(personID, istAdmin);
    }
}
