USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 unbekannter Status / Beginn nach Ende, 50403 keine Berechtigung, 50404 Raum gibt es nicht
CREATE OR ALTER PROCEDURE dbo.spReadAllLagerbewegung
    @AngemeldetID int,
    @RaumID       varchar(8) = NULL,
    @GegenstandID int = NULL,
    @Status       nvarchar(20) = NULL,
    @Bewegungsart nvarchar(20) = NULL,
    @Von          datetime = NULL,
    @Bis          datetime = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    SET @RaumID = NULLIF(TRIM(@RaumID), '');
    SET @Status = NULLIF(TRIM(@Status), N'');
    SET @Bewegungsart = NULLIF(TRIM(@Bewegungsart), N'');

    IF @Status IS NOT NULL AND @Status NOT IN (N'Offen', N'Abgeschlossen', N'Storniert')
    BEGIN
        THROW 50400, N'Der Status muss "Offen", "Abgeschlossen" oder "Storniert" sein.', 1;
    END;
    IF @Von IS NOT NULL AND @Bis IS NOT NULL AND @Von > @Bis
    BEGIN
        THROW 50400, N'Der Beginn des Zeitraums liegt nach dem Ende.', 1;
    END;

    IF @RaumID IS NULL
    BEGIN
        IF @IstAdmin = 0
        BEGIN
            THROW 50403, N'Die vollständige Historie sieht nur ein Admin.', 1;
        END;
    END
    ELSE
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
    WHERE (@RaumID IS NULL OR l.VonRaumID = @RaumID OR l.NachRaumID = @RaumID)
      AND (@GegenstandID IS NULL OR l.GegenstandID = @GegenstandID)
      AND (@Status IS NULL
           OR (@Status = N'Offen' AND l.BestaetigtAm IS NULL)
           OR (@Status = N'Storniert' AND l.BestaetigtAm IS NOT NULL AND ba.Name = N'Storniert')
           OR (@Status = N'Abgeschlossen' AND l.BestaetigtAm IS NOT NULL AND ba.Name <> N'Storniert'))
      AND (@Bewegungsart IS NULL OR ba.Name = @Bewegungsart)
      AND (@Von IS NULL OR l.ErstelltAm >= @Von)
      AND (@Bis IS NULL OR l.ErstelltAm <= @Bis)
    ORDER BY l.ErstelltAm DESC, l.ID DESC;
END;
GO
