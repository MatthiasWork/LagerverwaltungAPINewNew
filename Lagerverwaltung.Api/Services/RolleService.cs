using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services;

public class RolleService
{
    private readonly LagerverwaltungContext _context;

    public RolleService(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Rollen liest (spReadAllRolle)
    /// </summary>
    /// <returns>Gibt die Rollen nach Namen sortiert zurück</returns>
    public async Task<List<RolleLesenDTO>> ListeAsync()
    {
        return await _context.Database
            .SqlQuery<RolleLesenDTO>($"EXEC spReadAllRolle")
            .ToListAsync();
    }

    /// <summary>
    /// Methode, die eine Rolle liest (spReadRolleByID)
    /// </summary>
    /// <param name="id">Die ID der Rolle</param>
    /// <returns>Gibt die Rolle zurück oder null, wenn es sie nicht gibt</returns>
    public async Task<RolleLesenDTO?> LesenAsync(int id)
    {
        var zeilen = await _context.Database
            .SqlQuery<RolleLesenDTO>($"EXEC spReadRolleByID @ID = {id}")
            .ToListAsync();
        return zeilen.FirstOrDefault();
    }

    /// <summary>
    /// Methode, die eine neue Rolle anlegt (spCreateRolle)
    /// </summary>
    /// <param name="rolle">Der Name der Rolle und ob sie Adminrechte hat</param>
    /// <returns>Gibt die ID der neuen Rolle zurück</returns>
    public async Task<int> AnlegenAsync(RolleDTO rolle)
    {
        var neuRolle = new Rolle { Name = rolle.Name.Trim(), Admin = rolle.Admin };
        _context.Rolle.Add(neuRolle);
        await _context.SaveChangesAsync();   // -> spCreateRolle
        return neuRolle.ID;
    }

    /// <summary>
    /// Methode, die den Namen und die Adminrechte einer Rolle ändert (spUpdateRolle)
    /// Die Prozedur lehnt ab, wenn danach niemand mehr Adminrechte hätte
    /// </summary>
    /// <param name="id">Die ID der Rolle</param>
    /// <param name="rolle">Der neue Name und ob die Rolle Adminrechte hat</param>
    public async Task ÄndernAsync(int id, RolleDTO rolle)
    {
        var neuRolle = new Rolle { ID = id, Name = rolle.Name.Trim(), Admin = rolle.Admin };
        _context.Rolle.Update(neuRolle);
        await _context.SaveChangesAsync();   // -> spUpdateRolle
    }

    /// <summary>
    /// Methode, die eine Rolle löscht (spDeleteRolle)
    /// Eine Rolle, der noch Personen zugeordnet sind, lehnt die Prozedur ab
    /// </summary>
    /// <param name="id">Die ID der Rolle</param>
    public async Task LöschenAsync(int id)
    {
        _context.Rolle.Remove(new Rolle { ID = id });
        await _context.SaveChangesAsync();   // -> spDeleteRolle
    }
}
