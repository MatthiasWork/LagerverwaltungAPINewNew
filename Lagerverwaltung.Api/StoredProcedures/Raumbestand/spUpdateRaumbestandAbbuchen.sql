USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Menge kleiner 1, 50409 zu wenig Bestand / Gerät nicht im Raum
CREATE OR ALTER PROCEDURE dbo.spUpdateRaumbestandAbbuchen
    @GegenstandID int,
    @RaumID       varchar(8),
    @Menge        int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Vorhanden int;
    DECLARE @Name nvarchar(100);
    DECLARE @Seriennummer nvarchar(50);

    IF @Menge IS NULL OR @Menge < 1
    BEGIN
        THROW 50400, N'Die Menge muss mindestens 1 sein.', 1;
    END;

    DELETE FROM dbo.Raumbestand
    WHERE GegenstandID = @GegenstandID AND RaumID = @RaumID AND Menge = @Menge;
    IF @@ROWCOUNT = 1
    BEGIN
        RETURN;
    END;

    UPDATE dbo.Raumbestand
    SET Menge = Menge - @Menge
    WHERE GegenstandID = @GegenstandID AND RaumID = @RaumID AND Menge > @Menge;
    IF @@ROWCOUNT = 1
    BEGIN
        RETURN;
    END;

    SELECT @Vorhanden = Menge FROM dbo.Raumbestand WHERE GegenstandID = @GegenstandID AND RaumID = @RaumID;
    SELECT @Name = Name, @Seriennummer = Seriennummer FROM dbo.Gegenstand WHERE ID = @GegenstandID;

    IF @Seriennummer IS NOT NULL
    BEGIN
        SET @Meldung = CONCAT(N'Das Gerät "', @Name, N'" (', @Seriennummer, N') ist nicht im Raum ', @RaumID, N'.');
    END
    ELSE
    BEGIN
        SET @Meldung = CONCAT(N'Im Raum ', @RaumID, N' sind nur ', ISNULL(@Vorhanden, 0), N' Stück von "', @Name,
                              N'" vorhanden, ', @Menge, N' Stück können daher nicht gebucht werden.');
    END;
    THROW 50409, @Meldung, 1;
END;
GO
