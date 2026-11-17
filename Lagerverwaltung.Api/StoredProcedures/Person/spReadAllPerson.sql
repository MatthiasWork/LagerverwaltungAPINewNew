USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadAllPerson
    @Suche   nvarchar(100) = NULL,
    @RolleID int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Suche = NULLIF(TRIM(@Suche), N'');

    SELECT p.ID,
           p.Vorname,
           p.Nachname,
           p.Username,
           p.Email,
           p.RolleID,
           r.Name    AS Rolle,
           r.[Admin] AS IstAdmin,
           (SELECT STRING_AGG(CAST(ra.ID AS nvarchar(max)), N', ') WITHIN GROUP (ORDER BY ra.ID)
            FROM dbo.Raum ra
            WHERE ra.PersonID = p.ID) AS Raeume
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE (@Suche IS NULL
           OR CHARINDEX(@Suche, p.Vorname) > 0
           OR CHARINDEX(@Suche, p.Nachname) > 0
           OR CHARINDEX(@Suche, p.Username) > 0
           OR CHARINDEX(@Suche, p.Email) > 0)
      AND (@RolleID IS NULL OR p.RolleID = @RolleID)
    ORDER BY p.Nachname, p.Vorname;
END;
GO
