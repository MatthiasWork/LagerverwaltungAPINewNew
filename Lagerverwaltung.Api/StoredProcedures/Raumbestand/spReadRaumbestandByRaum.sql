USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50403 kein Admin und nicht zuständig, 50404 Raum gibt es nicht
CREATE OR ALTER PROCEDURE dbo.spReadRaumbestandByRaum
    @AngemeldetID int,
    @RaumID       varchar(8),
    @Name         nvarchar(100) = NULL,
    @KategorieID  int = NULL,
    @Seriennummer nvarchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    SET @RaumID = TRIM(@RaumID);
    SET @Name = NULLIF(TRIM(@Name), N'');
    SET @Seriennummer = NULLIF(TRIM(@Seriennummer), N'');

    IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @RaumID)
    BEGIN
        THROW 50404, N'Diesen Raum gibt es nicht.', 1;
    END;
    IF @IstAdmin = 0 AND NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @RaumID AND PersonID = @AngemeldetID)
    BEGIN
        THROW 50403, N'Für diesen Raum sind Sie nicht zuständig.', 1;
    END;

    SELECT g.ID   AS GegenstandID,
           g.Name,
           g.Seriennummer,
           g.KategorieID,
           k.Name AS Kategorie,
           h.Name AS Hersteller,
           rb.Menge
    FROM dbo.Raumbestand rb
    JOIN dbo.Gegenstand g ON g.ID = rb.GegenstandID
    JOIN dbo.Kategorie k  ON k.ID = g.KategorieID
    JOIN dbo.Hersteller h ON h.ID = g.HerstellerID
    WHERE rb.RaumID = @RaumID
      AND (@Name IS NULL OR CHARINDEX(@Name, g.Name) > 0)
      AND (@Seriennummer IS NULL OR CHARINDEX(@Seriennummer, g.Seriennummer) > 0)
      AND (@KategorieID IS NULL OR g.KategorieID = @KategorieID)
    ORDER BY g.Name, g.Seriennummer;
END;
GO
