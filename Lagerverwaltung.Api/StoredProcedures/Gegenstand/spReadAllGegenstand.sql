USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50403 nicht zuständig für den Raum / Filter nach fremder Person ohne Adminrechte, 50404 Raum gibt es nicht
CREATE OR ALTER PROCEDURE dbo.spReadAllGegenstand
    @AngemeldetID    int,
    @Name            nvarchar(100) = NULL,
    @KategorieID     int = NULL,
    @HerstellerID    int = NULL,
    @Seriennummer    nvarchar(50) = NULL,
    @MitSeriennummer bit = NULL,
    @RaumID          varchar(8) = NULL,
    @PersonID        int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IstAdmin bit = 0;
    DECLARE @Raeume TABLE (RaumID varchar(8) PRIMARY KEY);

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    SET @Name = NULLIF(TRIM(@Name), N'');
    SET @Seriennummer = NULLIF(TRIM(@Seriennummer), N'');
    SET @RaumID = NULLIF(TRIM(@RaumID), '');

    IF @RaumID IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @RaumID)
        BEGIN
            THROW 50404, N'Diesen Raum gibt es nicht.', 1;
        END;
        IF @IstAdmin = 0 AND NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @RaumID AND PersonID = @AngemeldetID)
        BEGIN
            THROW 50403, N'Für diesen Raum sind Sie nicht zuständig.', 1;
        END;
    END;

    IF @PersonID IS NOT NULL AND @IstAdmin = 0 AND (@AngemeldetID IS NULL OR @PersonID <> @AngemeldetID)
    BEGIN
        THROW 50403, N'Nach den Räumen anderer Personen darf nur ein Admin filtern.', 1;
    END;

    INSERT INTO @Raeume (RaumID)
    SELECT ID
    FROM dbo.Raum
    WHERE (@IstAdmin = 1 OR PersonID = @AngemeldetID)
      AND (@RaumID IS NULL OR ID = @RaumID)
      AND (@PersonID IS NULL OR PersonID = @PersonID);

    SELECT g.ID,
           g.Name,
           g.Seriennummer,
           g.KategorieID,
           k.Name AS Kategorie,
           g.HerstellerID,
           h.Name AS Hersteller,
           ISNULL((SELECT SUM(rb.Menge) FROM dbo.Raumbestand rb
                   WHERE rb.GegenstandID = g.ID AND rb.RaumID IN (SELECT RaumID FROM @Raeume)), 0) AS Bestand,
           ISNULL((SELECT SUM(l.Menge) FROM dbo.Lagerbewegung l
                   WHERE l.GegenstandID = g.ID AND l.BestaetigtAm IS NULL
                     AND (l.VonRaumID IN (SELECT RaumID FROM @Raeume) OR l.NachRaumID IN (SELECT RaumID FROM @Raeume))), 0) AS Unterwegs
    FROM dbo.Gegenstand g
    JOIN dbo.Kategorie k  ON k.ID = g.KategorieID
    JOIN dbo.Hersteller h ON h.ID = g.HerstellerID
    WHERE (@Name IS NULL OR CHARINDEX(@Name, g.Name) > 0)
      AND (@Seriennummer IS NULL OR CHARINDEX(@Seriennummer, g.Seriennummer) > 0)
      AND (@KategorieID IS NULL OR g.KategorieID = @KategorieID)
      AND (@HerstellerID IS NULL OR g.HerstellerID = @HerstellerID)
      AND (@MitSeriennummer IS NULL
           OR (@MitSeriennummer = 1 AND g.Seriennummer IS NOT NULL)
           OR (@MitSeriennummer = 0 AND g.Seriennummer IS NULL))
      AND ((@RaumID IS NULL AND @PersonID IS NULL)
           OR EXISTS (SELECT 1 FROM dbo.Raumbestand rb
                      WHERE rb.GegenstandID = g.ID AND rb.RaumID IN (SELECT RaumID FROM @Raeume)))
    ORDER BY g.Name, g.Seriennummer;
END;
GO
