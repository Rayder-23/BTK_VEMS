/*
    Add Frequency, ApplicableMonth, and ApplicableYear to FeeStructureDetails
    with billing-period check constraints.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.FeeStructureDetails')
      AND name = N'Frequency'
)
BEGIN
    ALTER TABLE dbo.FeeStructureDetails ADD Frequency VARCHAR(20) NULL;
END;
GO

UPDATE dbo.FeeStructureDetails
SET Frequency = N'Monthly'
WHERE Frequency IS NULL;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.FeeStructureDetails')
      AND name = N'Frequency'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.FeeStructureDetails ALTER COLUMN Frequency VARCHAR(20) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.FeeStructureDetails')
      AND name = N'ApplicableMonth'
)
BEGIN
    ALTER TABLE dbo.FeeStructureDetails ADD ApplicableMonth TINYINT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.FeeStructureDetails')
      AND name = N'ApplicableYear'
)
BEGIN
    ALTER TABLE dbo.FeeStructureDetails ADD ApplicableYear SMALLINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Frequency')
BEGIN
    ALTER TABLE dbo.FeeStructureDetails
        ADD CONSTRAINT CK_Frequency
        CHECK (Frequency IN (N'Monthly', N'OneTime'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OneTimeMonthYear')
BEGIN
    ALTER TABLE dbo.FeeStructureDetails
        ADD CONSTRAINT CK_OneTimeMonthYear
        CHECK
        (
            (Frequency = N'Monthly'
             AND ApplicableMonth IS NULL
             AND ApplicableYear IS NULL)

            OR

            (Frequency = N'OneTime'
             AND ApplicableMonth BETWEEN 1 AND 12
             AND ApplicableYear IS NOT NULL)
        );
END;
GO
