using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/transfers")]
public class UebergabenController : ControllerBase
{
    private readonly BuchungService _buchungService;

    public UebergabenController(BuchungService buchungService)
    {
        _buchungService = buchungService;
    }

    // POST: api/transfers
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LagerbewegungLesenDTO>> Anlegen(ÜbergabeDTO übergabe)
    {
        var bewegung = await _buchungService.ÜbergabeAnlegenAsync(User.GetPersonID(), übergabe);
        return StatusCode(StatusCodes.Status201Created, bewegung);
    }

    // GET: api/transfers/pending
    [HttpGet("pending")]
    public async Task<ActionResult<List<LagerbewegungLesenDTO>>> Offene()
    {
        return await _buchungService.OffeneÜbergabenAsync(User.GetPersonID());
    }

    // POST: api/transfers/5/confirm
    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<LagerbewegungLesenDTO>> Bestätigen(int id)
    {
        return await _buchungService.ÜbergabeBestätigenAsync(User.GetPersonID(), id);
    }

    // POST: api/transfers/5/cancel
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<LagerbewegungLesenDTO>> Stornieren(int id)
    {
        return await _buchungService.ÜbergabeStornierenAsync(User.GetPersonID(), id);
    }
}
