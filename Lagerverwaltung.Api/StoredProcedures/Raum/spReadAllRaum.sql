USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadAllRaum
    @Suche     nvarchar(20) = NULL,
    @RaumartID int = NULL,
    @PersonID  int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Suche = NULLIF(TRIM(@Suche), N'');

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
    WHERE (@Suche IS NULL
           OR CHARINDEX(@Suche, r.ID) > 0
           OR CHARINDEX(@Suche, ra.Name) > 0
           OR CHARINDEX(@Suche, CONCAT(p.Vorname, N' ', p.Nachname)) > 0
           OR CHARINDEX(@Suche, p.Username) > 0)
      AND (@RaumartID IS NULL OR r.RaumartID = @RaumartID)
      AND (@PersonID IS NULL OR r.PersonID = @PersonID)
    ORDER BY r.ID;
END;
GO
