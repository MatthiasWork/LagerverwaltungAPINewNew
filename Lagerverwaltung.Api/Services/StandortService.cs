using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services
{
    public class StandortService
    {
        private readonly LagerverwaltungContext _context;

        public StandortService(LagerverwaltungContext context)
        {
            _context = context;
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="suche"></param>
        /// <param name="raumartID"></param>
        /// <param name="personID"></param>
        /// <returns></returns>
        public async Task<List<RaumByIDLesenDTO>> ListeAsync(string? suche, int? raumartID, int? personID)
        {
            return await _context.Database.SqlQuery<RaumByIDLesenDTO>($"EXEC spReadAllRaum @Suche = {suche}, @RaumartID = {raumartID}, @PersonID = {personID}").ToListAsync();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="angemeldetID"></param>
        /// <returns></returns>
        public async Task<List<RaumByIDLesenDTO>> MeineRäumeAsync(int angemeldetID)
        {
            return await _context.Database.SqlQuery<RaumByIDLesenDTO>($"EXEC spReadRaumByPerson @AngemeldetID = {angemeldetID}").ToListAsync();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="angemeldetID"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RaumByIDLesenDTO> LesenAsync(int angemeldetID, string id)
        {
            var zeilen = await _context.Database.SqlQuery<RaumByIDLesenDTO>($"EXEC spReadRaumByID @AngemeldetID = {angemeldetID}, @ID = {id}").ToListAsync();
            return zeilen.First();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="raum"></param>
        /// <returns></returns>
        public async Task<string> AnlegenAsync(RaumAnlegenDTO raum)
        {
            Raum neuRaum = new Raum { ID = raum.Id.Trim(), PersonID = raum.PersonId, RaumartID = raum.RaumartId };
            _context.Raum.Add(neuRaum);
            await _context.SaveChangesAsync();
            return neuRaum.ID;
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="raum"></param>
        /// <returns></returns>
        public async Task ÄndernAsync(string id, RaumÄndernDTO raum)
        {
            Raum neuRaum = new Raum { ID = id, PersonID = raum.PersonId, RaumartID = raum.RaumartId };
            _context.Raum.Update(neuRaum);
            await _context.SaveChangesAsync();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task LöschenAsync(string id)
        {
            _context.Raum.Remove(new Raum { ID = id });
            await _context.SaveChangesAsync();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="angemeldetID"></param>
        /// <param name="id"></param>
        /// <param name="personID"></param>
        /// <returns></returns>
        public async Task<RaumByIDLesenDTO> VerantwortlicheSetzenAsync(int angemeldetID, string id, int? personID)
        {
            var zeilen = await _context.Database.SqlQuery<RaumByIDLesenDTO>($"EXEC spUpdateRaumVerantwortlich @AngemeldetID = {angemeldetID}, @ID = {id}, @PersonID = {personID}").ToListAsync();
            return zeilen.First();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<RaumartLesenDTO>> RaumartenAsync()
        {
            return await _context.Database.SqlQuery<RaumartLesenDTO>($"EXEC spReadAllRaumart").ToListAsync();
        }

        /// <summary>
        /// Methode, die eine Raumart liest (spReadRaumartByID)
        /// </summary>
        /// <param name="id">Die ID der Raumart</param>
        /// <returns>Gibt die Raumart zurück oder null, wenn es sie nicht gibt</returns>
        public async Task<RaumartLesenDTO?> RaumartLesenAsync(int id)
        {
            var zeilen = await _context.Database.SqlQuery<RaumartLesenDTO>($"EXEC spReadRaumartByID @ID = {id}").ToListAsync();
            return zeilen.FirstOrDefault();
        }

        /// <summary>
        /// Methode, die eine neue Raumart anlegt (spCreateRaumart)
        /// </summary>
        /// <param name="raumart">Der Name der neuen Raumart</param>
        /// <returns>Gibt die ID der neuen Raumart zurück</returns>
        public async Task<int> RaumartAnlegenAsync(RaumartDTO raumart)
        {
            Raumart neuRaumart = new Raumart { Name = raumart.Name.Trim() };
            _context.Raumart.Add(neuRaumart);
            await _context.SaveChangesAsync();
            return neuRaumart.ID;
        }

        /// <summary>
        /// Methode, die eine Raumart umbenennt (spUpdateRaumart)
        /// </summary>
        /// <param name="id">Die ID der Raumart</param>
        /// <param name="raumart">Der neue Name</param>
        public async Task RaumartÄndernAsync(int id, RaumartDTO raumart)
        {
            Raumart neuRaumart = new Raumart { ID = id, Name = raumart.Name.Trim() };
            _context.Raumart.Update(neuRaumart);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die eine Raumart löscht (spDeleteRaumart)
        /// Eine Raumart, der noch Räume zugeordnet sind, lehnt die Prozedur ab
        /// </summary>
        /// <param name="id">Die ID der Raumart</param>
        public async Task RaumartLöschenAsync(int id)
        {
            _context.Raumart.Remove(new Raumart { ID = id });
            await _context.SaveChangesAsync();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="angemeldetID"></param>
        /// <param name="raumID"></param>
        /// <param name="name"></param>
        /// <param name="kategorieID"></param>
        /// <param name="seriennummer"></param>
        /// <returns></returns>
        public async Task<List<RaumbestandByRaumLesenDTO>> InventarAsync(int angemeldetID, string raumID, string? name, int? kategorieID, string? seriennummer)
        {
            return await _context.Database.SqlQuery<RaumbestandByRaumLesenDTO>($"EXEC spReadRaumbestandByRaum @AngemeldetID = {angemeldetID}, @RaumID = {raumID}, @Name = {name}, @KategorieID = {kategorieID}, @Seriennummer = {seriennummer}").ToListAsync();
        }
    }
}
