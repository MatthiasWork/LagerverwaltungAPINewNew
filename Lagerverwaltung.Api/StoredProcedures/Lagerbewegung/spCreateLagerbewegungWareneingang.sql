USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Raum ist kein Lager / Menge kleiner 1 / Gerät mit Menge ungleich 1, 50403 kein Admin, 50404 Gegenstand bzw. Raum gibt es nicht, 50409 Gerät schon vorhanden oder unterwegs / Höchstbestand überschritten / Bewegungsart fehlt
CREATE OR ALTER PROCEDURE dbo.spCreateLagerbewegungWareneingang
    @AngemeldetID int,
    @GegenstandID int,
    @Menge        int,
    @RaumID       varchar(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @HoechstBestand bigint = 100000;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Name nvarchar(100);
    DECLARE @Seriennummer nvarchar(50);
    DECLARE @Bisher bigint;
    DECLARE @BewegungsartID int;
    DECLARE @NeueID int;
    DECLARE @Jetzt datetime = GETDATE();
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    IF @IstAdmin = 0
    BEGIN
        THROW 50403, N'Nur ein Admin darf einen Wareneingang buchen.', 1;
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

        IF NOT EXISTS (SELECT 1 FROM dbo.Raum r JOIN dbo.Raumart ra ON ra.ID = r.RaumartID
                       WHERE r.ID = @RaumID AND LOWER(ra.Name) LIKE N'%lager%')
        BEGIN
            THROW 50400, N'Ein Wareneingang ist nur in ein Lager möglich.', 1;
        END;
        IF @Menge IS NULL OR @Menge < 1
        BEGIN
            THROW 50400, N'Die Menge muss mindestens 1 sein.', 1;
        END;
        IF @Seriennummer IS NOT NULL AND @Menge <> 1
        BEGIN
            THROW 50400, N'Ein Gerät mit Seriennummer kann nur mit der Menge 1 gebucht werden.', 1;
        END;

        IF @Seriennummer IS NOT NULL
           AND (EXISTS (SELECT 1 FROM dbo.Raumbestand WHERE GegenstandID = @GegenstandID)
                OR EXISTS (SELECT 1 FROM dbo.Lagerbewegung WHERE GegenstandID = @GegenstandID AND BestaetigtAm IS NULL))
        BEGIN
            SET @Meldung = CONCAT(N'Das Gerät "', @Name, N'" (', @Seriennummer, N') ist bereits vorhanden oder unterwegs. Ein Gerät mit Seriennummer gibt es nur einmal.');
            THROW 50409, @Meldung, 1;
        END;

        SET @Bisher = ISNULL((SELECT SUM(CAST(Menge AS bigint)) FROM dbo.Raumbestand WHERE GegenstandID = @GegenstandID), 0)
                    + ISNULL((SELECT SUM(CAST(Menge AS bigint)) FROM dbo.Lagerbewegung WHERE GegenstandID = @GegenstandID AND BestaetigtAm IS NULL), 0);
        IF @Bisher + @Menge > @HoechstBestand
        BEGIN
            SET @Meldung = CONCAT(N'Von "', @Name, N'" kann es insgesamt höchstens ', @HoechstBestand, N' Stück geben, jetzt sind es ', @Bisher, N'.');
            THROW 50409, @Meldung, 1;
        END;

        SELECT @BewegungsartID = ID FROM dbo.Bewegungsart WHERE Name = N'Wareneingang';
        IF @BewegungsartID IS NULL
        BEGIN
            THROW 50409, N'Die Bewegungsart "Wareneingang" fehlt in der Datenbank (wird mit Testdaten.sql angelegt).', 1;
        END;

        INSERT INTO dbo.Lagerbewegung (Menge, ErstelltAm, BestaetigtAm, BewegungsartID, VonRaumID, NachRaumID, GegenstandID, PersonID)
        VALUES (@Menge, @Jetzt, @Jetzt, @BewegungsartID, @RaumID, @RaumID, @GegenstandID, @AngemeldetID);
        SET @NeueID = SCOPE_IDENTITY();

        EXEC dbo.spUpdateRaumbestandZubuchen @GegenstandID, @RaumID, @Menge;

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
