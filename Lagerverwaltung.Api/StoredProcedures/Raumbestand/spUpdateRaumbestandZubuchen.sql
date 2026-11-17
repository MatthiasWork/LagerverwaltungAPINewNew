USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Menge kleiner 1
CREATE OR ALTER PROCEDURE dbo.spUpdateRaumbestandZubuchen
    @GegenstandID int,
    @RaumID       varchar(8),
    @Menge        int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Menge IS NULL OR @Menge < 1
    BEGIN
        THROW 50400, N'Die Menge muss mindestens 1 sein.', 1;
    END;

    UPDATE dbo.Raumbestand WITH (UPDLOCK, HOLDLOCK)
    SET Menge = Menge + @Menge
    WHERE GegenstandID = @GegenstandID AND RaumID = @RaumID;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.Raumbestand (GegenstandID, RaumID, Menge)
        VALUES (@GegenstandID, @RaumID, @Menge);
    END;
END;
GO
