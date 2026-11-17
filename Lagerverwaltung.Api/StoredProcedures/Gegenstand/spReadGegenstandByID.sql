USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadGegenstandByID
    @AngemeldetID int,
    @ID           int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IstAdmin bit = 0;
    DECLARE @Raeume TABLE (RaumID varchar(8) PRIMARY KEY);

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    INSERT INTO @Raeume (RaumID)
    SELECT ID FROM dbo.Raum WHERE @IstAdmin = 1 OR PersonID = @AngemeldetID;

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
    WHERE g.ID = @ID;
END;
GO
