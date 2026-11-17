USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadLagerbewegungOffeneUebergaben
    @AngemeldetID int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

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
    WHERE l.BestaetigtAm IS NULL
      AND ba.Name <> N'Rueckgabe'
      AND (@IstAdmin = 1
           OR EXISTS (SELECT 1 FROM dbo.Raum r
                      WHERE r.PersonID = @AngemeldetID AND (r.ID = l.VonRaumID OR r.ID = l.NachRaumID)))
    ORDER BY l.ErstelltAm, l.ID;
END;
GO
