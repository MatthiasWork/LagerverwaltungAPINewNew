using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services;

public class HerstellerService
{
    private readonly LagerverwaltungContext _context;

    public HerstellerService(LagerverwaltungContext context)
    {
        _context = context;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task<List<Hersteller>> ListeAsync(string? name)
    {
        return await _context.Hersteller
            .FromSqlInterpolated($"EXEC spReadAllHersteller @Name = {name}")
            .ToListAsync();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Hersteller?> LesenAsync(int id)
    {
        var hersteller = await _context.Hersteller
            .FromSqlInterpolated($"EXEC spReadHerstellerByID @ID = {id}")
            .ToListAsync();
        return hersteller.FirstOrDefault();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="hersteller"></param>
    /// <returns></returns>
    public async Task<Hersteller> AnlegenAsync(HerstellerDTO hersteller)
    {
        var neuHersteller = new Hersteller { Name = hersteller.Name.Trim() };
        _context.Hersteller.Add(neuHersteller);
        await _context.SaveChangesAsync();   // -> spCreateHersteller
        return neuHersteller;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="hersteller"></param>
    /// <returns></returns>
    public async Task<Hersteller> ÄndernAsync(int id, HerstellerDTO hersteller)
    {
        var neuHersteller = new Hersteller { ID = id, Name = hersteller.Name.Trim() };
        _context.Hersteller.Update(neuHersteller);
        await _context.SaveChangesAsync();   // -> spUpdateHersteller
        return neuHersteller;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task LöschenAsync(int id)
    {
        _context.Hersteller.Remove(new Hersteller { ID = id });
        await _context.SaveChangesAsync();   // -> spDeleteHersteller
    }
}
