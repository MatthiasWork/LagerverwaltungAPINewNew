USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Name leer / Admin fehlt, 50404 gibt es nicht, 50409 Name schon vorhanden / die letzten Admins würden ihre Adminrechte verlieren
CREATE OR ALTER PROCEDURE dbo.spUpdateRolle
    @ID    int,
    @Name  nvarchar(50),
    @Admin bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Rolle WITH (UPDLOCK, HOLDLOCK) WHERE ID = @ID)
        BEGIN
            THROW 50404, N'Diese Rolle gibt es nicht.', 1;
        END;

        SET @Name = TRIM(@Name);
        IF ISNULL(@Name, N'') = N''
        BEGIN
            THROW 50400, N'Bitte einen Namen für die Rolle eingeben.', 1;
        END;
        IF @Admin IS NULL
        BEGIN
            THROW 50400, N'Bitte angeben, ob die Rolle Adminrechte hat.', 1;
        END;

        -- Verliert die Rolle ihre Adminrechte, muss es danach noch mindestens eine Person mit Adminrechten geben.
        IF @Admin = 0
           AND EXISTS (SELECT 1 FROM dbo.Rolle WHERE ID = @ID AND [Admin] = 1)
           AND EXISTS (SELECT 1 FROM dbo.Person WHERE RolleID = @ID)
           AND NOT EXISTS (SELECT 1
                           FROM dbo.Person p WITH (UPDLOCK, HOLDLOCK)
                           JOIN dbo.Rolle r ON r.ID = p.RolleID
                           WHERE r.[Admin] = 1 AND r.ID <> @ID)
        BEGIN
            THROW 50409, N'Diese Rolle ist die einzige mit Adminrechten und zugeordneten Benutzern. Sie muss die Adminrechte behalten.', 1;
        END;

        UPDATE dbo.Rolle
        SET Name = @Name, [Admin] = @Admin
        WHERE ID = @ID
          AND NOT EXISTS (SELECT 1 FROM dbo.Rolle r WITH (UPDLOCK, HOLDLOCK)
                          WHERE LOWER(r.Name) = LOWER(@Name) AND r.ID <> @ID);

        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50409, N'Diese Rolle gibt es bereits.', 1;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
