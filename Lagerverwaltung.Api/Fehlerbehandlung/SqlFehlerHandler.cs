using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using System.Net.NetworkInformation;

namespace Lagerverwaltung.Api.Fehlerbehandlung
{
    public class SqlFehlerHandler : IExceptionHandler
    {
        private readonly ILogger<SqlFehlerHandler> _logger;

        public SqlFehlerHandler(ILogger<SqlFehlerHandler> logger)
        {
            _logger = logger;
        }

        //TODO: Kommentare vervollständigen
        /// <summary>
        /// Methode, die den Fehlercode aus der StoredProcedure in eine Fehlermeldung für die Controller übersetzt
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="exception"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Gibt True oder False zurück</returns>
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            SqlException sqlFehler = FindeSqlFehler(exception)!;

            int fehlerCode;
            string titel;
            string meldung;

            switch(sqlFehler?.Number)
            {
                case 50400:
                    fehlerCode = StatusCodes.Status400BadRequest;
                    titel = "Ungültige Anfrage";
                    meldung = sqlFehler.Message;
                    break;
                case 50401:
                    fehlerCode = StatusCodes.Status401Unauthorized;
                    titel = "Nicht angemeldet";
                    meldung = sqlFehler.Message;
                    break;
                case 50403:
                    fehlerCode = StatusCodes.Status403Forbidden;
                    titel = "Keine Berechtigung auf den Zugriff";
                    meldung = sqlFehler.Message;
                    break;
                case 50404:
                    fehlerCode = StatusCodes.Status404NotFound;
                    titel = "Nicht gefunden";
                    meldung = sqlFehler.Message;
                    break;
                case 50409:
                    fehlerCode = StatusCodes.Status409Conflict;
                    titel = "Konflikt bei Anfrage";
                    meldung = sqlFehler.Message;
                    break;
                case 1205:
                    fehlerCode = StatusCodes.Status409Conflict;
                    titel = "Konflikt bei Anfrage";
                    meldung = sqlFehler.Message;
                    break;
                default:
                    _logger.LogError(exception, "Unerwarteter Fehler bei {Methode} {Pfad}", httpContext.Request.Method, httpContext.Request.Path);
                    fehlerCode = StatusCodes.Status500InternalServerError;
                    titel = "Serverfehler";
                    meldung = "Es ist ein unerwarteter Fehler aufgetreten. Bitte später erneut versuchen.";
                    break;
            }

            await Results.Problem(statusCode: fehlerCode, title: titel, detail: meldung).ExecuteAsync(httpContext);
            return true;
        }

        // TODO: Kommentare vervollständigen
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception">Die Fehlermeldung</param>
        /// <returns>Gibt eine SqlException zurück</returns>
        private static SqlException? FindeSqlFehler(Exception? exception)
        {
            while (exception != null)
            {
                if (exception is SqlException sqlException)
                {
                    return sqlException;
                }
                exception = exception.InnerException;
            }
            return null;
        }
    }
}
