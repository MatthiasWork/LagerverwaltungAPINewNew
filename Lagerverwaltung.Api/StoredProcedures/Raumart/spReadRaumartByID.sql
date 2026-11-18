USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spReadRaumartByID
    @ID int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ID,
           Name,
           CAST(CASE WHEN LOWER(Name) LIKE N'%lager%' THEN 1 ELSE 0 END AS bit) AS IstLager
    FROM dbo.Raumart
    WHERE ID = @ID;
END;
GO
