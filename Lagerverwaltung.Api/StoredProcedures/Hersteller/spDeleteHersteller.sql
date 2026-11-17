USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50404 gibt es nicht, 50409 noch Gegenstände zugeordnet
CREATE OR ALTER PROCEDURE dbo.spDeleteHersteller
    @ID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Anzahl int;

    IF NOT EXISTS (SELECT 1 FROM dbo.Hersteller WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diesen Hersteller gibt es nicht.', 1;
    END;

    SELECT @Anzahl = COUNT(*) FROM dbo.Gegenstand WHERE HerstellerID = @ID;
    IF @Anzahl > 0
    BEGIN
        SET @Meldung = CONCAT(N'Dem Hersteller sind noch ', @Anzahl, N' Gegenstand/Gegenstände zugeordnet. Diese müssen zuerst einem anderen Hersteller zugeordnet oder gelöscht werden.');
        THROW 50409, @Meldung, 1;
    END;

    DELETE FROM dbo.Hersteller WHERE ID = @ID;
END;
GO
