using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/location-types")]
[Authorize(Roles = "Admin")]
public class RaumartenController : ControllerBase
{
    private readonly StandortService _standortService;

    public RaumartenController(StandortService standortService)
    {
        _standortService = standortService;
    }

    // GET: api/location-types
    [HttpGet]
    public async Task<ActionResult<List<RaumartLesenDTO>>> Liste()
    {
        return await _standortService.RaumartenAsync();
    }
}
