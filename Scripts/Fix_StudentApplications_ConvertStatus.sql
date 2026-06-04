/* Align StudentApplications with Configurations.ApplicationStatuses (idempotent). */
SET NOCOUNT ON;

IF COL_LENGTH('dbo.StudentApplications', 'Country') IS NULL
    ALTER TABLE dbo.StudentApplications ADD Country NVARCHAR(100) NULL;
GO

UPDATE dbo.StudentApplications
SET Country = N'Pakistan'
WHERE Country IS NULL OR LTRIM(RTRIM(Country)) = N'';
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.StudentApplications')
      AND name = N'ApplicationStatus'
      AND max_length < 60
)
BEGIN
    ALTER TABLE dbo.StudentApplications ALTER COLUMN ApplicationStatus NVARCHAR(30) NOT NULL;
END;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_StudentApplications_Status')
BEGIN
    ALTER TABLE dbo.StudentApplications DROP CONSTRAINT CK_StudentApplications_Status;
END;

PRINT 'StudentApplications: Country column ready; rigid ApplicationStatus CHECK removed (use Configurations + app validation).';
