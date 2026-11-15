using System.Security.Claims;

namespace Lagerverwaltungsapp_MatthiasUtrata.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Der Claim mit dem Namen der Rolle. Er ist bewusst kein ClaimTypes.Role, sonst hätte jede Rolle mit dem Namen "Admin" Admin-Rechte.
        /// </summary>
        public const string RolleNameClaim = "RolleName";

        /// <summary>
        /// Der Claim mit dem Stempel des Passworts. Ändert sich das Passwort, passt der Stempel nicht mehr und die Anmeldung endet.
        /// </summary>
        public const string PasswortStempelClaim = "PasswortStempel";

        /// <summary>
        /// Methode, die den Namen der Rolle der angemeldeten Person aus dem Claim liest.
        /// </summary>
        /// <param name="user">Der angemeldete Benutzer.</param>
        /// <returns>Der Name der Rolle oder null, wenn niemand angemeldet ist.</returns>
        public static string? GetRolleName(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(RolleNameClaim);
        }

        /// <summary>
        /// Methode, die die ID der angemeldeten Person aus dem Claim liest.
        /// </summary>
        /// <param name="user">Der angemeldete Benutzer.</param>
        /// <returns>Die ID der Person oder null, wenn niemand angemeldet ist.</returns>
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
