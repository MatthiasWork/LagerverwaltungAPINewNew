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
            .SqlQuery<GegenstandLesenIDAlleDTO>($"EXEC spReadAllGegenstand @AngemeldetID = {angemeldetID}, @Name = {name}, @KategorieID = {kategorieID}, @HerstellerID = {herstellerID}, @Seriennummer = {seriennummer}, @MitSeriennummer = {mitSeriennummer}, @RaumID = {raumID}, @PersonID = {personID}")
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
            .SqlQuery<GegenstandLesenIDAlleDTO>($"EXEC spReadGegenstandByID @AngemeldetID = {angemeldetID}, @ID = {id}")
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
    public async Task<List<RaumbestandByArtikelLesenDTO>> BeständeAsync(int angemeldetID, int id)
    {
        return await _context.Database
            .SqlQuery<RaumbestandByArtikelLesenDTO>($"EXEC spReadRaumbestandByGegenstand @AngemeldetID = {angemeldetID}, @ID = {id}")
            .ToListAsync();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="gegenstand"></param>
    /// <returns></returns>
    public async Task<int> AnlegenAsync(ArtikelDTO gegenstand)
    {
        var neuGegenstand = new Gegenstand
        {
            Name = gegenstand.Name.Trim(),
            Seriennummer = LeerZuNull(gegenstand.Seriennummer),
            KategorieID = gegenstand.KategorieId,
            HerstellerID = gegenstand.HerstellerId
        };
        _context.Gegenstand.Add(neuGegenstand);
        await _context.SaveChangesAsync();
        return neuGegenstand.ID;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="gegenstand"></param>
    /// <returns></returns>
    public async Task ÄndernAsync(int id, ArtikelDTO gegenstand)
    {
        var neuGegenstand = new Gegenstand
        {
            ID = id,
            Name = gegenstand.Name.Trim(),
            Seriennummer = LeerZuNull(gegenstand.Seriennummer),
            KategorieID = gegenstand.KategorieId,
            HerstellerID = gegenstand.HerstellerId
        };
        _context.Gegenstand.Update(neuGegenstand);
        await _context.SaveChangesAsync();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task LöschenAsync(int id)
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
    private static string? LeerZuNull(string? seriennummer)
    {
        return string.IsNullOrWhiteSpace(seriennummer) ? null : seriennummer.Trim();
    }
}
