USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadPersonByUsername
    @Username nvarchar(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) ID, Username, Password AS PasswortHash
    FROM dbo.Person
    WHERE LOWER(Username) = LOWER(TRIM(@Username))
    ORDER BY ID;
END;
GO
