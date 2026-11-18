USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Menge kleiner 1 / Gerät mit Menge ungleich 1, 50403 kein Admin, 50404 Gegenstand bzw. Raum gibt es nicht / nicht im Bestand des Raums, 50409 Höchstbestand überschritten / Bewegungsart fehlt
CREATE OR ALTER PROCEDURE dbo.spUpdateRaumbestand
    @AngemeldetID int,
    @GegenstandID int,
    @RaumID       varchar(8),
    @Menge        int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @HoechstBestand bigint = 100000;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Name nvarchar(100);
    DECLARE @Seriennummer nvarchar(50);
    DECLARE @Aktuell int;
    DECLARE @Bisher bigint;
    DECLARE @BewegungsartID int;
    DECLARE @Jetzt datetime = GETDATE();
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    IF @IstAdmin = 0
    BEGIN
        THROW 50403, N'Nur ein Admin darf den Bestand eines Raums korrigieren.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @Name = Name, @Seriennummer = Seriennummer
        FROM dbo.Gegenstand WITH (UPDLOCK, HOLDLOCK)
        WHERE ID = @GegenstandID;
        IF @Name IS NULL
        BEGIN
            THROW 50404, N'Diesen Gegenstand gibt es nicht.', 1;
        END;

        SET @RaumID = TRIM(@RaumID);
        IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @RaumID)
        BEGIN
            THROW 50404, N'Diesen Raum gibt es nicht.', 1;
        END;
        SELECT @RaumID = ID FROM dbo.Raum WHERE ID = @RaumID;

        SELECT @Aktuell = Menge
        FROM dbo.Raumbestand WITH (UPDLOCK, HOLDLOCK)
        WHERE GegenstandID = @GegenstandID AND RaumID = @RaumID;
        IF @Aktuell IS NULL
        BEGIN
            SET @Meldung = CONCAT(N'"', @Name, N'" ist nicht im Bestand von Raum ', @RaumID, N'.');
            THROW 50404, @Meldung, 1;
        END;

        IF @Menge IS NULL OR @Menge < 1
        BEGIN
            THROW 50400, N'Die Menge muss mindestens 1 sein.', 1;
        END;
        IF @Seriennummer IS NOT NULL AND @Menge <> 1
        BEGIN
            THROW 50400, N'Ein Gerät mit Seriennummer kann nur mit der Menge 1 gebucht werden.', 1;
        END;

        -- Stimmt die gezählte Menge schon, wird nichts gebucht.
        IF @Menge <> @Aktuell
        BEGIN
            SET @Bisher = ISNULL((SELECT SUM(CAST(Menge AS bigint)) FROM dbo.Raumbestand WHERE GegenstandID = @GegenstandID), 0)
                        + ISNULL((SELECT SUM(CAST(Menge AS bigint)) FROM dbo.Lagerbewegung WHERE GegenstandID = @GegenstandID AND BestaetigtAm IS NULL), 0);
            IF @Bisher + @Menge - @Aktuell > @HoechstBestand
            BEGIN
                SET @Meldung = CONCAT(N'Von "', @Name, N'" kann es insgesamt höchstens ', @HoechstBestand, N' Stück geben, jetzt sind es ', @Bisher, N'.');
                THROW 50409, @Meldung, 1;
            END;

            SELECT @BewegungsartID = ID FROM dbo.Bewegungsart WHERE Name = N'Korrektur';
            IF @BewegungsartID IS NULL
            BEGIN
                THROW 50409, N'Die Bewegungsart "Korrektur" fehlt in der Datenbank (wird mit Testdaten.sql angelegt).', 1;
            END;

            UPDATE dbo.Raumbestand
            SET Menge = @Menge
            WHERE GegenstandID = @GegenstandID AND RaumID = @RaumID;

            -- Die Menge der Korrektur ist die Änderung des Bestands und kann daher negativ sein.
            INSERT INTO dbo.Lagerbewegung (Menge, ErstelltAm, BestaetigtAm, BewegungsartID, VonRaumID, NachRaumID, GegenstandID, PersonID)
            VALUES (@Menge - @Aktuell, @Jetzt, @Jetzt, @BewegungsartID, @RaumID, @RaumID, @GegenstandID, @AngemeldetID);
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

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
    WHERE rb.GegenstandID = @GegenstandID AND rb.RaumID = @RaumID;
END;
GO
