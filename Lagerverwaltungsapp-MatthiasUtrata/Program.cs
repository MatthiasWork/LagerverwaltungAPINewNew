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

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Jede Seite erfordert eine Anmeldung, außer sie ist mit [AllowAnonymous] markiert
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
builder.Services.AddDbContext<LagerverwaltungContext>(
        options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Anmeldung per Cookie; nicht angemeldete Benutzer werden zum Login umgeleitet
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.LogoutPath = "/Home/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";

        // Rolle und Admin-Recht stehen im Cookie. Damit entzogene Rechte sofort gelten und ein gelöschter Benutzer mit seinem
        // alten Cookie nicht mehr hineinkommt, wird die Person bei jeder Anfrage neu gelesen und der Inhalt des Cookies ersetzt
        options.Events.OnValidatePrincipal = async context =>
        {
            Person? person = null;
            var personID = context.Principal?.GetPersonID();
            if (personID != null)
            {
                var datenbank = context.HttpContext.RequestServices.GetRequiredService<LagerverwaltungContext>();
                person = await datenbank.Person.AsNoTracking().Include(p => p.Rolle).FirstOrDefaultAsync(p => p.ID == personID);
            }

            if (person == null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            context.ReplacePrincipal(person.ErstelleClaimsPrincipal());
        };
    });

builder.Services.AddScoped<IPasswordHasher<Person>, PasswordHasher<Person>>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<LagerService>();

// Merkt sich die fehlgeschlagenen Anmeldungen je Benutzername, siehe LoginSperreService
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<LoginSperreService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Bei einem Fehlercode ohne Inhalt (z. B. 404) eine Seite mit Text anzeigen statt einer leeren, siehe HomeController.Fehlercode
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
