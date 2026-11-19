using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/receipts")]
[Authorize(Roles = "Admin")]
public class WareneingängeController : ControllerBase
{
    private readonly BuchungService _buchungService;

    public WareneingängeController(BuchungService buchungService)
    {
        _buchungService = buchungService;
    }

    // POST: api/receipts
    [HttpPost]
    public async Task<ActionResult<LagerbewegungLesenDTO>> Buchen(WareneingangDTO dto)
    {
        var bewegung = await _buchungService.WareneingangAsync(User.GetPersonID(), dto);
        return StatusCode(StatusCodes.Status201Created, bewegung);
    }
}
