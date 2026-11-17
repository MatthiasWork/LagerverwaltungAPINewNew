USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadAllRaumart
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ID,
           Name,
           CAST(CASE WHEN LOWER(Name) LIKE N'%lager%' THEN 1 ELSE 0 END AS bit) AS IstLager
    FROM dbo.Raumart
    ORDER BY Name;
END;
GO
