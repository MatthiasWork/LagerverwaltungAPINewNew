using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Lagerverwaltung.Api.Authentication
{
    public class TokenAuthentifizierungHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemaName = "Bearer";
        public const string TokenHashKey = "TokenHash";
        private const string Präfix = "Bearer ";
        private readonly TokenService _tokenService;

        public TokenAuthentifizierungHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, TokenService tokenService) : base(options, logger, encoder)
        {
            _tokenService = tokenService;
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? header = Request.Headers.Authorization;
            if (string.IsNullOrEmpty(header) ||!header.StartsWith(Präfix, StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.NoResult();
            }

            string token = header.Substring(Präfix.Length).Trim();
            if(token.Length == 0)
            {
                return AuthenticateResult.Fail("Leeres Token");
            }

            byte[] tokenHash = TokenService.HashToken(token);
            var angemeldet = await _tokenService.PrüfenAsync(tokenHash);
            if(angemeldet == null)
            {
                return AuthenticateResult.Fail("Token ist ungültig oder abgelaufen");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, angemeldet.PersonID.ToString()),
                new Claim(ClaimTypes.Name, angemeldet.Username),
                new Claim(ClaimsPrincipalExtensions.RolleNameClaim, angemeldet.Rolle)
            };

            if(angemeldet.IstAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            Context.Items[TokenHashKey] = tokenHash;

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers.WWWAuthenticate = SchemaName;
            await Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Nicht angemeldet",
                detail: "Bitte mit einem gültigen Token im Header anmelden.").ExecuteAsync(Context);
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            await Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Keine Berechtigung",
                detail: "Für diese Funktion fehlen die notwendigen Rechte.").ExecuteAsync(Context);
        }
    }
}
