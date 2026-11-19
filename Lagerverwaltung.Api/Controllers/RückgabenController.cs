using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/returns")]
public class RueckgabenController : ControllerBase
{
    private readonly BuchungService _buchungService;

    public RueckgabenController(BuchungService buchungService)
    {
        _buchungService = buchungService;
    }

    // POST: api/returns
    [HttpPost]
    public async Task<ActionResult<LagerbewegungLesenDTO>> Anlegen(RückgabeDTO rückgabe)
    {
        var bewegung = await _buchungService.RückgabeAnlegenAsync(User.GetPersonID(), rückgabe);
        return StatusCode(StatusCodes.Status201Created, bewegung);
    }

    // GET: api/returns/pending
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<LagerbewegungLesenDTO>>> Offene()
    {
        return await _buchungService.OffeneRückgabenAsync(User.GetPersonID());
    }

    // POST: api/returns/5/accept
    [HttpPost("{id:int}/accept")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<LagerbewegungLesenDTO>>> Annehmen(int id)
    {
        return await _buchungService.RückgabeAnnehmenAsync(User.GetPersonID(), id);
    }
}
