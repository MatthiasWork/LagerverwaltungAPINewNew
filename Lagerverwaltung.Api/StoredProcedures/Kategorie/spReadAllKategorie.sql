USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadAllKategorie
    @Name nvarchar(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Name = NULLIF(TRIM(@Name), N'');

    SELECT ID, Name
    FROM dbo.Kategorie
    WHERE @Name IS NULL OR CHARINDEX(@Name, Name) > 0
    ORDER BY Name;
END;
GO
