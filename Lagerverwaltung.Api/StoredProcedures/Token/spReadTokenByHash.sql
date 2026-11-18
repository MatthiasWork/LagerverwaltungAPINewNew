USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadTokenByHash
    @TokenHash binary(32)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.ID      AS PersonID,
           p.Username,
           r.Name    AS Rolle,
           r.[Admin] AS IstAdmin,
           t.LäuftAbAm
    FROM dbo.Token t
    JOIN dbo.Person p ON p.ID = t.PersonID
    JOIN dbo.Rolle r  ON r.ID = p.RolleID
    WHERE t.TokenHash = @TokenHash
      AND t.AbgelaufenAm IS NULL
      AND t.LäuftAbAm > GETDATE();
END;
GO
