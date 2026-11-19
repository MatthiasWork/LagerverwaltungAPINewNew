using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class ÜbersichtController : ControllerBase
{
    private readonly ÜbersichtService _übersichtService;

    public ÜbersichtController(ÜbersichtService übersichtService)
    {
        _übersichtService = übersichtService;
    }

    // GET: api/dashboard
    [HttpGet]
    public async Task<ActionResult<ÜbersichtDTO>> Übersicht()
    {
        return await _übersichtService.ÜbersichtAsync(User.GetPersonID(), User.IsInRole("Admin"));
    }
}
