using System.Threading.RateLimiting;
using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.Data;
using Lagerverwaltung.Api.Fehlerbehandlung;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["Password:Pepper"]))
{
    throw new InvalidOperationException(
        "Der Pepper fehlt. Bitte im Ordner Lagerverwaltung.Api mit 'dotnet user-secrets set \"Password:Pepper\" \"<Wert>\"' " +
        "setzen oder die Umgebungsvariable Password__Pepper setzen.");
}

builder.Services.AddDbContext<LagerverwaltungContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Der Connection String 'DefaultConnection' fehlt in der appsettings.json.")));

builder.Services.AddSingleton<PasswortService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BenutzerService>();
builder.Services.AddScoped<KategorieService>();
builder.Services.AddScoped<HerstellerService>();
builder.Services.AddScoped<ArtikelService>();
builder.Services.AddScoped<StandortService>();
builder.Services.AddScoped<BuchungService>();
builder.Services.AddScoped<BewegungsartService>();
builder.Services.AddScoped<RaumbestandService>();
builder.Services.AddScoped<ÜbersichtService>();

builder.Services.AddAuthentication(TokenAuthentifizierungHandler.SchemaName)
    .AddScheme<AuthenticationSchemeOptions, TokenAuthentifizierungHandler>(TokenAuthentifizierungHandler.SchemaName, null);

// Jeder Endpunkt ohne eigenes Attribut verlangt einen angemeldeten Benutzer
// Ausnahmen sind mit [AllowAnonymous] markiert (Login, Registrierung), Admin-Endpunkte mit [Authorize(Roles = "Admin")]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// SqlFehlerHandler übersetzt die Fehlernummern der Prozeduren
// Bei ungültigen Eingaben (Fehler 400) kommt eigene Fehlermeldung
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (context.ProblemDetails is Microsoft.AspNetCore.Mvc.ValidationProblemDetails)
        {
            context.ProblemDetails.Title = "Ungültige Eingaben";
        }
    };
});
builder.Services.AddExceptionHandler<SqlFehlerHandler>();

// Höchstens 10 Login- bzw. Registrierungsversuche pro Minute und IP-Adresse, danach kommt 429 Fehlermeldung
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("Anmeldung", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unbekannt",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        await Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Zu viele Anfragen",
            detail: "Zu viele Anmeldeversuche. Bitte in einer Minute erneut versuchen.")
            .ExecuteAsync(context.HttpContext);
    };
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var jsonFehler = context.ModelState.Keys.Any(k => k.StartsWith('$'));
            var parameterNamen = context.ActionDescriptor.Parameters.Select(p => p.Name).ToHashSet();
            var fehler = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();

            foreach (var (feld, eintrag) in context.ModelState)
            {
                if (eintrag.Errors.Count == 0)
                {
                    continue;
                }

                if (feld.StartsWith('$'))
                {
                    var name = feld.TrimStart('$', '.');
                    fehler.AddModelError(name == "" ? "body" : name, "Der Wert hat den falschen Datentyp oder der JSON-Inhalt ist ungültig.");
                }
                else if (parameterNamen.Contains(feld))
                {
                    if (!jsonFehler)
                    {
                        fehler.AddModelError("body", "Der Inhalt fehlt oder es ist kein gültiges JSON.");
                    }
                }
                else
                {
                    foreach (var e in eintrag.Errors)
                    {
                        fehler.AddModelError(feld, e.ErrorMessage);
                    }
                }
            }

            var problem = context.HttpContext.RequestServices
                .GetRequiredService<Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory>()
                .CreateValidationProblemDetails(context.HttpContext, fehler);
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
