USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50404 gibt es nicht, 50409 noch Bestand in einem Raum oder schon in Lagerbewegungen vorgekommen
CREATE OR ALTER PROCEDURE dbo.spDeleteGegenstand
    @ID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Anzahl int;

    IF NOT EXISTS (SELECT 1 FROM dbo.Gegenstand WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diesen Gegenstand gibt es nicht.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Raumbestand WHERE GegenstandID = @ID)
    BEGIN
        THROW 50409, N'Der Gegenstand ist noch im Bestand eines Raums und kann daher nicht gelöscht werden.', 1;
    END;

    SELECT @Anzahl = COUNT(*) FROM dbo.Lagerbewegung WHERE GegenstandID = @ID;
    IF @Anzahl > 0
    BEGIN
        SET @Meldung = CONCAT(N'Der Gegenstand kommt in ', @Anzahl, N' Lagerbewegung(en) vor und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.');
        THROW 50409, @Meldung, 1;
    END;

    DELETE FROM dbo.Gegenstand WHERE ID = @ID;
END;
GO
