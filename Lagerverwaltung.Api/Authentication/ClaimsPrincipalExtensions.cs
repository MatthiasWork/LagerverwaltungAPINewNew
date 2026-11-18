using System.Security.Claims;

namespace Lagerverwaltung.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public const string RolleNameClaim = "RolleName";

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static int GetPersonID(this ClaimsPrincipal user)
    {
        if (int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            return id;
        }
        throw new InvalidOperationException("Es ist niemand angemeldet.");
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public static string GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Name) ?? "";
    }
}
