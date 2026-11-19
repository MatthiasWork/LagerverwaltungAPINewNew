using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/users")]
public class BenutzerController : ControllerBase
{
    private readonly BenutzerService _benutzerService;
    private readonly AuthService _authService;
    private readonly ÜbersichtService _übersichtService;
    private readonly RolleService _rolleService;

    public BenutzerController(BenutzerService benutzerService, AuthService authService, ÜbersichtService übersichtService,
        RolleService rolleService)
    {
        _benutzerService = benutzerService;
        _authService = authService;
        _übersichtService = übersichtService;
        _rolleService = rolleService;
    }

    // GET: api/users/me
    [HttpGet("me")]
    public async Task<ActionResult<PersonLesenDTO>> Ich()
    {
        var benutzer = await _benutzerService.PersonLesenAsync(User.GetPersonID());
        if (benutzer == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Der Benutzer wurde nicht gefunden.");
        }
        return Ok(benutzer);
    }

    // PUT: api/users/me
    [HttpPut("me")]
    public async Task<ActionResult<PersonLesenDTO>> IchÄndern(ProfilÄndernDTO dto)
    {
        if (!string.IsNullOrEmpty(dto.NeuesPasswort))
        {
            if (string.IsNullOrEmpty(dto.AktuellesPasswort)
                || !await _benutzerService.PasswortTestenAsync(User.GetUsername(), dto.AktuellesPasswort))
            {
                ModelState.AddModelError(nameof(dto.AktuellesPasswort), "Das aktuelle Passwort fehlt oder ist falsch.");
                return ValidationProblem(ModelState);
            }
        }

        var benutzer = await _benutzerService.ÄndernAsync(User.GetPersonID(), dto.Vorname, dto.Nachname, dto.Benutzername, dto.Email,
            dto.NeuesPasswort, HttpContext.GetTokenHash());
        return Ok(benutzer);
    }

    // DELETE: api/users/me
    [HttpDelete("me")]
    public async Task<IActionResult> IchLöschen()
    {
        int id = User.GetPersonID();
        await _benutzerService.LöschenAsync(id, id);
        return NoContent();
    }

    // GET: api/users/me/profile
    [HttpGet("me/profile")]
    public async Task<ActionResult<ProfilDTO>> MeinProfil()
    {
        int id = User.GetPersonID();
        var profil = await _übersichtService.ProfilAsync(id, User.IsInRole("Admin"), id);
        if (profil == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Der Benutzer wurde nicht gefunden.");
        }
        return Ok(profil);
    }

    // GET: api/users/me/profile
    [HttpGet("me/profile")]
    public async Task<ActionResult<ProfilDTO>> MeinProfil()
    {
        int id = User.GetPersonID();
        var profil = await _übersichtService.ProfilAsync(id, User.IsInRole("Admin"), id);
        if (profil == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Der Benutzer wurde nicht gefunden.");
        }
        return Ok(profil);
    }

    // GET: api/users?suche=&rolleId=
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<PersonLesenDTO>>> Liste(string? suche, int? rolleId)
    {
        return await _benutzerService.PersonenLesenAsync(suche, rolleId);
    }

    // GET: api/users/5
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PersonLesenDTO>> Lesen(int id)
    {
        var benutzer = await _benutzerService.PersonLesenAsync(id);
        if (benutzer == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Der Benutzer wurde nicht gefunden.");
        }
        return Ok(benutzer);
    }

    // GET: api/users/5/profile
    [HttpGet("{id:int}/profile")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProfilDTO>> Profil(int id)
    {
        var profil = await _übersichtService.ProfilAsync(User.GetPersonID(), true, id);
        if (profil == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Der Benutzer wurde nicht gefunden.");
        }
        return Ok(profil);
    }

    // POST: api/users
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PersonLesenDTO>> Anlegen(BenutzerAnlegenDTO dto)
    {
        // Die Rolle wird vor dem Anlegen geprüft, sonst gäbe es den Benutzer bei einer unbekannten Rolle schon mit der Standardrolle
        if (dto.RolleId != null && await _rolleService.LesenAsync(dto.RolleId.Value) == null)
        {
            ModelState.AddModelError(nameof(dto.RolleId), "Diese Rolle gibt es nicht.");
            return ValidationProblem(ModelState);
        }

        var benutzer = await _authService.RegistrierenAsync(dto.Vorname, dto.Nachname, dto.Benutzername, dto.Email, dto.Passwort);
        if (dto.RolleId != null && dto.RolleId != benutzer.RolleID)
        {
            benutzer = await _benutzerService.RolleÄndernAsync(User.GetPersonID(), benutzer.ID, dto.RolleId.Value);
        }
        return CreatedAtAction(nameof(Lesen), new { id = benutzer.ID }, benutzer);
    }

    // PUT: api/users/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PersonLesenDTO>> Ändern(int id, BenutzerÄndernDTO dto)
    {
        // Ein neues Passwort beendet alle Anmeldungen der Person. Ändert der Admin sein eigenes, bleibt sein aktuelles Token gültig
        byte[]? behaltenTokenHash = null;
        if (id == User.GetPersonID())
        {
            behaltenTokenHash = HttpContext.GetTokenHash();
        }
        return await _benutzerService.ÄndernAsync(id, dto.Vorname, dto.Nachname, dto.Benutzername, dto.Email,
            dto.NeuesPasswort, behaltenTokenHash);
    }

    // PUT: api/users/5/role
    [HttpPut("{id:int}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PersonLesenDTO>> RolleÄndern(int id, BenutzerRolleDTO dto)
    {
        return await _benutzerService.RolleÄndernAsync(User.GetPersonID(), id, dto.RolleId);
    }

    // DELETE: api/users/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Löschen(int id)
    {
        await _benutzerService.LöschenAsync(User.GetPersonID(), id);
        return NoContent();
    }
}
