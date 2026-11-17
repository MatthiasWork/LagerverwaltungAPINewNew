USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadRaumbestandByGegenstand
    @AngemeldetID int,
    @ID           int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    SELECT rb.RaumID,
           ra.Name    AS Raumart,
           r.PersonID AS VerantwortlichID,
           CAST(CASE WHEN p.ID IS NULL THEN NULL ELSE CONCAT(p.Vorname, N' ', p.Nachname) END AS nvarchar(max)) AS Verantwortlich,
           rb.Menge
    FROM dbo.Raumbestand rb
    JOIN dbo.Raum r        ON r.ID = rb.RaumID
    JOIN dbo.Raumart ra    ON ra.ID = r.RaumartID
    LEFT JOIN dbo.Person p ON p.ID = r.PersonID
    WHERE rb.GegenstandID = @ID
      AND (@IstAdmin = 1 OR r.PersonID = @AngemeldetID)
    ORDER BY rb.RaumID;
END;
GO
