USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50403 kein Admin, 50404 gibt es nicht oder ist keine Rückgabe, 50409 nicht mehr offen / HL oder Bewegungsart fehlt
CREATE OR ALTER PROCEDURE dbo.spUpdateLagerbewegungRueckgabeAnnehmen
    @AngemeldetID int,
    @ID           int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Bewegungsart nvarchar(20);
    DECLARE @BestaetigtAm datetime;
    DECLARE @GegenstandID int;
    DECLARE @UmbuchungslagerID varchar(8);
    DECLARE @Menge int;
    DECLARE @EinlagerungArtID int;
    DECLARE @EinlagerungID int;
    DECLARE @Jetzt datetime = GETDATE();
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    IF @IstAdmin = 0
    BEGIN
        THROW 50403, N'Rückgaben darf nur ein Admin annehmen.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @Bewegungsart = ba.Name, @BestaetigtAm = l.BestaetigtAm,
               @GegenstandID = l.GegenstandID, @UmbuchungslagerID = l.NachRaumID, @Menge = l.Menge
        FROM dbo.Lagerbewegung l WITH (UPDLOCK)
        JOIN dbo.Bewegungsart ba ON ba.ID = l.BewegungsartID
        WHERE l.ID = @ID;

        IF @Bewegungsart IS NULL OR @Bewegungsart <> N'Rueckgabe'
        BEGIN
            THROW 50404, N'Diese Rückgabe gibt es nicht.', 1;
        END;
        IF @BestaetigtAm IS NOT NULL
        BEGIN
            SET @Meldung = CONCAT(N'Diese Rückgabe wurde bereits am ', CONVERT(nvarchar(10), @BestaetigtAm, 104), N' ',
                                  CONVERT(nvarchar(5), @BestaetigtAm, 108), N' angenommen.');
            THROW 50409, @Meldung, 1;
        END;

        SELECT @EinlagerungArtID = ID FROM dbo.Bewegungsart WHERE Name = N'Einlagerung';
        IF @EinlagerungArtID IS NULL
        BEGIN
            THROW 50409, N'Die Bewegungsart "Einlagerung" fehlt in der Datenbank (wird mit Testdaten.sql angelegt).', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = 'HL')
        BEGIN
            THROW 50409, N'Das Hauptlager (Raum HL) fehlt.', 1;
        END;

        UPDATE dbo.Lagerbewegung
        SET BestaetigtAm = @Jetzt
        WHERE ID = @ID AND BestaetigtAm IS NULL;
        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50409, N'Diese Rückgabe wurde inzwischen bereits angenommen.', 1;
        END;

        INSERT INTO dbo.Lagerbewegung (Menge, ErstelltAm, BestaetigtAm, BewegungsartID, VonRaumID, NachRaumID, GegenstandID, PersonID)
        VALUES (@Menge, @Jetzt, @Jetzt, @EinlagerungArtID, @UmbuchungslagerID, 'HL', @GegenstandID, @AngemeldetID);
        SET @EinlagerungID = SCOPE_IDENTITY();

        EXEC dbo.spUpdateRaumbestandZubuchen @GegenstandID, 'HL', @Menge;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT l.ID,
           l.GegenstandID,
           g.Name         AS Gegenstand,
           g.Seriennummer,
           l.Menge,
           l.VonRaumID,
           l.NachRaumID,
           l.BewegungsartID,
           ba.Name        AS Bewegungsart,
           CAST(CASE
                    WHEN l.BestaetigtAm IS NULL THEN N'Offen'
                    WHEN ba.Name = N'Storniert' THEN N'Storniert'
                    ELSE N'Abgeschlossen'
                END AS nvarchar(20)) AS Status,
           l.ErstelltAm,
           l.BestaetigtAm,
           l.PersonID,
           p.Username     AS Person,
           CAST(CASE
                    WHEN l.BestaetigtAm IS NOT NULL THEN 0
                    WHEN ba.Name = N'Rueckgabe' THEN @IstAdmin
                    WHEN frei.PersonID = @AngemeldetID THEN 1
                    WHEN LOWER(frei_ra.Name) LIKE N'%lager%' AND @IstAdmin = 1 THEN 1
                    ELSE 0
                END AS bit) AS DarfBestaetigen
    FROM dbo.Lagerbewegung l
    JOIN dbo.Gegenstand g    ON g.ID = l.GegenstandID
    JOIN dbo.Bewegungsart ba ON ba.ID = l.BewegungsartID
    JOIN dbo.Person p        ON p.ID = l.PersonID
    JOIN dbo.Raum nach       ON nach.ID = l.NachRaumID
    JOIN dbo.Raum frei       ON frei.ID = CASE WHEN nach.PersonID = l.PersonID THEN l.VonRaumID ELSE l.NachRaumID END
    JOIN dbo.Raumart frei_ra ON frei_ra.ID = frei.RaumartID
    WHERE l.ID IN (@ID, @EinlagerungID)
    ORDER BY l.ID;
END;
GO
