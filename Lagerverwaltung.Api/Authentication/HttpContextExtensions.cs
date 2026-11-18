namespace Lagerverwaltung.Api.Authentication;

public static class HttpContextExtensions
{
    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static byte[] GetTokenHash(this HttpContext context)
    {
        if (context.Items[TokenAuthentifizierungHandler.TokenHashKey] is byte[] tokenHash)
        {
            return tokenHash;
        }
        throw new InvalidOperationException("Die Anfrage ist nicht mit einem Token angemeldet.");
    }
}