using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
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
    /// <param name="person"></param>
    /// <returns></returns>
    public async Task<PersonLesenIDDTO> RegistrierenAsync(RegistrierenDTO person)
    {
        string passwortHash = _passwortService.HashPasswort(person.Passwort);

        var neuPerson = new Person
        {
            Vorname = person.Vorname.Trim(),
            Nachname = person.Nachname.Trim(),
            Username = person.Benutzername.Trim(),
            Email = person.Email.Trim(),
            Password = passwortHash
        };
        _context.Person.Add(neuPerson);
        await _context.SaveChangesAsync();

        var benutzer = await _benutzerService.PersonLesenAsync(neuPerson.ID);
        return benutzer!;
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="loginDaten"></param>
    /// <returns></returns>
    public async Task<LoginAntwortDTO?> AnmeldenAsync(LoginDTO loginDaten)
    {
        var zeilen = await _context.Database
            .SqlQuery<PersonLesenUsernameDTO>($"EXEC spReadPersonByUsername @Username = {loginDaten.Benutzername.Trim()}")
            .ToListAsync();
        var anmeldedaten = zeilen.FirstOrDefault();

        if (anmeldedaten == null)
        {
            _passwortService.HashPasswort(loginDaten.Passwort);
            return null;
        }

        if (!_passwortService.PrüfePasswort(loginDaten.Passwort, anmeldedaten.PasswortHash))
        {
            return null;
        }

        var (token, läuftAbAm) = await _tokenService.AnlegenAsync(anmeldedaten.ID);
        var benutzer = await _benutzerService.PersonLesenAsync(anmeldedaten.ID);

        return new LoginAntwortDTO
        {
            Token = token,
            LäuftAbAm = läuftAbAm,
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
