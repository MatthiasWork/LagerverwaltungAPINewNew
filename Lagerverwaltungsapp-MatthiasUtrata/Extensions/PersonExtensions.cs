using System.Security.Claims;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Lagerverwaltungsapp_MatthiasUtrata.Extensions
{
    public static class PersonExtensions
    {
        /// <summary>
        /// Methode, die die Anmeldedaten einer Person liefert, wie sie im Cookie stehen und bei jeder Anfrage gelten.
        /// Die ID steht im Sid-Claim (siehe ClaimsPrincipalExtensions.GetPersonID); wer eine Admin-Rolle hat,
        /// bekommt zusätzlich die Rolle "Admin".
        /// </summary>
        /// <param name="person">Die Person, die angemeldet ist; ihre Rolle muss geladen sein</param>
        /// <returns>Der angemeldete Benutzer mit allen Claims der Person</returns>
        public static ClaimsPrincipal ErstelleClaimsPrincipal(this Person person)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, person.Email),
                new Claim(ClaimTypes.Sid, person.ID.ToString()),
                new Claim(ClaimTypes.Name, person.Username),
                new Claim(ClaimTypes.Surname, person.Nachname),
                new Claim(ClaimTypes.GivenName, person.Vorname),
                new Claim(ClaimTypes.Role, person.Rolle.Name)
            };

            if (person.Rolle.Admin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }
    }
}
