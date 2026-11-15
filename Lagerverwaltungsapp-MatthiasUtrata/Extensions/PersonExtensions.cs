using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Lagerverwaltungsapp_MatthiasUtrata.Extensions
{
    public static class PersonExtensions
    {
        /// <summary>
        /// Methode, die die Anmeldedaten einer Person mit allen Claims liefert. Die Rolle "Admin" bekommt nur, wessen Rolle Admin-Rechte hat.
        /// </summary>
        /// <param name="person">Die angemeldete Person mit geladener Rolle.</param>
        /// <returns>Der angemeldete Benutzer mit allen Claims der Person.</returns>
        public static ClaimsPrincipal ErstelleClaimsPrincipal(this Person person)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, person.Email),
                new Claim(ClaimTypes.Sid, person.ID.ToString()),
                new Claim(ClaimTypes.Name, person.Username),
                new Claim(ClaimTypes.Surname, person.Nachname),
                new Claim(ClaimTypes.GivenName, person.Vorname),
                new Claim(ClaimsPrincipalExtensions.RolleNameClaim, person.Rolle.Name),
                new Claim(ClaimsPrincipalExtensions.PasswortStempelClaim, person.PasswortStempel())
            };

            if (person.Rolle.Admin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }

        /// <summary>
        /// Methode, die einen Stempel des Passworts liefert, der sich bei jedem neuen Passwort ändert. Der Hash selbst kommt nicht ins Cookie.
        /// </summary>
        /// <param name="person">Die Person mit geladenem Passwort-Hash.</param>
        /// <returns>Der Stempel als Hex-Text.</returns>
        public static string PasswortStempel(this Person person)
        {
            var wert = SHA256.HashData(Encoding.UTF8.GetBytes(person.Password));
            return Convert.ToHexString(wert, 0, 16);
        }
    }
}
