using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Lagerverwaltung.Api.Services;

public class PasswortService
{
    public const int MindestLaenge = 8;
    private const byte FormatVersion = 0x01;
    private const uint PrfSha256 = 1;
    private const uint PrfSha512 = 2;
    private const int Iterationen = 600_000;
    private const int SaltLaenge = 16;
    private const int HashLaenge = 32;

    // Version (1) + PRF (4) + Iterationen (4) + Saltlänge (4)
    private const int KopfLaenge = 13;

    private readonly string _pepper;

    public PasswortService(IConfiguration configuration)
    {
        _pepper = configuration["Password:Pepper"]
            ?? throw new InvalidOperationException("Password:Pepper fehlt (User Secrets oder Umgebungsvariable Password__Pepper).");
    }

    
    //TODO: Kommentare vervollständigen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="passwort"></param>
    /// <returns></returns>
    public string HashPasswort(string passwort)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltLaenge);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(passwort + _pepper, salt, Iterationen, HashAlgorithmName.SHA256, HashLaenge);

        byte[] ergebnis = new byte[KopfLaenge + SaltLaenge + HashLaenge];
        ergebnis[0] = FormatVersion;
        BinaryPrimitives.WriteUInt32BigEndian(ergebnis.AsSpan(1), PrfSha256);
        BinaryPrimitives.WriteUInt32BigEndian(ergebnis.AsSpan(5), Iterationen);
        BinaryPrimitives.WriteUInt32BigEndian(ergebnis.AsSpan(9), SaltLaenge);
        salt.CopyTo(ergebnis, KopfLaenge);
        hash.CopyTo(ergebnis, KopfLaenge + SaltLaenge);

        return Convert.ToBase64String(ergebnis);
    }

    //TODO: Kommentare vervollständigen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="passwort"></param>
    /// <param name="gespeicherterHash"></param>
    /// <returns></returns>
    public bool PruefePasswort(string passwort, string gespeicherterHash)
    {
        byte[] daten;
        try
        {
            daten = Convert.FromBase64String(gespeicherterHash);
        }
        catch (FormatException)
        {
            return false;
        }

        if (daten.Length < KopfLaenge || daten[0] != FormatVersion)
        {
            return false;
        }

        uint prf = BinaryPrimitives.ReadUInt32BigEndian(daten.AsSpan(1));
        uint iterationen = BinaryPrimitives.ReadUInt32BigEndian(daten.AsSpan(5));
        uint saltLaenge = BinaryPrimitives.ReadUInt32BigEndian(daten.AsSpan(9));

        HashAlgorithmName algorithmus;
        if (prf == PrfSha256)
        {
            algorithmus = HashAlgorithmName.SHA256;
        }
        else if (prf == PrfSha512)
        {
            algorithmus = HashAlgorithmName.SHA512;
        }
        else
        {
            return false;
        }

        // Prüfung auf sinnlose Werte
        //if (iterationen == 0 || iterationen > int.MaxValue || saltLaenge < 16 || KopfLaenge + saltLaenge + 16 > daten.Length)
        //{
        //    return false;
        //}

        byte[] salt = daten.AsSpan(KopfLaenge, (int)saltLaenge).ToArray();
        byte[] erwartet = daten.AsSpan(KopfLaenge + (int)saltLaenge).ToArray();
        byte[] berechnet = Rfc2898DeriveBytes.Pbkdf2(passwort + _pepper, salt, (int)iterationen, algorithmus, erwartet.Length);

        return CryptographicOperations.FixedTimeEquals(berechnet, erwartet);
    }
}
