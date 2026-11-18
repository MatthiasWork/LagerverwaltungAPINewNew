USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Name leer, 50409 Name schon vorhanden
CREATE OR ALTER PROCEDURE dbo.spCreateRaumart
    @ID   int OUTPUT,
    @Name nvarchar(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Name = TRIM(@Name);
    IF ISNULL(@Name, N'') = N''
    BEGIN
        THROW 50400, N'Bitte einen Namen für die Raumart eingeben.', 1;
    END;

    INSERT INTO dbo.Raumart (Name)
    SELECT @Name
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Raumart WITH (UPDLOCK, HOLDLOCK) WHERE LOWER(Name) = LOWER(@Name));

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50409, N'Diese Raumart gibt es bereits.', 1;
    END;

    SET @ID = SCOPE_IDENTITY();
END;
GO
