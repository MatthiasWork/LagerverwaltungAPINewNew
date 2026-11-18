USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50403 weder Admin noch das eigene Konto, 50404 gibt es nicht, 50409 hat Lagerbewegungen / ist für Räume zuständig / ist der letzte Admin
CREATE OR ALTER PROCEDURE dbo.spDeletePerson
    @AngemeldetID int,
    @ID           int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @Raeume nvarchar(max);
    DECLARE @Anzahl int;
    DECLARE @IstAdmin bit = 0;

    SELECT @IstAdmin = r.[Admin]
    FROM dbo.Person p
    JOIN dbo.Rolle r ON r.ID = p.RolleID
    WHERE p.ID = @AngemeldetID;

    IF @IstAdmin = 0 AND (@AngemeldetID IS NULL OR @AngemeldetID <> @ID)
    BEGIN
        THROW 50403, N'Nur ein Admin darf fremde Benutzerkonten löschen.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Person WITH (UPDLOCK, HOLDLOCK) WHERE ID = @ID)
        BEGIN
            THROW 50404, N'Diesen Benutzer gibt es nicht.', 1;
        END;

        SELECT @Anzahl = COUNT(*) FROM dbo.Lagerbewegung WHERE PersonID = @ID;
        IF @Anzahl > 0
        BEGIN
            SET @Meldung = CONCAT(N'Der Benutzer hat ', @Anzahl, N' Lagerbewegung(en) erfasst und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.');
            THROW 50409, @Meldung, 1;
        END;

        SELECT @Raeume = STRING_AGG(CAST(ID AS nvarchar(max)), N', ') WITHIN GROUP (ORDER BY ID)
        FROM dbo.Raum
        WHERE PersonID = @ID;
        IF @Raeume IS NOT NULL
        BEGIN
            SET @Meldung = CONCAT(N'Der Benutzer ist für die Räume ', @Raeume, N' zuständig. Bitte die Räume zuerst einer anderen Person zuweisen.');
            THROW 50409, @Meldung, 1;
        END;

        IF EXISTS (SELECT 1 FROM dbo.Person p JOIN dbo.Rolle r ON r.ID = p.RolleID
                   WHERE p.ID = @ID AND r.[Admin] = 1)
           AND NOT EXISTS (SELECT 1
                           FROM dbo.Person p WITH (UPDLOCK, HOLDLOCK)
                           JOIN dbo.Rolle r ON r.ID = p.RolleID
                           WHERE r.[Admin] = 1 AND p.ID <> @ID)
        BEGIN
            THROW 50409, N'Der letzte Administrator kann nicht gelöscht werden.', 1;
        END;

        EXEC dbo.spDeleteTokenByPerson @ID, 0;
        DELETE FROM dbo.Person WHERE ID = @ID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
