USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spUpdateTokenWiderrufen
    @TokenHash binary(32)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Token
    SET WiderrufenAm = GETDATE()
    WHERE TokenHash = @TokenHash AND WiderrufenAm IS NULL;
END;
GO
