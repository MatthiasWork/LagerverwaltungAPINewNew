USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Name leer / Kategorie bzw. Hersteller gibt es nicht, 50409 Seriennummer schon vergeben
CREATE OR ALTER PROCEDURE dbo.spCreateGegenstand
    @ID           int OUTPUT,
    @Name         nvarchar(100),
    @Seriennummer nvarchar(50) = NULL,
    @KategorieID  int,
    @HerstellerID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

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

    INSERT INTO dbo.Gegenstand (Name, Seriennummer, KategorieID, HerstellerID)
    SELECT @Name, @Seriennummer, @KategorieID, @HerstellerID
    WHERE @Seriennummer IS NULL
       OR NOT EXISTS (SELECT 1 FROM dbo.Gegenstand WITH (UPDLOCK, HOLDLOCK) WHERE Seriennummer = @Seriennummer);

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50409, N'Diese Seriennummer ist bereits vergeben.', 1;
    END;

    SET @ID = SCOPE_IDENTITY();
END;
GO
