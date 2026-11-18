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
            .FromSqlInterpolated($"EXEC dbo.spReadAllHersteller @Name = {name}")
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
            .FromSqlInterpolated($"EXEC dbo.spReadHerstellerByID @ID = {id}")
            .ToListAsync();
        return hersteller.FirstOrDefault();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<Hersteller> AnlegenAsync(HerstellerDTO dto)
    {
        var hersteller = new Hersteller { Name = dto.Name.Trim() };
        _context.Hersteller.Add(hersteller);
        await _context.SaveChangesAsync();   // -> spCreateHersteller
        return hersteller;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<Hersteller> AendernAsync(int id, HerstellerDTO dto)
    {
        var hersteller = new Hersteller { ID = id, Name = dto.Name.Trim() };
        _context.Hersteller.Update(hersteller);
        await _context.SaveChangesAsync();   // -> spUpdateHersteller
        return hersteller;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task LoeschenAsync(int id)
    {
        _context.Hersteller.Remove(new Hersteller { ID = id });
        await _context.SaveChangesAsync();   // -> spDeleteHersteller
    }
}
