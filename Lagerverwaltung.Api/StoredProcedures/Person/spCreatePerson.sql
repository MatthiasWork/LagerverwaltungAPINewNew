USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Pflichtfeld leer, 50409 Benutzername schon vergeben / keine Rolle ohne Adminrechte vorhanden
CREATE OR ALTER PROCEDURE dbo.spCreatePerson
    @Vorname      nvarchar(250),
    @Nachname     nvarchar(250),
    @Username     nvarchar(20),
    @Email        nvarchar(100),
    @PasswortHash varchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @RolleID int;
    DECLARE @NeueID int;

    SET @Vorname  = TRIM(@Vorname);
    SET @Nachname = TRIM(@Nachname);
    SET @Username = TRIM(@Username);
    SET @Email    = TRIM(@Email);

    IF ISNULL(@Vorname, N'') = N'' OR ISNULL(@Nachname, N'') = N'' OR ISNULL(@Username, N'') = N''
       OR ISNULL(@Email, N'') = N'' OR ISNULL(@PasswortHash, '') = ''
    BEGIN
        THROW 50400, N'Bitte Vorname, Nachname, Benutzername, E-Mail-Adresse und Passwort angeben.', 1;
    END;

    SELECT TOP (1) @RolleID = ID FROM dbo.Rolle WHERE Name = N'LehrerIn' AND [Admin] = 0 ORDER BY ID;
    IF @RolleID IS NULL
    BEGIN
        SELECT TOP (1) @RolleID = ID FROM dbo.Rolle WHERE [Admin] = 0 ORDER BY ID;
    END;
    IF @RolleID IS NULL
    BEGIN
        THROW 50409, N'Es gibt keine Rolle ohne Adminrechte, daher ist keine Registrierung möglich.', 1;
    END;

    INSERT INTO dbo.Person (Vorname, Nachname, Username, Password, Email, RolleID)
    SELECT @Vorname, @Nachname, @Username, @PasswortHash, @Email, @RolleID
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Person WITH (UPDLOCK, HOLDLOCK) WHERE LOWER(Username) = LOWER(@Username));

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50409, N'Dieser Benutzername ist bereits vergeben.', 1;
    END;
    SET @NeueID = SCOPE_IDENTITY();

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
    WHERE p.ID = @NeueID;
END;
GO
