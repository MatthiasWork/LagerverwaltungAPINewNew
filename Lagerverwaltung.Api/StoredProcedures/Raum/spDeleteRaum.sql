USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50404 gibt es nicht, 50409 HL bzw. UL / noch Bestand im Raum / kommt in Lagerbewegungen vor
CREATE OR ALTER PROCEDURE dbo.spDeleteRaum
    @ID varchar(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Anzahl int;

    SET @ID = TRIM(@ID);
    IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diesen Raum gibt es nicht.', 1;
    END;
    IF @ID IN ('HL', 'UL')
    BEGIN
        THROW 50409, N'Hauptlager (HL) und Umbuchungslager (UL) werden für die Buchungen gebraucht und können nicht gelöscht werden.', 1;
    END;
    IF EXISTS (SELECT 1 FROM dbo.Raumbestand WHERE RaumID = @ID)
    BEGIN
        THROW 50409, N'Im Raum ist noch Bestand vorhanden. Der Raum kann erst gelöscht werden, wenn er leer ist.', 1;
    END;

    SELECT @Anzahl = COUNT(*) FROM dbo.Lagerbewegung WHERE VonRaumID = @ID OR NachRaumID = @ID;
    IF @Anzahl > 0
    BEGIN
        SET @Meldung = CONCAT(N'Der Raum kommt in ', @Anzahl, N' Lagerbewegung(en) vor und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.');
        THROW 50409, @Meldung, 1;
    END;

    DELETE FROM dbo.Raum WHERE ID = @ID;
END;
GO
