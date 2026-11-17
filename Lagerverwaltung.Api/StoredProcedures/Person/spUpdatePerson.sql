USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Pflichtfeld leer, 50404 gibt es nicht, 50409 Benutzername von jemand anderem vergeben
CREATE OR ALTER PROCEDURE dbo.spUpdatePerson
    @ID       int,
    @Vorname  nvarchar(250),
    @Nachname nvarchar(250),
    @Username nvarchar(20),
    @Email    nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Person WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diesen Benutzer gibt es nicht.', 1;
    END;

    SET @Vorname  = TRIM(@Vorname);
    SET @Nachname = TRIM(@Nachname);
    SET @Username = TRIM(@Username);
    SET @Email    = TRIM(@Email);

    IF ISNULL(@Vorname, N'') = N'' OR ISNULL(@Nachname, N'') = N'' OR ISNULL(@Username, N'') = N'' OR ISNULL(@Email, N'') = N''
    BEGIN
        THROW 50400, N'Bitte Vorname, Nachname, Benutzername und E-Mail-Adresse angeben.', 1;
    END;

    UPDATE dbo.Person
    SET Vorname = @Vorname, Nachname = @Nachname, Username = @Username, Email = @Email
    WHERE ID = @ID
      AND NOT EXISTS (SELECT 1 FROM dbo.Person p WITH (UPDLOCK, HOLDLOCK)
                      WHERE LOWER(p.Username) = LOWER(@Username) AND p.ID <> @ID);

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50409, N'Dieser Benutzername ist bereits vergeben.', 1;
    END;

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
