using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/movements")]
[Authorize(Roles = "Admin")]
public class BewegungenController : ControllerBase
{
    private readonly BuchungService _buchungService;

    public BewegungenController(BuchungService buchungService)
    {
        _buchungService = buchungService;
    }

    // GET: api/movements?raumId=&gegenstandId=&status=&bewegungsart=&von=&bis=
    [HttpGet]
    public async Task<ActionResult<List<LagerbewegungLesenDTO>>> Liste(string? raumId, int? gegenstandId, string? status,
        string? bewegungsart, DateTime? von, DateTime? bis)
    {
        return await _buchungService.BewegungenAsync(User.GetPersonID(), raumId, gegenstandId, status, bewegungsart, von, bis);
    }
}
