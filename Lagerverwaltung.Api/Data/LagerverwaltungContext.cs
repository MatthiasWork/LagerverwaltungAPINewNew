using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lagerverwaltung.Api.Data;

public class LagerverwaltungContext : DbContext
{
    public LagerverwaltungContext(DbContextOptions<LagerverwaltungContext> options)
        : base(options)
    {
    }

    public DbSet<Kategorie> Kategorie { get; set; }

    public DbSet<Hersteller> Hersteller { get; set; }

    public DbSet<Gegenstand> Gegenstand { get; set; }

    public DbSet<Raum> Raum { get; set; }

    public DbSet<Raumart> Raumart { get; set; }

    public DbSet<Rolle> Rolle { get; set; }

    public DbSet<Bewegungsart> Bewegungsart { get; set; }

    public DbSet<Person> Person { get; set; }

    public DbSet<Token> Token { get; set; }

    public DbSet<Lagerbewegung> Lagerbewegung { get; set; }

    public DbSet<Raumbestand> Raumbestand { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Kategorie>(b =>
        {
            b.InsertUsingStoredProcedure("spCreateKategorie", "dbo",
                spb => spb.HasParameter(k => k.ID, p => p.IsOutput())
                          .HasParameter(k => k.Name));
            b.UpdateUsingStoredProcedure("spUpdateKategorie", "dbo",
                spb => spb.HasOriginalValueParameter(k => k.ID)
                          .HasParameter(k => k.Name));
            b.DeleteUsingStoredProcedure("spDeleteKategorie", "dbo",
                spb => spb.HasOriginalValueParameter(k => k.ID));
        });

        modelBuilder.Entity<Hersteller>(b =>
        {
            b.InsertUsingStoredProcedure("spCreateHersteller", "dbo",
                spb => spb.HasParameter(h => h.ID, p => p.IsOutput())
                          .HasParameter(h => h.Name));
            b.UpdateUsingStoredProcedure("spUpdateHersteller", "dbo",
                spb => spb.HasOriginalValueParameter(h => h.ID)
                          .HasParameter(h => h.Name));
            b.DeleteUsingStoredProcedure("spDeleteHersteller", "dbo",
                spb => spb.HasOriginalValueParameter(h => h.ID));
        });

        modelBuilder.Entity<Gegenstand>(b =>
        {
            b.InsertUsingStoredProcedure("spCreateGegenstand", "dbo",
                spb => spb.HasParameter(g => g.ID, p => p.IsOutput())
                          .HasParameter(g => g.Name)
                          .HasParameter(g => g.Seriennummer)
                          .HasParameter(g => g.KategorieID)
                          .HasParameter(g => g.HerstellerID));
            b.UpdateUsingStoredProcedure("spUpdateGegenstand", "dbo",
                spb => spb.HasOriginalValueParameter(g => g.ID)
                          .HasParameter(g => g.Name)
                          .HasParameter(g => g.Seriennummer)
                          .HasParameter(g => g.KategorieID)
                          .HasParameter(g => g.HerstellerID));
            b.DeleteUsingStoredProcedure("spDeleteGegenstand", "dbo",
                spb => spb.HasOriginalValueParameter(g => g.ID));
        });

        modelBuilder.Entity<Raum>(b =>
        {
            b.Property(r => r.ID).ValueGeneratedNever();
            b.InsertUsingStoredProcedure("spCreateRaum", "dbo",
                spb => spb.HasParameter(r => r.ID)
                          .HasParameter(r => r.PersonID)
                          .HasParameter(r => r.RaumartID));
            b.UpdateUsingStoredProcedure("spUpdateRaum", "dbo",
                spb => spb.HasOriginalValueParameter(r => r.ID)
                          .HasParameter(r => r.PersonID)
                          .HasParameter(r => r.RaumartID));
            b.DeleteUsingStoredProcedure("spDeleteRaum", "dbo",
                spb => spb.HasOriginalValueParameter(r => r.ID));
        });

        modelBuilder.Entity<Raumart>(b =>
        {
            b.InsertUsingStoredProcedure("spCreateRaumart", "dbo",
                spb => spb.HasParameter(r => r.ID, p => p.IsOutput())
                          .HasParameter(r => r.Name));
            b.UpdateUsingStoredProcedure("spUpdateRaumart", "dbo",
                spb => spb.HasOriginalValueParameter(r => r.ID)
                          .HasParameter(r => r.Name));
            b.DeleteUsingStoredProcedure("spDeleteRaumart", "dbo",
                spb => spb.HasOriginalValueParameter(r => r.ID));
        });

        modelBuilder.Entity<Rolle>(b =>
        {
            b.InsertUsingStoredProcedure("spCreateRolle", "dbo",
                spb => spb.HasParameter(r => r.ID, p => p.IsOutput())
                          .HasParameter(r => r.Name)
                          .HasParameter(r => r.Admin));
            b.UpdateUsingStoredProcedure("spUpdateRolle", "dbo",
                spb => spb.HasOriginalValueParameter(r => r.ID)
                          .HasParameter(r => r.Name)
                          .HasParameter(r => r.Admin));
            b.DeleteUsingStoredProcedure("spDeleteRolle", "dbo",
                spb => spb.HasOriginalValueParameter(r => r.ID));
        });

        modelBuilder.Entity<Bewegungsart>(b =>
        {
            b.InsertUsingStoredProcedure("spCreateBewegungsart", "dbo",
                spb => spb.HasParameter(a => a.ID, p => p.IsOutput())
                          .HasParameter(a => a.Name));
            b.UpdateUsingStoredProcedure("spUpdateBewegungsart", "dbo",
                spb => spb.HasOriginalValueParameter(a => a.ID)
                          .HasParameter(a => a.Name));
            b.DeleteUsingStoredProcedure("spDeleteBewegungsart", "dbo",
                spb => spb.HasOriginalValueParameter(a => a.ID));
        });

        modelBuilder.Entity<Person>(b =>
        {
            b.Property(p => p.RolleID).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            b.Property(p => p.RolleID).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            b.Property(p => p.Password).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            b.InsertUsingStoredProcedure("spCreatePerson", "dbo",
                spb => spb.HasParameter(p => p.Vorname)
                          .HasParameter(p => p.Nachname)
                          .HasParameter(p => p.Username)
                          .HasParameter(p => p.Email)
                          .HasParameter(p => p.Password, pb => pb.HasName("PasswortHash"))
                          .HasResultColumn(p => p.ID));
            b.UpdateUsingStoredProcedure("spUpdatePerson", "dbo",
                spb => spb.HasOriginalValueParameter(p => p.ID)
                          .HasParameter(p => p.Vorname)
                          .HasParameter(p => p.Nachname)
                          .HasParameter(p => p.Username)
                          .HasParameter(p => p.Email));
        });

        modelBuilder.Entity<Token>(b =>
        {
            // ErstelltAm und AbgelaufenAm setzt die Prozedur selbst
            b.Property(t => t.ErstelltAm).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            b.Property(t => t.AbgelaufenAm).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            b.InsertUsingStoredProcedure("spCreateToken", "dbo",
                spb => spb.HasParameter(t => t.ID, p => p.IsOutput())
                          .HasParameter(t => t.PersonID)
                          .HasParameter(t => t.TokenHash)
                          .HasParameter(t => t.LäuftAbAm));
        });

        modelBuilder.Entity<Raumbestand>()
            .HasKey(r => new { r.GegenstandID, r.RaumID });
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <returns></returns>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PruefeProzedurZuordnung();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    //TODO: Kommentare hinzufügen
    /// <summary>
    /// 
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PruefeProzedurZuordnung();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Methode, die vor dem Speichern prüft, ob für jede Änderung eine Stored Procedure zugeordnet ist.
    /// Ohne Zuordnung würde SaveChanges ein INSERT, UPDATE oder DELETE direkt auf die Tabelle ausführen
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn für eine Änderung keine Stored Procedure zugeordnet ist</exception>
    private void PruefeProzedurZuordnung()
    {
        foreach (var eintrag in ChangeTracker.Entries())
        {
            string? aktion = null;
            if (eintrag.State == EntityState.Added && eintrag.Metadata.GetInsertStoredProcedure() == null)
            {
                aktion = "das Anlegen";
            }
            else if (eintrag.State == EntityState.Modified && eintrag.Metadata.GetUpdateStoredProcedure() == null)
            {
                aktion = "das Ändern";
            }
            else if (eintrag.State == EntityState.Deleted && eintrag.Metadata.GetDeleteStoredProcedure() == null)
            {
                aktion = "das Löschen";
            }

            if (aktion != null)
            {
                throw new InvalidOperationException(
                    $"Für {aktion} von {eintrag.Metadata.ClrType.Name} ist keine Stored Procedure zugeordnet. " +
                    "Bitte die passende Prozedur direkt aufrufen (SqlQuery / ExecuteSqlInterpolated).");
            }
        }
    }
}
