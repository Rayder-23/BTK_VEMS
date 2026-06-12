/* Link challans to admission applications when no student record exists yet (idempotent). */
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.Challans', 'ApplicationUid') IS NULL
    ALTER TABLE dbo.Challans ADD ApplicationUid INT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Challans_Students')
    ALTER TABLE dbo.Challans DROP CONSTRAINT FK_Challans_Students;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Challans')
      AND name = N'StudentID'
      AND is_nullable = 0)
    ALTER TABLE dbo.Challans ALTER COLUMN StudentID INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Challans_Students')
    ALTER TABLE dbo.Challans WITH CHECK
        ADD CONSTRAINT FK_Challans_Students
        FOREIGN KEY (StudentID) REFERENCES dbo.Students (Uid);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Challans_StudentApplications')
    ALTER TABLE dbo.Challans WITH CHECK
        ADD CONSTRAINT FK_Challans_StudentApplications
        FOREIGN KEY (ApplicationUid) REFERENCES dbo.StudentApplications (Uid);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Challans_StudentOrApplication')
    ALTER TABLE dbo.Challans WITH CHECK
        ADD CONSTRAINT CK_Challans_StudentOrApplication
        CHECK (StudentID IS NOT NULL OR ApplicationUid IS NOT NULL);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Challans_ApplicationUid'
      AND object_id = OBJECT_ID(N'dbo.Challans'))
    CREATE NONCLUSTERED INDEX IX_Challans_ApplicationUid
        ON dbo.Challans (ApplicationUid)
        WHERE ApplicationUid IS NOT NULL;
GO

PRINT 'Challans application linkage migration complete.';
GO
