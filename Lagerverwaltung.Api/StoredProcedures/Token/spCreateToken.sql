USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Hash fehlt oder Ablaufzeit nicht in der Zukunft, 50404 Person gibt es nicht
CREATE OR ALTER PROCEDURE dbo.spCreateToken
    @PersonID  int,
    @TokenHash binary(32),
    @LäuftAbAm datetime
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TokenHash IS NULL OR @LäuftAbAm IS NULL OR @LäuftAbAm <= GETDATE()
    BEGIN
        THROW 50400, N'Das Token ist ungültig.', 1;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.Person WHERE ID = @PersonID)
    BEGIN
        THROW 50404, N'Diesen Benutzer gibt es nicht.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Aufräumen: Tokens dieser Person, die seit mehr als 7 Tagen abgelaufen oder widerrufen sind
        EXEC dbo.spDeleteTokenByPerson @PersonID, 1;

        INSERT INTO dbo.Token (PersonID, TokenHash, ErstelltAm, LäuftAbAm, AbgelaufenAm)
        VALUES (@PersonID, @TokenHash, GETDATE(), @LäuftAbAm, NULL);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
