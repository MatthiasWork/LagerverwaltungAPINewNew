using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Admin")]
public class RollenController : ControllerBase
{
    private readonly BenutzerService _benutzerService;

    public RollenController(BenutzerService benutzerService)
    {
        _benutzerService = benutzerService;
    }

    // GET: api/roles
    [HttpGet]
    public async Task<ActionResult<List<RolleLesenDTO>>> Liste()
    {
        return await _benutzerService.RollenAsync();
    }
}
