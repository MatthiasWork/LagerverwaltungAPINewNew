USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Raumnummer leer / ungültige Zeichen / nur Punkte / Raumart bzw. Person gibt es nicht, 50409 Raumnummer schon vorhanden
CREATE OR ALTER PROCEDURE dbo.spCreateRaum
    @ID        varchar(8),
    @PersonID  int = NULL,
    @RaumartID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @ID = TRIM(@ID);
    IF ISNULL(@ID, '') = ''
    BEGIN
        THROW 50400, N'Bitte eine Raumnummer eingeben.', 1;
    END;
    IF @ID COLLATE Latin1_General_BIN LIKE '%[^A-Za-z0-9._-]%'
    BEGIN
        THROW 50400, N'Die Raumnummer darf nur Buchstaben, Ziffern, Punkte, Binde- und Unterstriche enthalten.', 1;
    END;
    IF REPLACE(@ID, '.', '') = ''
    BEGIN
        THROW 50400, N'Die Raumnummer darf nicht nur aus Punkten bestehen.', 1;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.Raumart WHERE ID = @RaumartID)
    BEGIN
        THROW 50400, N'Diese Raumart gibt es nicht.', 1;
    END;
    IF @PersonID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Person WHERE ID = @PersonID)
    BEGIN
        THROW 50400, N'Diese Person gibt es nicht.', 1;
    END;

    INSERT INTO dbo.Raum (ID, PersonID, RaumartID)
    SELECT @ID, @PersonID, @RaumartID
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Raum WITH (UPDLOCK, HOLDLOCK) WHERE LOWER(ID) = LOWER(@ID));

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50409, N'Diesen Raum gibt es bereits.', 1;
    END;
END;
GO
