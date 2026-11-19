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

    public BenutzerController(BenutzerService benutzerService)
    {
        _benutzerService = benutzerService;
    }

    // GET: api/users/me
    [HttpGet("me")]
    public async Task<ActionResult<PersonLesenIDDTO>> Ich()
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
    public async Task<ActionResult<PersonLesenIDDTO>> IchÄndern(ProfilÄndernDTO dto)
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

        var benutzer = await _benutzerService.ÄndernAsync(User.GetPersonID(), dto, HttpContext.GetTokenHash());
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

    // GET: api/users?suche=&rolleId=
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<PersonLesenIDDTO>>> Liste(string? suche, int? rolleId)
    {
        return await _benutzerService.PersonenLesenAsync(suche, rolleId);
    }

    // GET: api/users/5
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PersonLesenIDDTO>> Lesen(int id)
    {
        var benutzer = await _benutzerService.PersonLesenAsync(id);
        if (benutzer == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Der Benutzer wurde nicht gefunden.");
        }
        return Ok(benutzer);
    }

    // PUT: api/users/5/role
    [HttpPut("{id:int}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PersonLesenIDDTO>> RolleÄndern(int id, RolleÄndernDTO dto)
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
