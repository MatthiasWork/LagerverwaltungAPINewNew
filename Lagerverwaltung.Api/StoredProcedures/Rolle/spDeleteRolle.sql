USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50404 gibt es nicht, 50409 noch Personen zugeordnet
CREATE OR ALTER PROCEDURE dbo.spDeleteRolle
    @ID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Anzahl int;

    IF NOT EXISTS (SELECT 1 FROM dbo.Rolle WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diese Rolle gibt es nicht.', 1;
    END;

    SELECT @Anzahl = COUNT(*) FROM dbo.Person WHERE RolleID = @ID;
    IF @Anzahl > 0
    BEGIN
        SET @Meldung = CONCAT(N'Der Rolle sind noch ', @Anzahl, N' Person(en) zugeordnet. Diese müssen zuerst eine andere Rolle bekommen.');
        THROW 50409, @Meldung, 1;
    END;

    DELETE FROM dbo.Rolle WHERE ID = @ID;
END;
GO
