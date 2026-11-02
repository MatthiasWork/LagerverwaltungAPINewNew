using System.Security.Claims;

namespace Lagerverwaltungsapp_MatthiasUtrata.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Art des Claims, in dem der Name der Rolle steht (z. B. "Laborvorstand"). Er ist bewusst kein ClaimTypes.Role: Als Rolle
        /// zählt nur "Admin", und das bekommt nur, wessen Rolle Admin-Rechte hat. Sonst hätte jede Rolle, die zufällig "Admin" heißt,
        /// diese Rechte.
        /// </summary>
        public const string RolleNameClaim = "RolleName";

        /// <summary>
        /// Art des Claims, in dem ein Stempel des Passworts steht (siehe PersonExtensions.PasswortStempel). Ändert sich das Passwort,
        /// passt der Stempel im Cookie nicht mehr und die Anmeldung endet.
        /// </summary>
        public const string PasswortStempelClaim = "PasswortStempel";

        /// <summary>
        /// Methode, die den Namen der Rolle der angemeldeten Person aus dem Claim liest, der beim Login gesetzt wird.
        /// </summary>
        /// <param name="user">Der angemeldete Benutzer</param>
        /// <returns>Der Name der Rolle oder null, wenn niemand angemeldet ist</returns>
        public static string? GetRolleName(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(RolleNameClaim);
        }

        /// <summary>
        /// Methode, die die ID der angemeldeten Person aus dem Sid-Claim liest, der beim Login gesetzt wird.
        /// </summary>
        /// <param name="user">Der angemeldete Benutzer</param>
        /// <returns>Die ID der Person oder null, wenn niemand angemeldet ist</returns>
        public static int? GetPersonID(this ClaimsPrincipal user)
        {
            if (int.TryParse(user.FindFirstValue(ClaimTypes.Sid), out var id))
            {
                return id;
            }
            return null;
        }
    }
}
