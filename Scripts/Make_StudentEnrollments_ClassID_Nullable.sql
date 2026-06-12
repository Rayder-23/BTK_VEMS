/* Allow student enrollment without a class (e.g. when no classes exist yet for a program). Idempotent. */
SET NOCOUNT ON;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.StudentEnrollments')
      AND name = N'ClassID'
      AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.StudentEnrollments ALTER COLUMN ClassID INT NULL;
    PRINT 'dbo.StudentEnrollments.ClassID is now nullable.';
END
ELSE
BEGIN
    PRINT 'dbo.StudentEnrollments.ClassID is already nullable (no change).';
END
GO
