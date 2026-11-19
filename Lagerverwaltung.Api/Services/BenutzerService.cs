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

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dto"></param>
        /// <param name="tokenHash"></param>
        /// <returns></returns>
        public async Task<PersonLesenIDDTO> ÄndernAsync(int id, ProfilÄndernDTO dto, byte[] tokenHash)
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
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC spUpdatePersonPasswort @ID = {id}, @PasswortHash = {passwortHash}, @BehaltenTokenHash = {tokenHash}");
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
    }
}
