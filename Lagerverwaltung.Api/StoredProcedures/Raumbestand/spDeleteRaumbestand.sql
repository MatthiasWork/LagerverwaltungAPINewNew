USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50403 kein Admin, 50404 Gegenstand bzw. Raum gibt es nicht / nicht im Bestand des Raums, 50409 Bewegungsart fehlt
CREATE OR ALTER PROCEDURE dbo.spDeleteRaumbestand
    @AngemeldetID int,
    @GegenstandID int,
    @RaumID       varchar(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Name nvarchar(100);
    DECLARE @Seriennummer nvarchar(50);
    DECLARE @Aktuell int;
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

        SELECT @BewegungsartID = ID FROM dbo.Bewegungsart WHERE Name = N'Korrektur';
        IF @BewegungsartID IS NULL
        BEGIN
            THROW 50409, N'Die Bewegungsart "Korrektur" fehlt in der Datenbank (wird mit Testdaten.sql angelegt).', 1;
        END;

        DELETE FROM dbo.Raumbestand
        WHERE GegenstandID = @GegenstandID AND RaumID = @RaumID;

        -- Die Menge der Korrektur ist die Änderung des Bestands und daher hier negativ.
        INSERT INTO dbo.Lagerbewegung (Menge, ErstelltAm, BestaetigtAm, BewegungsartID, VonRaumID, NachRaumID, GegenstandID, PersonID)
        VALUES (-@Aktuell, @Jetzt, @Jetzt, @BewegungsartID, @RaumID, @RaumID, @GegenstandID, @AngemeldetID);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
