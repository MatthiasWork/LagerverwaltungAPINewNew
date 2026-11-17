USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Person gibt es nicht, 50403 kein Admin, 50404 Raum gibt es nicht, 50409 Personenwechsel bei offenen Lagerbewegungen
CREATE OR ALTER PROCEDURE dbo.spUpdateRaumVerantwortlich
    @AngemeldetID int,
    @ID           varchar(8),
    @PersonID     int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @BisherigePersonID int;
    DECLARE @Anzahl int;
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    IF @IstAdmin = 0
    BEGIN
        THROW 50403, N'Nur ein Admin darf die zuständige Person eines Raums festlegen.', 1;
    END;

    SET @ID = TRIM(@ID);
    IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diesen Raum gibt es nicht.', 1;
    END;
    IF @PersonID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Person WHERE ID = @PersonID)
    BEGIN
        THROW 50400, N'Diese Person gibt es nicht.', 1;
    END;

    SELECT @BisherigePersonID = PersonID FROM dbo.Raum WHERE ID = @ID;
    IF @BisherigePersonID IS NOT NULL AND (@PersonID IS NULL OR @PersonID <> @BisherigePersonID)
    BEGIN
        SELECT @Anzahl = COUNT(*) FROM dbo.Lagerbewegung
        WHERE BestaetigtAm IS NULL AND (VonRaumID = @ID OR NachRaumID = @ID);
        IF @Anzahl > 0
        BEGIN
            SET @Meldung = CONCAT(N'Die zuständige Person von Raum ', @ID, N' kann nicht gewechselt werden, solange ', @Anzahl,
                                  N' offene Lagerbewegung(en) ihn betreffen.');
            THROW 50409, @Meldung, 1;
        END;
    END;

    UPDATE dbo.Raum SET PersonID = @PersonID WHERE ID = @ID;

    SELECT r.ID,
           r.RaumartID,
           ra.Name AS Raumart,
           CAST(CASE WHEN LOWER(ra.Name) LIKE N'%lager%' THEN 1 ELSE 0 END AS bit) AS IstLager,
           r.PersonID,
           CAST(CASE WHEN p.ID IS NULL THEN NULL ELSE CONCAT(p.Vorname, N' ', p.Nachname) END AS nvarchar(max)) AS Verantwortlich,
           (SELECT COUNT(*) FROM dbo.Raumbestand rb WHERE rb.RaumID = r.ID) AS Positionen,
           (SELECT ISNULL(SUM(rb.Menge), 0) FROM dbo.Raumbestand rb WHERE rb.RaumID = r.ID) AS Stueck
    FROM dbo.Raum r
    JOIN dbo.Raumart ra    ON ra.ID = r.RaumartID
    LEFT JOIN dbo.Person p ON p.ID = r.PersonID
    WHERE r.ID = @ID;
END;
GO
