using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services;

public class RaumbestandService
{
    private readonly LagerverwaltungContext _context;

    public RaumbestandService(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die einen Gegenstand in den Bestand eines Raums aufnimmt (spCreateRaumbestand)
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="gegenstandID">Die ID des Gegenstands</param>
    /// <param name="raumID">Die ID des Raums</param>
    /// <param name="menge">Die Menge im Raum</param>
    /// <returns>Gibt die neue Bestandszeile zurück</returns>
    public async Task<RaumbestandByRaumLesenDTO> AnlegenAsync(int angemeldetID, int gegenstandID, string raumID, int menge)
    {
        var zeilen = await _context.Database
            .SqlQuery<RaumbestandByRaumLesenDTO>($"EXEC spCreateRaumbestand @AngemeldetID = {angemeldetID}, @GegenstandID = {gegenstandID}, @RaumID = {raumID}, @Menge = {menge}")
            .ToListAsync();
        return zeilen.First();
    }

    /// <summary>
    /// Methode, die die Menge eines Gegenstands in einem Raum korrigiert (spUpdateRaumbestand)
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="gegenstandID">Die ID des Gegenstands</param>
    /// <param name="raumID">Die ID des Raums</param>
    /// <param name="menge">Die neue Menge im Raum</param>
    /// <returns>Gibt die geänderte Bestandszeile zurück</returns>
    public async Task<RaumbestandByRaumLesenDTO> ÄndernAsync(int angemeldetID, int gegenstandID, string raumID, int menge)
    {
        var zeilen = await _context.Database
            .SqlQuery<RaumbestandByRaumLesenDTO>($"EXEC spUpdateRaumbestand @AngemeldetID = {angemeldetID}, @GegenstandID = {gegenstandID}, @RaumID = {raumID}, @Menge = {menge}")
            .ToListAsync();
        return zeilen.First();
    }

    /// <summary>
    /// Methode, die einen Gegenstand aus dem Bestand eines Raums entfernt (spDeleteRaumbestand)
    /// </summary>
    /// <param name="angemeldetID">Die ID der angemeldeten Person</param>
    /// <param name="gegenstandID">Die ID des Gegenstands</param>
    /// <param name="raumID">Die ID des Raums</param>
    public async Task LöschenAsync(int angemeldetID, int gegenstandID, string raumID)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC spDeleteRaumbestand @AngemeldetID = {angemeldetID}, @GegenstandID = {gegenstandID}, @RaumID = {raumID}");
    }
}
