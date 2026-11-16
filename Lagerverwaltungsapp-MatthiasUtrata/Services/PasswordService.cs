using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.AspNetCore.Identity;

namespace Lagerverwaltungsapp_MatthiasUtrata.Services
{
    
    public class PasswordService
    {
        private readonly PasswordHasher<Person> _passwordHasher;
        private readonly string _pepper;

        /// <summary>
        /// Konstruktor, der den PasswordHasher übernimmt und den Pepper aus der Konfiguration liest.
        /// </summary>
        /// <param name="passwordHasher">Der PasswordHasher.</param>
        /// <param name="configuration">Die Konfiguration aus der appsettings.json.</param>
        /// <exception cref="InvalidOperationException">Wenn der Pepper in der appsettings.json fehlt.</exception>
        public PasswordService(PasswordHasher<Person> passwordHasher, IConfiguration configuration)
        {
            _passwordHasher = passwordHasher;
            _pepper = configuration["Password:Pepper"]
                ?? throw new InvalidOperationException("Password:Pepper fehlt in der appsettings.json");
        }

        /// <summary>
        /// Methode, die ein Passwort zusammen mit dem Pepper hasht.
        /// </summary>
        /// <param name="person">Die Person.</param>
        /// <param name="password">Das Passwort.</param>
        /// <returns>Der Hashwert des Passworts.</returns>
        public string HashPassword(Person person, string password)
        {
            return _passwordHasher.HashPassword(person, password + _pepper);
        }

        /// <summary>
        /// Methode, die prüft, ob das eingegebene Passwort zum gespeicherten Passwort der Person passt.
        /// </summary>
        /// <param name="person">Die Person.</param>
        /// <param name="password">Das eingegebene Passwort.</param>
        /// <returns>True, wenn das Passwort stimmt.</returns>
        public bool VerifyPassword(Person person, string password)
        {
            try
            {
                var result = _passwordHasher.VerifyHashedPassword(person, person.Password, password + _pepper);
                return result != PasswordVerificationResult.Failed;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
