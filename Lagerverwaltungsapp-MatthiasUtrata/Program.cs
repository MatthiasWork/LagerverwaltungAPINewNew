using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Die Dienste werden registriert.
builder.Services.AddControllersWithViews(options =>
{
    // Jede Seite erfordert eine Anmeldung, außer sie ist mit [AllowAnonymous] markiert.
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
builder.Services.AddDbContext<LagerverwaltungContext>(
        options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Anmeldung per Cookie; nicht angemeldete Benutzer werden zum Login umgeleitet.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.LogoutPath = "/Home/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";

        // Die Person wird bei jeder Anfrage neu gelesen, damit entzogene Rechte sofort gelten und ein gelöschter Benutzer nicht mehr hineinkommt.
        // Nach einer Passwortänderung endet die Anmeldung, damit ein altes Cookie nicht weiter gilt.
        options.Events.OnValidatePrincipal = async context =>
        {
            Person? person = null;
            var personID = context.Principal?.GetPersonID();
            if (personID != null)
            {
                var datenbank = context.HttpContext.RequestServices.GetRequiredService<LagerverwaltungContext>();
                person = await datenbank.Person.AsNoTracking().Include(p => p.Rolle).FirstOrDefaultAsync(p => p.ID == personID);
            }

            if (person == null
                || context.Principal?.FindFirst(ClaimsPrincipalExtensions.PasswortStempelClaim)?.Value != person.PasswortStempel())
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            context.ReplacePrincipal(person.ErstelleClaimsPrincipal());
        };
    });

builder.Services.AddScoped<PasswordHasher<Person>>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<LagerService>();

// Der LoginSperreService merkt sich die fehlgeschlagenen Anmeldungen je Benutzername.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LoginSperreService>();

var app = builder.Build();

// Die Pipeline für die HTTP-Anfragen wird eingerichtet.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS gilt standardmäßig 30 Tage.
    app.UseHsts();
}

// Bei einem Fehlercode ohne Inhalt, z. B. 404, wird eine Seite mit Text angezeigt.
app.UseStatusCodePagesWithReExecute("/Home/Fehlercode/{0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
