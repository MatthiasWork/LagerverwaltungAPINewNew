USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Passwort-Hash leer, 50404 gibt es nicht
CREATE OR ALTER PROCEDURE dbo.spUpdatePersonPasswort
    @ID                int,
    @PasswortHash      varchar(max),
    @BehaltenTokenHash binary(32) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF ISNULL(@PasswortHash, '') = ''
    BEGIN
        THROW 50400, N'Bitte ein neues Passwort angeben.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.Person SET Password = @PasswortHash WHERE ID = @ID;
        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50404, N'Diesen Benutzer gibt es nicht.', 1;
        END;

        UPDATE dbo.Token
        SET WiderrufenAm = GETDATE()
        WHERE PersonID = @ID
          AND WiderrufenAm IS NULL
          AND (@BehaltenTokenHash IS NULL OR TokenHash <> @BehaltenTokenHash);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
