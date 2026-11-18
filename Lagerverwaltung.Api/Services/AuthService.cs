using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltung.Api.Services;

public class AuthService
{
    private readonly LagerverwaltungContext _context;
    private readonly PasswortService _passwortService;
    private readonly TokenService _tokenService;
    private readonly BenutzerService _benutzerService;

    public AuthService(LagerverwaltungContext context, PasswortService passwortService, TokenService tokenService,
        BenutzerService benutzerService)
    {
        _context = context;
        _passwortService = passwortService;
        _tokenService = tokenService;
        _benutzerService = benutzerService;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<PersonLesenIDDTO> RegistrierenAsync(RegistrierenDTO dto)
    {
        string passwortHash = _passwortService.HashPasswort(dto.Passwort);

        var zeilen = await _context.Database
            .SqlQuery<PersonLesenIDDTO>($"EXEC dbo.spCreatePerson @Vorname = {dto.Vorname.Trim()}, @Nachname = {dto.Nachname.Trim()}, @Username = {dto.Benutzername.Trim()}, @Email = {dto.Email.Trim()}, @PasswortHash = {passwortHash}")
            .ToListAsync();
        return zeilen.First();
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<LoginAntwortDTO?> AnmeldenAsync(LoginDTO dto)
    {
        var zeilen = await _context.Database
            .SqlQuery<PersonLesenUsernameDTO>($"EXEC dbo.spReadPersonByUsername @Username = {dto.Benutzername.Trim()}")
            .ToListAsync();
        var anmeldedaten = zeilen.FirstOrDefault();

        if (anmeldedaten == null)
        {
            // Unbekannter Benutzer: trotzdem einen Hash rechnen, damit die Antwort ungefähr gleich lange dauert
            // und man an der Zeit nicht erkennt, ob es den Benutzernamen gibt.
            _passwortService.HashPasswort(dto.Passwort);
            return null;
        }

        if (!_passwortService.PruefePasswort(dto.Passwort, anmeldedaten.PasswortHash))
        {
            return null;
        }

        var (token, läuftAbAm) = await _tokenService.AnlegenAsync(anmeldedaten.ID);
        var benutzer = await _benutzerService.PersonLesenAsync(anmeldedaten.ID);

        return new LoginAntwortDTO
        {
            Token = token,
            LaeuftAbAm = läuftAbAm,
            Benutzer = benutzer!
        };
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tokenHash"></param>
    /// <returns></returns>
    public async Task AbmeldenAsync(byte[] tokenHash)
    {
        await _tokenService.WiderrufenAsync(tokenHash);
    }
}
