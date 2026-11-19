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

    /// <summary>
    /// Methode, die eine neue Person mit der Standardrolle anlegt (spCreatePerson)
    /// Die Angaben kommen aus RegistrierenDTO (Registrierung) oder BenutzerAnlegenDTO (Anlegen durch einen Admin)
    /// </summary>
    /// <param name="vorname">Der Vorname</param>
    /// <param name="nachname">Der Nachname</param>
    /// <param name="benutzername">Der Benutzername</param>
    /// <param name="email">Die E-Mail-Adresse</param>
    /// <param name="passwort">Das Passwort im Klartext. Gespeichert wird nur der Hash</param>
    /// <returns>Gibt die neue Person zurück</returns>
    public async Task<PersonLesenDTO> RegistrierenAsync(string vorname, string nachname, string benutzername, string email, string passwort)
    {
        string passwortHash = _passwortService.HashPasswort(passwort);

        var neuPerson = new Person
        {
            Vorname = vorname.Trim(),
            Nachname = nachname.Trim(),
            Username = benutzername.Trim(),
            Email = email.Trim(),
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
            .SqlQuery<PersonByUsernameLesenDTO>($"EXEC spReadPersonByUsername @Username = {loginDaten.Benutzername.Trim()}")
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
