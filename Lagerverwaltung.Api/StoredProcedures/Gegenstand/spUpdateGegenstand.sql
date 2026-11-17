USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Name leer / Kategorie bzw. Hersteller gibt es nicht, 50404 gibt es nicht, 50409 Seriennummer schon vergeben / Seriennummer bei mehr als 1 Stück
CREATE OR ALTER PROCEDURE dbo.spUpdateGegenstand
    @ID           int,
    @Name         nvarchar(100),
    @Seriennummer nvarchar(50) = NULL,
    @KategorieID  int,
    @HerstellerID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Gesamt bigint;

    IF NOT EXISTS (SELECT 1 FROM dbo.Gegenstand WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diesen Gegenstand gibt es nicht.', 1;
    END;

    SET @Name = TRIM(@Name);
    SET @Seriennummer = NULLIF(TRIM(@Seriennummer), N'');

    IF ISNULL(@Name, N'') = N''
    BEGIN
        THROW 50400, N'Bitte eine Bezeichnung eingeben.', 1;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.Kategorie WHERE ID = @KategorieID)
    BEGIN
        THROW 50400, N'Diese Kategorie gibt es nicht.', 1;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.Hersteller WHERE ID = @HerstellerID)
    BEGIN
        THROW 50400, N'Diesen Hersteller gibt es nicht.', 1;
    END;

    IF @Seriennummer IS NOT NULL
    BEGIN
        SELECT @Gesamt = ISNULL((SELECT SUM(CAST(Menge AS bigint)) FROM dbo.Raumbestand WHERE GegenstandID = @ID), 0)
                       + ISNULL((SELECT SUM(CAST(Menge AS bigint)) FROM dbo.Lagerbewegung WHERE GegenstandID = @ID AND BestaetigtAm IS NULL), 0);
        IF @Gesamt > 1
        BEGIN
            SET @Meldung = CONCAT(N'Von diesem Artikel gibt es ', @Gesamt, N' Stück. Eine Seriennummer ist nur bei einem einzelnen Gerät möglich.');
            THROW 50409, @Meldung, 1;
        END;
    END;

    UPDATE dbo.Gegenstand
    SET Name = @Name, Seriennummer = @Seriennummer, KategorieID = @KategorieID, HerstellerID = @HerstellerID
    WHERE ID = @ID
      AND (@Seriennummer IS NULL
           OR NOT EXISTS (SELECT 1 FROM dbo.Gegenstand g WITH (UPDLOCK, HOLDLOCK)
                          WHERE g.Seriennummer = @Seriennummer AND g.ID <> @ID));

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50409, N'Diese Seriennummer ist bereits vergeben.', 1;
    END;
END;
GO
