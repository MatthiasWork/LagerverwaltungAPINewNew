namespace Lagerverwaltung.Api.DTOs
{
    // Alle Zahlen beziehen sich auf die Räume, die die angemeldete Person sehen darf: ein Admin alle, sonst die eigenen
    public class ÜbersichtDTO
    {
        // Stück, die in einem Raum liegen
        public int Verfuegbar { get; set; }

        // Stück, die abgebucht, aber noch nicht bestätigt oder storniert sind
        public int Unterwegs { get; set; }

        public int Gesamt { get; set; }

        // Offene Lagerbewegungen, die die angemeldete Person bestätigen darf
        public int OffeneFreigaben { get; set; }

        public List<KategorieBestandDTO> BestandJeKategorie { get; set; } = new List<KategorieBestandDTO>();

        public List<RaumBestandDTO> BestandJeRaum { get; set; } = new List<RaumBestandDTO>();

        public List<LagerbewegungLesenDTO> LetzteBewegungen { get; set; } = new List<LagerbewegungLesenDTO>();
    }

    public class KategorieBestandDTO
    {
        public int KategorieID { get; set; }

        public string Kategorie { get; set; } = null!;

        public int Stueck { get; set; }
    }

    public class RaumBestandDTO
    {
        public string RaumID { get; set; } = null!;

        public string Raumart { get; set; } = null!;

        public int Stueck { get; set; }
    }
}
