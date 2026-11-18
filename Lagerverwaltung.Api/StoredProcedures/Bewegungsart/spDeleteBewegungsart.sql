USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50404 gibt es nicht, 50409 feste Bewegungsart / kommt in Lagerbewegungen vor
CREATE OR ALTER PROCEDURE dbo.spDeleteBewegungsart
    @ID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Name nvarchar(20);
    DECLARE @Anzahl int;

    SELECT @Name = Name FROM dbo.Bewegungsart WHERE ID = @ID;
    IF @Name IS NULL
    BEGIN
        THROW 50404, N'Diese Bewegungsart gibt es nicht.', 1;
    END;

    IF LOWER(@Name) IN (N'wareneingang', N'ausgabe', N'rueckgabe', N'einlagerung', N'storniert', N'korrektur')
    BEGIN
        SET @Meldung = CONCAT(N'Die Bewegungsart "', @Name, N'" wird für die Buchungen gebraucht und kann daher nicht gelöscht werden.');
        THROW 50409, @Meldung, 1;
    END;

    SELECT @Anzahl = COUNT(*) FROM dbo.Lagerbewegung WHERE BewegungsartID = @ID;
    IF @Anzahl > 0
    BEGIN
        SET @Meldung = CONCAT(N'Die Bewegungsart kommt in ', @Anzahl, N' Lagerbewegung(en) vor und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.');
        THROW 50409, @Meldung, 1;
    END;

    DELETE FROM dbo.Bewegungsart WHERE ID = @ID;
END;
GO
