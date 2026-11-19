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
        public async Task<PersonLesenIDDTO?> PersonLesenAsync(int id)
        {
            var person = await _context.Database.SqlQuery<PersonLesenIDDTO>($"EXEC spReadPersonByID @ID = {id}").ToListAsync();
            return person.FirstOrDefault();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="suche"></param>
        /// <param name="rolleID"></param>
        /// <returns></returns>
        public async Task<List<PersonLesenIDDTO>> PersonenLesenAsync(string? suche, int? rolleID)
        {
            return await _context.Database.SqlQuery<PersonLesenIDDTO>($"EXEC spReadAllPerson @Suche = {suche}, @RolleID = {rolleID}").ToListAsync();
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
                .SqlQuery<PersonLesenUsernameDTO>($"EXEC spReadPersonByUsername @Username = {username}")
                .ToListAsync();
            var anmeldedaten = zeilen.FirstOrDefault();
            return anmeldedaten != null && _passwortService.PrüfePasswort(passwort, anmeldedaten.PasswortHash);
        }

        /// <summary>
        /// Methode, die die Daten einer Person ändert (spUpdatePerson) und bei einem neuen Passwort auch das Passwort (spUpdatePersonPasswort)
        /// Ein neues Passwort beendet alle Anmeldungen der Person außer der mit dem angegebenen Token
        /// </summary>
        /// <param name="id">Die ID der Person</param>
        /// <param name="dto">Die neuen Daten. Ohne neues Passwort bleibt das bisherige erhalten</param>
        /// <param name="tokenHash">Der Hash des Tokens, das gültig bleibt. Mit null enden alle Anmeldungen der Person</param>
        /// <returns>Gibt die geänderte Person zurück</returns>
        public async Task<PersonLesenIDDTO> ÄndernAsync(int id, BenutzerÄndernDTO dto, byte[]? tokenHash)
        {
            var person = new Person
            {
                ID = id,
                Vorname = dto.Vorname.Trim(),
                Nachname = dto.Nachname.Trim(),
                Username = dto.Benutzername.Trim(),
                Email = dto.Email.Trim()
            };
            _context.Person.Update(person);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(dto.NeuesPasswort))
            {
                string passwortHash = _passwortService.HashPasswort(dto.NeuesPasswort);
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
        public async Task<PersonLesenIDDTO> RolleÄndernAsync(int angemeldetID, int id, int rolleID)
        {
            var zeilen = await _context.Database
                .SqlQuery<PersonLesenIDDTO>($"EXEC spUpdatePersonRolle @AngemeldetID = {angemeldetID}, @ID = {id}, @RolleID = {rolleID}")
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

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<RolleLesenDTO>> RollenAsync()
        {
            return await _context.Database
                .SqlQuery<RolleLesenDTO>($"EXEC spReadAllRolle")
                .ToListAsync();
        }

        /// <summary>
        /// Methode, die eine Rolle liest (spReadRolleByID)
        /// </summary>
        /// <param name="id">Die ID der Rolle</param>
        /// <returns>Gibt die Rolle zurück oder null, wenn es sie nicht gibt</returns>
        public async Task<RolleLesenDTO?> RolleLesenAsync(int id)
        {
            var zeilen = await _context.Database
                .SqlQuery<RolleLesenDTO>($"EXEC spReadRolleByID @ID = {id}")
                .ToListAsync();
            return zeilen.FirstOrDefault();
        }

        /// <summary>
        /// Methode, die eine neue Rolle anlegt (spCreateRolle)
        /// </summary>
        /// <param name="rolle">Der Name der Rolle und ob sie Adminrechte hat</param>
        /// <returns>Gibt die ID der neuen Rolle zurück</returns>
        public async Task<int> RolleAnlegenAsync(RolleDTO rolle)
        {
            var neuRolle = new Rolle { Name = rolle.Name.Trim(), Admin = rolle.Admin };
            _context.Rolle.Add(neuRolle);
            await _context.SaveChangesAsync();
            return neuRolle.ID;
        }

        /// <summary>
        /// Methode, die den Namen und die Adminrechte einer Rolle ändert (spUpdateRolle)
        /// Die Prozedur lehnt ab, wenn danach niemand mehr Adminrechte hätte
        /// </summary>
        /// <param name="id">Die ID der Rolle</param>
        /// <param name="rolle">Der neue Name und ob die Rolle Adminrechte hat</param>
        public async Task RolleDatenÄndernAsync(int id, RolleDTO rolle)
        {
            var neuRolle = new Rolle { ID = id, Name = rolle.Name.Trim(), Admin = rolle.Admin };
            _context.Rolle.Update(neuRolle);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die eine Rolle löscht (spDeleteRolle)
        /// Eine Rolle, der noch Personen zugeordnet sind, lehnt die Prozedur ab
        /// </summary>
        /// <param name="id">Die ID der Rolle</param>
        public async Task RolleLöschenAsync(int id)
        {
            _context.Rolle.Remove(new Rolle { ID = id });
            await _context.SaveChangesAsync();
        }
    }
}
