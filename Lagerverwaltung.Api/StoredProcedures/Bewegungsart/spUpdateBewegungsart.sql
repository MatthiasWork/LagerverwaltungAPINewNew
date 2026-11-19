USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Name leer, 50404 gibt es nicht, 50409 Name schon vorhanden / feste Bewegungsart würde umbenannt
CREATE OR ALTER PROCEDURE dbo.spUpdateBewegungsart
    @ID   int,
    @Name nvarchar(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @BisherigerName nvarchar(20);

    SELECT @BisherigerName = Name FROM dbo.Bewegungsart WHERE ID = @ID;
    IF @BisherigerName IS NULL
    BEGIN
        THROW 50404, N'Diese Bewegungsart gibt es nicht.', 1;
    END;

    SET @Name = TRIM(@Name);
    IF ISNULL(@Name, N'') = N''
    BEGIN
        THROW 50400, N'Bitte einen Namen für die Bewegungsart eingeben.', 1;
    END;

    IF LOWER(@BisherigerName) IN (N'wareneingang', N'ausgabe', N'rückgabe', N'einlagerung', N'storniert', N'korrektur')
       AND @Name <> @BisherigerName
    BEGIN
        SET @Meldung = CONCAT(N'Die Bewegungsart "', @BisherigerName, N'" wird von den Buchungen über ihren Namen gesucht und kann daher nicht umbenannt werden.');
        THROW 50409, @Meldung, 1;
    END;

    UPDATE dbo.Bewegungsart
    SET Name = @Name
    WHERE ID = @ID
      AND NOT EXISTS (SELECT 1 FROM dbo.Bewegungsart b WITH (UPDLOCK, HOLDLOCK)
                      WHERE LOWER(b.Name) = LOWER(@Name) AND b.ID <> @ID);

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50409, N'Diese Bewegungsart gibt es bereits.', 1;
    END;
END;
GO
