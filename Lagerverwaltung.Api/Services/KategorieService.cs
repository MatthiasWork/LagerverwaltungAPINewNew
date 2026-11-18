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
            .FromSqlInterpolated($"EXEC dbo.spReadAllKategorie @Name = {name}")
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
            .FromSqlInterpolated($"EXEC dbo.spReadKategorieByID @ID = {id}")
            .ToListAsync();
        return kategorien.FirstOrDefault();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<Kategorie> AnlegenAsync(KategorieDTO dto)
    {
        var kategorie = new Kategorie { Name = dto.Name.Trim() };
        _context.Kategorie.Add(kategorie);
        await _context.SaveChangesAsync();   // -> spCreateKategorie
        return kategorie;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<Kategorie> AendernAsync(int id, KategorieDTO dto)
    {
        var kategorie = new Kategorie { ID = id, Name = dto.Name.Trim() };
        _context.Kategorie.Update(kategorie);
        await _context.SaveChangesAsync();   // -> spUpdateKategorie
        return kategorie;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task LoeschenAsync(int id)
    {
        _context.Kategorie.Remove(new Kategorie { ID = id });
        await _context.SaveChangesAsync();   // -> spDeleteKategorie
    }
}
