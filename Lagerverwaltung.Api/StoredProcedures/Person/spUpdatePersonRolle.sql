USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Rolle gibt es nicht, 50403 kein Admin, 50404 Person gibt es nicht, 50409 der letzte Admin würde seine Adminrechte verlieren
CREATE OR ALTER PROCEDURE dbo.spUpdatePersonRolle
    @AngemeldetID int,
    @ID           int,
    @RolleID      int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @WirdAdmin bit;
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    IF @IstAdmin = 0
    BEGIN
        THROW 50403, N'Nur ein Admin darf Rollen ändern.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Person WHERE ID = @ID)
        BEGIN
            THROW 50404, N'Diesen Benutzer gibt es nicht.', 1;
        END;

        SELECT @WirdAdmin = [Admin] FROM dbo.Rolle WHERE ID = @RolleID;
        IF @WirdAdmin IS NULL
        BEGIN
            THROW 50400, N'Diese Rolle gibt es nicht.', 1;
        END;

        IF @WirdAdmin = 0
           AND EXISTS (SELECT 1 FROM dbo.Person p JOIN dbo.Rolle r ON r.ID = p.RolleID
                       WHERE p.ID = @ID AND r.[Admin] = 1)
           AND NOT EXISTS (SELECT 1
                           FROM dbo.Person p WITH (UPDLOCK, HOLDLOCK)
                           JOIN dbo.Rolle r ON r.ID = p.RolleID
                           WHERE r.[Admin] = 1 AND p.ID <> @ID)
        BEGIN
            THROW 50409, N'Der letzte Administrator muss eine Admin-Rolle behalten.', 1;
        END;

        UPDATE dbo.Person SET RolleID = @RolleID WHERE ID = @ID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT p.ID,
           p.Vorname,
           p.Nachname,
           p.Username,
           p.Email,
           p.RolleID,
           r.Name    AS Rolle,
           r.[Admin] AS IstAdmin,
           (SELECT STRING_AGG(CAST(ra.ID AS nvarchar(max)), N', ') WITHIN GROUP (ORDER BY ra.ID)
            FROM dbo.Raum ra
            WHERE ra.PersonID = p.ID) AS Raeume
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @ID;
END;
GO
