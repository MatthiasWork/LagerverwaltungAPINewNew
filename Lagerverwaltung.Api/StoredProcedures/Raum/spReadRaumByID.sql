USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50403 kein Admin und nicht zuständig, 50404 gibt es nicht
CREATE OR ALTER PROCEDURE dbo.spReadRaumByID
    @AngemeldetID int,
    @ID           varchar(8)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    SET @ID = TRIM(@ID);
    IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diesen Raum gibt es nicht.', 1;
    END;
    IF @IstAdmin = 0 AND NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @ID AND PersonID = @AngemeldetID)
    BEGIN
        THROW 50403, N'Für diesen Raum sind Sie nicht zuständig.', 1;
    END;

    SELECT r.ID,
           r.RaumartID,
           ra.Name AS Raumart,
           CAST(CASE WHEN LOWER(ra.Name) LIKE N'%lager%' THEN 1 ELSE 0 END AS bit) AS IstLager,
           r.PersonID,
           CAST(CASE WHEN p.ID IS NULL THEN NULL ELSE CONCAT(p.Vorname, N' ', p.Nachname) END AS nvarchar(max)) AS Verantwortlich,
           (SELECT COUNT(*) FROM dbo.Raumbestand rb WHERE rb.RaumID = r.ID) AS Positionen,
           (SELECT ISNULL(SUM(rb.Menge), 0) FROM dbo.Raumbestand rb WHERE rb.RaumID = r.ID) AS Stueck
    FROM dbo.Raum r
    JOIN dbo.Raumart ra    ON ra.ID = r.RaumartID
    LEFT JOIN dbo.Person p ON p.ID = r.PersonID
    WHERE r.ID = @ID;
END;
GO
