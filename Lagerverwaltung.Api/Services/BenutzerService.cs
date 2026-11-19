using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services
{
    public class BenutzerService
    {
        private readonly LagerverwaltungContext _context;
        private readonly PasswortService _passwortService;

        public BenutzerService(LagerverwaltungContext context, PasswortService passwortService)
        {
            _context = context;
            _passwortService = passwortService;
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<PersonLesenDTO?> PersonLesenAsync(int id)
        {
            var person = await _context.Database.SqlQuery<PersonLesenDTO>($"EXEC spReadPersonByID @ID = {id}").ToListAsync();
            return person.FirstOrDefault();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="suche"></param>
        /// <param name="rolleID"></param>
        /// <returns></returns>
        public async Task<List<PersonLesenDTO>> PersonenLesenAsync(string? suche, int? rolleID)
        {
            return await _context.Database.SqlQuery<PersonLesenDTO>($"EXEC spReadAllPerson @Suche = {suche}, @RolleID = {rolleID}").ToListAsync();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="passwort"></param>
        /// <returns></returns>
        public async Task<bool> PasswortTestenAsync(string username, string passwort)
        {
            var zeilen = await _context.Database
                .SqlQuery<PersonByUsernameLesenDTO>($"EXEC spReadPersonByUsername @Username = {username}")
                .ToListAsync();
            var anmeldedaten = zeilen.FirstOrDefault();
            return anmeldedaten != null && _passwortService.PrüfePasswort(passwort, anmeldedaten.PasswortHash);
        }

        /// <summary>
        /// Methode, die die Daten einer Person ändert (spUpdatePerson) und bei einem neuen Passwort auch das Passwort (spUpdatePersonPasswort)
        /// Ein neues Passwort beendet alle Anmeldungen der Person außer der mit dem angegebenen Token
        /// </summary>
        /// <param name="id">Die ID der Person</param>
        /// <param name="vorname">Der neue Vorname</param>
        /// <param name="nachname">Der neue Nachname</param>
        /// <param name="benutzername">Der neue Benutzername</param>
        /// <param name="email">Die neue E-Mail-Adresse</param>
        /// <param name="neuesPasswort">Das neue Passwort im Klartext. Ohne Angabe bleibt das bisherige erhalten</param>
        /// <param name="tokenHash">Der Hash des Tokens, das gültig bleibt. Mit null enden alle Anmeldungen der Person</param>
        /// <returns>Gibt die geänderte Person zurück</returns>
        public async Task<PersonLesenDTO> ÄndernAsync(int id, string vorname, string nachname, string benutzername, string email,
            string? neuesPasswort, byte[]? tokenHash)
        {
            var person = new Person
            {
                ID = id,
                Vorname = vorname.Trim(),
                Nachname = nachname.Trim(),
                Username = benutzername.Trim(),
                Email = email.Trim()
            };
            _context.Person.Update(person);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(neuesPasswort))
            {
                string passwortHash = _passwortService.HashPasswort(neuesPasswort);
                if (tokenHash == null)
                {
                    // Der Parameter wird weggelassen, weil ein NULL ohne Datentyp nicht in binary(32) umgewandelt werden kann
                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"EXEC spUpdatePersonPasswort @ID = {id}, @PasswortHash = {passwortHash}");
                }
                else
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"EXEC spUpdatePersonPasswort @ID = {id}, @PasswortHash = {passwortHash}, @BehaltenTokenHash = {tokenHash}");
                }
            }

            var benutzer = await PersonLesenAsync(id);
            return benutzer!;
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        ///
        /// </summary>
        /// <param name="angemeldetID"></param>
        /// <param name="id"></param>
        /// <param name="rolleID"></param>
        /// <returns></returns>
        public async Task<PersonLesenDTO> RolleÄndernAsync(int angemeldetID, int id, int rolleID)
        {
            var zeilen = await _context.Database
                .SqlQuery<PersonLesenDTO>($"EXEC spUpdatePersonRolle @AngemeldetID = {angemeldetID}, @ID = {id}, @RolleID = {rolleID}")
                .ToListAsync();
            return zeilen.First();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="angemeldetID"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task LöschenAsync(int angemeldetID, int id)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC spDeletePerson @AngemeldetID = {angemeldetID}, @ID = {id}");
        }
    }
}
