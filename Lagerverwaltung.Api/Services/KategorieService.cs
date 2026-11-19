using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services;

public class KategorieService
{
    private readonly LagerverwaltungContext _context;

    public KategorieService(LagerverwaltungContext context)
    {
        _context = context;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task<List<Kategorie>> ListeAsync(string? name)
    {
        return await _context.Kategorie
            .FromSqlInterpolated($"EXEC spReadAllKategorie @Name = {name}")
            .ToListAsync();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Kategorie?> LesenAsync(int id)
    {
        var kategorien = await _context.Kategorie
            .FromSqlInterpolated($"EXEC spReadKategorieByID @ID = {id}")
            .ToListAsync();
        return kategorien.FirstOrDefault();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="kategorie"></param>
    /// <returns></returns>
    public async Task<Kategorie> AnlegenAsync(KategorieDTO kategorie)
    {
        var neuKategorie = new Kategorie { Name = kategorie.Name.Trim() };
        _context.Kategorie.Add(neuKategorie);
        await _context.SaveChangesAsync();   // -> spCreateKategorie
        return neuKategorie;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="kategorie"></param>
    /// <returns></returns>
    public async Task<Kategorie> ÄndernAsync(int id, KategorieDTO kategorie)
    {
        var neuKategorie = new Kategorie { ID = id, Name = kategorie.Name.Trim() };
        _context.Kategorie.Update(neuKategorie);
        await _context.SaveChangesAsync();   // -> spUpdateKategorie
        return neuKategorie;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task LöschenAsync(int id)
    {
        _context.Kategorie.Remove(new Kategorie { ID = id });
        await _context.SaveChangesAsync();   // -> spDeleteKategorie
    }
}
