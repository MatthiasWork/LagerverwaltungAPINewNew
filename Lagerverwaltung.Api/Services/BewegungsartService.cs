using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services;

public class BewegungsartService
{
    private readonly LagerverwaltungContext _context;

    public BewegungsartService(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Bewegungsarten liest (spReadAllBewegungsart)
    /// </summary>
    /// <param name="name">Der Suchbegriff für den Namen. Ohne Suchbegriff kommen alle Bewegungsarten</param>
    /// <returns>Gibt die Bewegungsarten nach Namen sortiert zurück</returns>
    public async Task<List<Bewegungsart>> ListeAsync(string? name)
    {
        return await _context.Bewegungsart
            .FromSqlInterpolated($"EXEC spReadAllBewegungsart @Name = {name}")
            .ToListAsync();
    }

    /// <summary>
    /// Methode, die eine Bewegungsart liest (spReadBewegungsartByID)
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart</param>
    /// <returns>Gibt die Bewegungsart zurück oder null, wenn es sie nicht gibt</returns>
    public async Task<Bewegungsart?> LesenAsync(int id)
    {
        var bewegungsarten = await _context.Bewegungsart
            .FromSqlInterpolated($"EXEC spReadBewegungsartByID @ID = {id}")
            .ToListAsync();
        return bewegungsarten.FirstOrDefault();
    }

    /// <summary>
    /// Methode, die eine neue Bewegungsart anlegt (spCreateBewegungsart)
    /// </summary>
    /// <param name="bewegungsart">Der Name der neuen Bewegungsart</param>
    /// <returns>Gibt die neue Bewegungsart mit ihrer ID zurück</returns>
    public async Task<Bewegungsart> AnlegenAsync(BewegungsartDTO bewegungsart)
    {
        var neuBewegungsart = new Bewegungsart { Name = bewegungsart.Name.Trim() };
        _context.Bewegungsart.Add(neuBewegungsart);
        await _context.SaveChangesAsync();   // -> spCreateBewegungsart
        return neuBewegungsart;
    }

    /// <summary>
    /// Methode, die eine Bewegungsart umbenennt (spUpdateBewegungsart)
    /// Die festen Bewegungsarten der Buchungen lehnt die Prozedur ab
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart</param>
    /// <param name="bewegungsart">Der neue Name</param>
    /// <returns>Gibt die geänderte Bewegungsart zurück</returns>
    public async Task<Bewegungsart> ÄndernAsync(int id, BewegungsartDTO bewegungsart)
    {
        var neuBewegungsart = new Bewegungsart { ID = id, Name = bewegungsart.Name.Trim() };
        _context.Bewegungsart.Update(neuBewegungsart);
        await _context.SaveChangesAsync();   // -> spUpdateBewegungsart
        return neuBewegungsart;
    }

    /// <summary>
    /// Methode, die eine Bewegungsart löscht (spDeleteBewegungsart)
    /// Feste Bewegungsarten und solche, die in Lagerbewegungen vorkommen, lehnt die Prozedur ab
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart</param>
    public async Task LöschenAsync(int id)
    {
        _context.Bewegungsart.Remove(new Bewegungsart { ID = id });
        await _context.SaveChangesAsync();   // -> spDeleteBewegungsart
    }
}
