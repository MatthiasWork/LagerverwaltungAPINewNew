USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spDeleteTokenByPerson
    @PersonID       int,
    @NurAbgelaufene bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Token
    WHERE PersonID = @PersonID
      AND (@NurAbgelaufene = 0
           OR LäuftAbAm < DATEADD(DAY, -7, GETDATE())
           OR AbgelaufenAm < DATEADD(DAY, -7, GETDATE()));
END;
GO
