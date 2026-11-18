using Lagerverwaltung.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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
    }
}
