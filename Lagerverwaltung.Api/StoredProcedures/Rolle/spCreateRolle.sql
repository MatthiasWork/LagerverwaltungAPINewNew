USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Name leer / Admin fehlt, 50409 Name schon vorhanden
CREATE OR ALTER PROCEDURE dbo.spCreateRolle
    @ID    int OUTPUT,
    @Name  nvarchar(50),
    @Admin bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Name = TRIM(@Name);
    IF ISNULL(@Name, N'') = N''
    BEGIN
        THROW 50400, N'Bitte einen Namen für die Rolle eingeben.', 1;
    END;
    IF @Admin IS NULL
    BEGIN
        THROW 50400, N'Bitte angeben, ob die Rolle Adminrechte hat.', 1;
    END;

    INSERT INTO dbo.Rolle (Name, [Admin])
    SELECT @Name, @Admin
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Rolle WITH (UPDLOCK, HOLDLOCK) WHERE LOWER(Name) = LOWER(@Name));

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50409, N'Diese Rolle gibt es bereits.', 1;
    END;

    SET @ID = SCOPE_IDENTITY();
END;
GO
