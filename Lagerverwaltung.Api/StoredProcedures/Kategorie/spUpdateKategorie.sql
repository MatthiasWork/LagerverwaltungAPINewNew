USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Name leer, 50404 gibt es nicht, 50409 Name schon vorhanden
CREATE OR ALTER PROCEDURE dbo.spUpdateKategorie
    @ID   int,
    @Name nvarchar(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Kategorie WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diese Kategorie gibt es nicht.', 1;
    END;

    SET @Name = TRIM(@Name);
    IF ISNULL(@Name, N'') = N''
    BEGIN
        THROW 50400, N'Bitte einen Namen für die Kategorie eingeben.', 1;
    END;

    UPDATE dbo.Kategorie
    SET Name = @Name
    WHERE ID = @ID
      AND NOT EXISTS (SELECT 1 FROM dbo.Kategorie k WITH (UPDLOCK, HOLDLOCK)
                      WHERE LOWER(k.Name) = LOWER(@Name) AND k.ID <> @ID);

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50409, N'Diese Kategorie gibt es bereits.', 1;
    END;
END;
GO
