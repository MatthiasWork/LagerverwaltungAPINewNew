USE [Lagerverwaltung];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Fehler: 50400 Raumart bzw. Person gibt es nicht / HL bzw. UL wäre kein Lager mehr, 50404 gibt es nicht, 50409 Personenwechsel bei offenen Lagerbewegungen
CREATE OR ALTER PROCEDURE dbo.spUpdateRaum
    @ID        varchar(8),
    @PersonID  int = NULL,
    @RaumartID int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Meldung nvarchar(2048);
    DECLARE @BisherigePersonID int;
    DECLARE @Anzahl int;

    SET @ID = TRIM(@ID);
    IF NOT EXISTS (SELECT 1 FROM dbo.Raum WHERE ID = @ID)
    BEGIN
        THROW 50404, N'Diesen Raum gibt es nicht.', 1;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.Raumart WHERE ID = @RaumartID)
    BEGIN
        THROW 50400, N'Diese Raumart gibt es nicht.', 1;
    END;
    IF @PersonID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Person WHERE ID = @PersonID)
    BEGIN
        THROW 50400, N'Diese Person gibt es nicht.', 1;
    END;
    IF @ID IN ('HL', 'UL') AND NOT EXISTS (SELECT 1 FROM dbo.Raumart WHERE ID = @RaumartID AND LOWER(Name) LIKE N'%lager%')
    BEGIN
        THROW 50400, N'Hauptlager (HL) und Umbuchungslager (UL) müssen eine Lager-Raumart behalten.', 1;
    END;

    SELECT @BisherigePersonID = PersonID FROM dbo.Raum WHERE ID = @ID;
    IF @BisherigePersonID IS NOT NULL AND (@PersonID IS NULL OR @PersonID <> @BisherigePersonID)
    BEGIN
        SELECT @Anzahl = COUNT(*) FROM dbo.Lagerbewegung
        WHERE BestaetigtAm IS NULL AND (VonRaumID = @ID OR NachRaumID = @ID);
        IF @Anzahl > 0
        BEGIN
            SET @Meldung = CONCAT(N'Die zuständige Person von Raum ', @ID, N' kann nicht gewechselt werden, solange ', @Anzahl,
                                  N' offene Lagerbewegung(en) ihn betreffen.');
            THROW 50409, @Meldung, 1;
        END;
    END;

    UPDATE dbo.Raum SET PersonID = @PersonID, RaumartID = @RaumartID WHERE ID = @ID;
END;
GO
