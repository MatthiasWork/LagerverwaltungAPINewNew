USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Von-Raum ist ein Lager / Menge kleiner 1 / Gerät mit Menge ungleich 1, 50403 nicht zuständig für den Raum, 50404 Gegenstand bzw. Raum gibt es nicht, 50409 zu wenig Bestand im Raum / UL oder Bewegungsart fehlt
CREATE OR ALTER PROCEDURE dbo.spCreateLagerbewegungRueckgabe
    @AngemeldetID int,
    @GegenstandID int,
    @Menge        int,
    @VonRaumID    varchar(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Seriennummer nvarchar(50);
    DECLARE @BewegungsartID int;
    DECLARE @NeueID int;
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Gegenstand WHERE ID = @GegenstandID)
        BEGIN
            THROW 50404, N'Diesen Gegenstand gibt es nicht.', 1;
        END;
        SELECT @Seriennummer = Seriennummer FROM dbo.Gegenstand WHERE ID = @GegenstandID;

        SET @VonRaumID = TRIM(@VonRaumID);
        IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @VonRaumID)
        BEGIN
            THROW 50404, N'Diesen Raum gibt es nicht.', 1;
        END;
        SELECT @VonRaumID = ID FROM dbo.Raum WHERE ID = @VonRaumID;

        IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @VonRaumID AND PersonID = @AngemeldetID)
        BEGIN
            THROW 50403, N'Zurückgeben darf nur die für den Raum zuständige Person.', 1;
        END;
        IF EXISTS (SELECT 1 FROM dbo.Raum r JOIN dbo.Raumart ra ON ra.ID = r.RaumartID
                   WHERE r.ID = @VonRaumID AND LOWER(ra.Name) LIKE N'%lager%')
        BEGIN
            THROW 50400, N'Aus einem Lager wird nichts zurückgegeben. Rückgaben kommen aus einem Labor.', 1;
        END;
        IF @Menge IS NULL OR @Menge < 1
        BEGIN
            THROW 50400, N'Die Menge muss mindestens 1 sein.', 1;
        END;
        IF @Seriennummer IS NOT NULL AND @Menge <> 1
        BEGIN
            THROW 50400, N'Ein Gerät mit Seriennummer kann nur mit der Menge 1 gebucht werden.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = 'UL')
        BEGIN
            THROW 50409, N'Das Umbuchungslager (Raum UL) fehlt.', 1;
        END;

        SELECT @BewegungsartID = ID FROM dbo.Bewegungsart WHERE Name = N'Rueckgabe';
        IF @BewegungsartID IS NULL
        BEGIN
            THROW 50409, N'Die Bewegungsart "Rueckgabe" fehlt in der Datenbank (wird mit Testdaten.sql angelegt).', 1;
        END;

        EXEC dbo.spUpdateRaumbestandAbbuchen @GegenstandID, @VonRaumID, @Menge;

        INSERT INTO dbo.Lagerbewegung (Menge, ErstelltAm, BestaetigtAm, BewegungsartID, VonRaumID, NachRaumID, GegenstandID, PersonID)
        VALUES (@Menge, GETDATE(), NULL, @BewegungsartID, @VonRaumID, 'UL', @GegenstandID, @AngemeldetID);
        SET @NeueID = SCOPE_IDENTITY();

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
    WHERE l.ID = @NeueID;
END;
GO
