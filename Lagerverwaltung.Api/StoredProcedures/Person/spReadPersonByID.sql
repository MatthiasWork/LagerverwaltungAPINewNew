USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadPersonByID
    @ID int
AS
BEGIN
    SET NOCOUNT ON;

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
    WHERE p.ID = @ID;
END;
GO
