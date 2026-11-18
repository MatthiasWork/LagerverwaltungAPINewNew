using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services;

public class BuchungService
{
    private readonly LagerverwaltungContext _context;

    public BuchungService(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die einen Wareneingang in ein Lager bucht (spCreateLagerbewegungWareneingang)
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="dto">Gegenstand, Menge und Lager. Ohne Lager wird ins Hauptlager gebucht</param>
    /// <returns>Gibt die neue Lagerbewegung zurück</returns>
    public async Task<LagerbewegungLesenDTO> WareneingangAsync(int angemeldetID, WareneingangDTO dto)
    {
        string raumID = string.IsNullOrWhiteSpace(dto.RaumId) ? "HL" : dto.RaumId.Trim();
        var zeilen = await _context.Database
            .SqlQuery<LagerbewegungLesenDTO>($"EXEC dbo.spCreateLagerbewegungWareneingang @AngemeldetID = {angemeldetID}, @GegenstandID = {dto.GegenstandId}, @Menge = {dto.Menge}, @RaumID = {raumID}")
            .ToListAsync();
        return zeilen.First();
    }

    /// <summary>
    /// Methode, die eine Übergabe aus dem Hauptlager an einen Raum anlegt (spCreateLagerbewegungUebergabe)
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="dto">Gegenstand, Menge und Zielraum</param>
    /// <returns>Gibt die neue, offene Lagerbewegung zurück</returns>
    public async Task<LagerbewegungLesenDTO> ÜbergabeAnlegenAsync(int angemeldetID, ÜbergabeDTO dto)
    {
        var zeilen = await _context.Database
            .SqlQuery<LagerbewegungLesenDTO>($"EXEC dbo.spCreateLagerbewegungUebergabe @AngemeldetID = {angemeldetID}, @GegenstandID = {dto.GegenstandId}, @Menge = {dto.Menge}, @NachRaumID = {dto.NachRaumId.Trim()}")
            .ToListAsync();
        return zeilen.First();
    }

    /// <summary>
    /// Methode, die eine offene Übergabe bestätigt (spUpdateLagerbewegungBestaetigen)
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="id">Die ID der Lagerbewegung</param>
    /// <returns>Gibt die bestätigte Lagerbewegung zurück</returns>
    public async Task<LagerbewegungLesenDTO> ÜbergabeBestaetigenAsync(int angemeldetID, int id)
    {
        var zeilen = await _context.Database
            .SqlQuery<LagerbewegungLesenDTO>($"EXEC dbo.spUpdateLagerbewegungBestaetigen @AngemeldetID = {angemeldetID}, @ID = {id}")
            .ToListAsync();
        return zeilen.First();
    }

    /// <summary>
    /// Methode, die eine offene Übergabe storniert (spUpdateLagerbewegungStornieren)
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="id">Die ID der Lagerbewegung</param>
    /// <returns>Gibt die stornierte Lagerbewegung zurück</returns>
    public async Task<LagerbewegungLesenDTO> ÜbergabeStornierenAsync(int angemeldetID, int id)
    {
        var zeilen = await _context.Database
            .SqlQuery<LagerbewegungLesenDTO>($"EXEC dbo.spUpdateLagerbewegungStornieren @AngemeldetID = {angemeldetID}, @ID = {id}")
            .ToListAsync();
        return zeilen.First();
    }

    /// <summary>
    /// Methode, die eine Rückgabe aus einem Raum ins Umbuchungslager anlegt (spCreateLagerbewegungRueckgabe)
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="dto">Gegenstand, Menge und Raum, aus dem zurückgegeben wird</param>
    /// <returns>Gibt die neue, offene Lagerbewegung zurück</returns>
    public async Task<LagerbewegungLesenDTO> RückgabeAnlegenAsync(int angemeldetID, RückgabeDTO dto)
    {
        var zeilen = await _context.Database
            .SqlQuery<LagerbewegungLesenDTO>($"EXEC dbo.spCreateLagerbewegungRueckgabe @AngemeldetID = {angemeldetID}, @GegenstandID = {dto.GegenstandId}, @Menge = {dto.Menge}, @VonRaumID = {dto.VonRaumId.Trim()}")
            .ToListAsync();
        return zeilen.First();
    }

    /// <summary>
    /// Methode, die eine offene Rückgabe annimmt (spUpdateLagerbewegungRueckgabeAnnehmen)
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="id">Die ID der Rückgabe</param>
    /// <returns>Gibt zwei Lagerbewegungen zurück: die Rückgabe und die Einlagerung ins Hauptlager</returns>
    public async Task<List<LagerbewegungLesenDTO>> RückgabeAnnehmenAsync(int angemeldetID, int id)
    {
        return await _context.Database
            .SqlQuery<LagerbewegungLesenDTO>($"EXEC dbo.spUpdateLagerbewegungRueckgabeAnnehmen @AngemeldetID = {angemeldetID}, @ID = {id}")
            .ToListAsync();
    }
}
