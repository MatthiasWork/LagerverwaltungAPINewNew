using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Lagerverwaltung.Api.Services
{
    public class TokenService
    {
        private readonly LagerverwaltungContext _context;
        private readonly int _lebenszeitMinuten;

        public TokenService(LagerverwaltungContext context, IConfiguration configuration)
        {
            _context = context;
            _lebenszeitMinuten = configuration.GetValue("Token:LebenszeitMinuten", 480);
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static byte[] HashToken(string token)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(token));
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="personID"></param>
        /// <returns></returns>
        public async Task<(string Token, DateTime LäuftAbAm)> AnlegenAsync(int personID)
        {
            string token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
            byte[] tokenHash = HashToken(token);

            DateTime läuftAbAm = DateTime.Now.AddMinutes(_lebenszeitMinuten);

            var eintrag = new Token { PersonID = personID, TokenHash = tokenHash, LäuftAbAm = läuftAbAm };
            _context.Token.Add(eintrag);
            await _context.SaveChangesAsync();
            return (token, läuftAbAm);
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenHash"></param>
        /// <returns></returns>
        public async Task<TokenLesenDTO?> PrüfenAsync(byte[] tokenHash)
        {
            var zeilen = await _context.Database
                .SqlQuery<TokenLesenDTO>($"EXEC spReadTokenByHash @TokenHash = {tokenHash}")
                .ToListAsync();
            return zeilen.FirstOrDefault();
        }

        //TODO: Kommentare hinzufügen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenHash"></param>
        /// <returns></returns>
        public async Task WiderrufenAsync(byte[] tokenHash)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC spUpdateTokenWiderrufen @TokenHash = {tokenHash}");
        }
    }
}
