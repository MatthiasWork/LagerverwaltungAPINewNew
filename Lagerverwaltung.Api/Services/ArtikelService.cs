using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services;

public class ArtikelService
{
    private readonly LagerverwaltungContext _context;

    public ArtikelService(LagerverwaltungContext context)
    {
        _context = context;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="angemeldetID"></param>
    /// <param name="name"></param>
    /// <param name="kategorieID"></param>
    /// <param name="herstellerID"></param>
    /// <param name="seriennummer"></param>
    /// <param name="mitSeriennummer"></param>
    /// <param name="raumID"></param>
    /// <param name="personID"></param>
    /// <returns></returns>
    public async Task<List<GegenstandLesenIDAlleDTO>> SuchenAsync(int angemeldetID, string? name, int? kategorieID, int? herstellerID,
        string? seriennummer, bool? mitSeriennummer, string? raumID, int? personID)
    {
        return await _context.Database
            .SqlQuery<GegenstandLesenIDAlleDTO>($"EXEC dbo.spReadAllGegenstand @AngemeldetID = {angemeldetID}, @Name = {name}, @KategorieID = {kategorieID}, @HerstellerID = {herstellerID}, @Seriennummer = {seriennummer}, @MitSeriennummer = {mitSeriennummer}, @RaumID = {raumID}, @PersonID = {personID}")
            .ToListAsync();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="angemeldetID"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<GegenstandLesenIDAlleDTO?> LesenAsync(int angemeldetID, int id)
    {
        var zeilen = await _context.Database
            .SqlQuery<GegenstandLesenIDAlleDTO>($"EXEC dbo.spReadGegenstandByID @AngemeldetID = {angemeldetID}, @ID = {id}")
            .ToListAsync();
        return zeilen.FirstOrDefault();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="angemeldetID"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<List<RaumbestandByArtikelLesenDTO>> BestaendeAsync(int angemeldetID, int id)
    {
        return await _context.Database
            .SqlQuery<RaumbestandByArtikelLesenDTO>($"EXEC dbo.spReadRaumbestandByGegenstand @AngemeldetID = {angemeldetID}, @ID = {id}")
            .ToListAsync();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<int> AnlegenAsync(ArtikelDTO dto)
    {
        var gegenstand = new Gegenstand
        {
            Name = dto.Name.Trim(),
            Seriennummer = LeerAlsNull(dto.Seriennummer),
            KategorieID = dto.KategorieId,
            HerstellerID = dto.HerstellerId
        };
        _context.Gegenstand.Add(gegenstand);
        await _context.SaveChangesAsync();
        return gegenstand.ID;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task AendernAsync(int id, ArtikelDTO dto)
    {
        var gegenstand = new Gegenstand
        {
            ID = id,
            Name = dto.Name.Trim(),
            Seriennummer = LeerAlsNull(dto.Seriennummer),
            KategorieID = dto.KategorieId,
            HerstellerID = dto.HerstellerId
        };
        _context.Gegenstand.Update(gegenstand);
        await _context.SaveChangesAsync();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task LoeschenAsync(int id)
    {
        _context.Gegenstand.Remove(new Gegenstand { ID = id });
        await _context.SaveChangesAsync();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="seriennummer"></param>
    /// <returns></returns>
    private static string? LeerAlsNull(string? seriennummer)
    {
        return string.IsNullOrWhiteSpace(seriennummer) ? null : seriennummer.Trim();
    }
}
