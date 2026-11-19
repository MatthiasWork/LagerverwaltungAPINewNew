using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services;

public class RaumartService
{
    private readonly LagerverwaltungContext _context;

    public RaumartService(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Raumarten liest (spReadAllRaumart)
    /// </summary>
    /// <returns>Gibt die Raumarten nach Namen sortiert zurück</returns>
    public async Task<List<RaumartLesenDTO>> ListeAsync()
    {
        return await _context.Database
            .SqlQuery<RaumartLesenDTO>($"EXEC spReadAllRaumart")
            .ToListAsync();
    }

    /// <summary>
    /// Methode, die eine Raumart liest (spReadRaumartByID)
    /// </summary>
    /// <param name="id">Die ID der Raumart</param>
    /// <returns>Gibt die Raumart zurück oder null, wenn es sie nicht gibt</returns>
    public async Task<RaumartLesenDTO?> LesenAsync(int id)
    {
        var zeilen = await _context.Database
            .SqlQuery<RaumartLesenDTO>($"EXEC spReadRaumartByID @ID = {id}")
            .ToListAsync();
        return zeilen.FirstOrDefault();
    }

    /// <summary>
    /// Methode, die eine neue Raumart anlegt (spCreateRaumart)
    /// </summary>
    /// <param name="raumart">Der Name der neuen Raumart</param>
    /// <returns>Gibt die ID der neuen Raumart zurück</returns>
    public async Task<int> AnlegenAsync(RaumartDTO raumart)
    {
        var neuRaumart = new Raumart { Name = raumart.Name.Trim() };
        _context.Raumart.Add(neuRaumart);
        await _context.SaveChangesAsync();   // -> spCreateRaumart
        return neuRaumart.ID;
    }

    /// <summary>
    /// Methode, die eine Raumart umbenennt (spUpdateRaumart)
    /// </summary>
    /// <param name="id">Die ID der Raumart</param>
    /// <param name="raumart">Der neue Name</param>
    public async Task ÄndernAsync(int id, RaumartDTO raumart)
    {
        var neuRaumart = new Raumart { ID = id, Name = raumart.Name.Trim() };
        _context.Raumart.Update(neuRaumart);
        await _context.SaveChangesAsync();   // -> spUpdateRaumart
    }

    /// <summary>
    /// Methode, die eine Raumart löscht (spDeleteRaumart)
    /// Eine Raumart, der noch Räume zugeordnet sind, lehnt die Prozedur ab
    /// </summary>
    /// <param name="id">Die ID der Raumart</param>
    public async Task LöschenAsync(int id)
    {
        _context.Raumart.Remove(new Raumart { ID = id });
        await _context.SaveChangesAsync();   // -> spDeleteRaumart
    }
}
