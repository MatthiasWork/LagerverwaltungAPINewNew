USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50404 gibt es nicht, 50409 noch Räume zugeordnet
CREATE OR ALTER PROCEDURE dbo.spDeleteRaumart
    @ID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Raeume nvarchar(max);

    IF NOT EXISTS (SELECT 1 FROM dbo.Raumart WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diese Raumart gibt es nicht.', 1;
    END;

    SELECT @Raeume = STRING_AGG(CAST(ID AS nvarchar(max)), N', ') WITHIN GROUP (ORDER BY ID)
    FROM dbo.Raum
    WHERE RaumartID = @ID;
    IF @Raeume IS NOT NULL
    BEGIN
        SET @Meldung = CONCAT(N'Der Raumart sind noch Räume zugeordnet (', @Raeume, N'). Diese müssen zuerst eine andere Raumart bekommen oder gelöscht werden.');
        THROW 50409, @Meldung, 1;
    END;

    DELETE FROM dbo.Raumart WHERE ID = @ID;
END;
GO
