USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Hash fehlt oder Ablaufzeit nicht in der Zukunft, 50404 Person gibt es nicht
CREATE OR ALTER PROCEDURE dbo.spCreateToken
    @PersonID   int,
    @TokenHash  binary(32),
    @LaeuftAbAm datetime
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TokenHash IS NULL OR @LaeuftAbAm IS NULL OR @LaeuftAbAm <= GETDATE()
    BEGIN
        THROW 50400, N'Das Token ist ungültig.', 1;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.Person WHERE ID = @PersonID)
    BEGIN
        THROW 50404, N'Diesen Benutzer gibt es nicht.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.Token
        WHERE PersonID = @PersonID
          AND (LaeuftAbAm < DATEADD(DAY, -7, GETDATE()) OR WiderrufenAm < DATEADD(DAY, -7, GETDATE()));

        INSERT INTO dbo.Token (PersonID, TokenHash, ErstelltAm, LaeuftAbAm, WiderrufenAm)
        VALUES (@PersonID, @TokenHash, GETDATE(), @LaeuftAbAm, NULL);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
