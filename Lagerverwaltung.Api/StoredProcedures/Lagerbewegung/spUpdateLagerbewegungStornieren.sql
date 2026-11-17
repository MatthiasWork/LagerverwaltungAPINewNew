USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50403 nicht berechtigt, 50404 gibt es nicht oder ist eine Rückgabe, 50409 nicht mehr offen / Bewegungsart fehlt
CREATE OR ALTER PROCEDURE dbo.spUpdateLagerbewegungStornieren
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
    DECLARE @VonRaumID varchar(8);
    DECLARE @Menge int;
    DECLARE @AnfragePersonID int;
    DECLARE @FreigabePersonID int;
    DECLARE @StorniertID int;
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @Bewegungsart = ba.Name, @BestaetigtAm = l.BestaetigtAm,
               @GegenstandID = l.GegenstandID, @VonRaumID = l.VonRaumID, @Menge = l.Menge,
               @AnfragePersonID  = CASE WHEN nach.PersonID = l.PersonID THEN nach.PersonID ELSE von.PersonID END,
               @FreigabePersonID = CASE WHEN nach.PersonID = l.PersonID THEN von.PersonID ELSE nach.PersonID END
        FROM dbo.Lagerbewegung l WITH (UPDLOCK)
        JOIN dbo.Bewegungsart ba ON ba.ID = l.BewegungsartID
        JOIN dbo.Raum von        ON von.ID = l.VonRaumID
        JOIN dbo.Raum nach       ON nach.ID = l.NachRaumID
        WHERE l.ID = @ID;

        IF @Bewegungsart IS NULL OR @Bewegungsart = N'Rueckgabe'
        BEGIN
            THROW 50404, N'Diese Übergabe gibt es nicht.', 1;
        END;
        IF @IstAdmin = 0
           AND (@FreigabePersonID IS NULL OR @FreigabePersonID <> @AngemeldetID)
           AND (@AnfragePersonID IS NULL OR @AnfragePersonID <> @AngemeldetID)
        BEGIN
            THROW 50403, N'Diese Übergabe darf nur ein Admin oder eine beteiligte zuständige Person stornieren.', 1;
        END;
        IF @BestaetigtAm IS NOT NULL
        BEGIN
            SET @Meldung = CONCAT(N'Diese Übergabe wurde bereits am ', CONVERT(nvarchar(10), @BestaetigtAm, 104), N' ',
                                  CONVERT(nvarchar(5), @BestaetigtAm, 108),
                                  CASE WHEN @Bewegungsart = N'Storniert' THEN N' storniert.' ELSE N' bestätigt.' END);
            THROW 50409, @Meldung, 1;
        END;

        SELECT @StorniertID = ID FROM dbo.Bewegungsart WHERE Name = N'Storniert';
        IF @StorniertID IS NULL
        BEGIN
            THROW 50409, N'Die Bewegungsart "Storniert" fehlt in der Datenbank (wird mit Testdaten.sql angelegt).', 1;
        END;

        UPDATE dbo.Lagerbewegung
        SET BewegungsartID = @StorniertID, BestaetigtAm = GETDATE()
        WHERE ID = @ID AND BestaetigtAm IS NULL;
        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50409, N'Diese Übergabe wurde inzwischen bereits bestätigt oder storniert.', 1;
        END;

        EXEC dbo.spUpdateRaumbestandZubuchen @GegenstandID, @VonRaumID, @Menge;

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
    WHERE l.ID = @ID;
END;
GO
