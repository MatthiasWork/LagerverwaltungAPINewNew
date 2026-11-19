using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("Anmeldung")]
    public async Task<ActionResult<PersonLesenDTO>> Registrieren(RegistrierenDTO dto)
    {
        var benutzer = await _authService.RegistrierenAsync(dto.Vorname, dto.Nachname, dto.Benutzername, dto.Email, dto.Passwort);
        // Der Location-Header zeigt auf /api/users/me, weil der neue Benutzer /api/users/{id} (nur Admin) nicht abrufen darf.
        return Created("/api/users/me", benutzer);
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("Anmeldung")]
    public async Task<ActionResult<LoginAntwortDTO>> Login(LoginDTO dto)
    {
        var antwort = await _authService.AnmeldenAsync(dto);
        if (antwort == null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Anmeldung fehlgeschlagen",
                detail: "Benutzername oder Passwort ist falsch.");
        }
        return Ok(antwort);
    }

    // POST: api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.AbmeldenAsync(HttpContext.GetTokenHash());
        return NoContent();
    }
}
