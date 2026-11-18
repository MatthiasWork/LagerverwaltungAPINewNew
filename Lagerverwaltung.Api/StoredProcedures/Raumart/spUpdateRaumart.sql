USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Name leer, 50404 gibt es nicht, 50409 Name schon vorhanden / HL bzw. UL wäre kein Lager mehr
CREATE OR ALTER PROCEDURE dbo.spUpdateRaumart
    @ID   int,
    @Name nvarchar(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Raumart WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diese Raumart gibt es nicht.', 1;
    END;

    SET @Name = TRIM(@Name);
    IF ISNULL(@Name, N'') = N''
    BEGIN
        THROW 50400, N'Bitte einen Namen für die Raumart eingeben.', 1;
    END;

    -- Ein Lager wird am Wortteil "lager" im Namen der Raumart erkannt, und HL und UL müssen Lager bleiben.
    IF LOWER(@Name) NOT LIKE N'%lager%'
       AND EXISTS (SELECT 1 FROM dbo.Raum WHERE RaumartID = @ID AND ID IN ('HL', 'UL'))
    BEGIN
        THROW 50409, N'Hauptlager (HL) bzw. Umbuchungslager (UL) haben diese Raumart. Ihr Name sollte daher weiterhin "lager" enthalten.', 1;
    END;

    UPDATE dbo.Raumart
    SET Name = @Name
    WHERE ID = @ID
      AND NOT EXISTS (SELECT 1 FROM dbo.Raumart r WITH (UPDLOCK, HOLDLOCK)
                      WHERE LOWER(r.Name) = LOWER(@Name) AND r.ID <> @ID);

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50409, N'Diese Raumart gibt es bereits.', 1;
    END;
END;
GO
