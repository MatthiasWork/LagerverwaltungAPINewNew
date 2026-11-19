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
            return await _context.Database.SqlQuery<RaumByIDLesenDTO>($"EXEC spReadRaumByID @AngemeldetID = {angemeldetID}").ToListAsync();
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
            return await _context.Database.SqlQuery<RaumByIDLesenDTO>($"EXEC spReadRaumByID @AngemeldetID = {angemeldetID}, @ID = {id}").FirstAsync();
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
            return await _context.Database.SqlQuery<RaumByIDLesenDTO>($"EXEC spUpdateRaumVerantwortlich @AngemeldetID = {angemeldetID}, @ID = {id}, @PersonID = {personID}").FirstAsync();
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
            return await _context.Database.SqlQuery<RaumbestandByRaumLesenDTO>($"EXEC spReadRaumbestandByRaum @AngemeldetID = {angemeldetID}, @RaumID = {raumID}, @Name = {name}, @KategorieID = {kategorieID}, @Seriennumer = {seriennummer}").ToListAsync();
        }
    }
}
