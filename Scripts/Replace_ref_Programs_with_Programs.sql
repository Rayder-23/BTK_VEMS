USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- Drop inbound foreign keys referencing ref_Programs
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Classes_Program')
    ALTER TABLE dbo.Classes DROP CONSTRAINT FK_Classes_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Courses_Program')
    ALTER TABLE dbo.Courses DROP CONSTRAINT FK_Courses_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Exams_Program')
    ALTER TABLE dbo.Exams DROP CONSTRAINT FK_Exams_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FeeStructures_Program')
    ALTER TABLE dbo.FeeStructures DROP CONSTRAINT FK_FeeStructures_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MarkingScheme_Program')
    ALTER TABLE dbo.MarkingScheme DROP CONSTRAINT FK_MarkingScheme_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Enrollments_Program')
    ALTER TABLE dbo.StudentEnrollments DROP CONSTRAINT FK_Enrollments_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StudentGrades_Program')
    ALTER TABLE dbo.StudentGrades DROP CONSTRAINT FK_StudentGrades_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StudentResults_Program')
    ALTER TABLE dbo.StudentResults DROP CONSTRAINT FK_StudentResults_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Students_Program')
    ALTER TABLE dbo.Students DROP CONSTRAINT FK_Students_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Teachers_Program')
    ALTER TABLE dbo.Teachers DROP CONSTRAINT FK_Teachers_Program;
GO

IF OBJECT_ID('tempdb..#ref_Programs_Backup') IS NOT NULL
    DROP TABLE #ref_Programs_Backup;

SELECT
    Uid,
    ProgramCode,
    ProgramName,
    DurationYears,
    IsActive,
    CreatedAt
INTO #ref_Programs_Backup
FROM dbo.ref_Programs;
GO

IF OBJECT_ID(N'dbo.Programs', N'U') IS NOT NULL
    DROP TABLE dbo.Programs;
GO

IF OBJECT_ID(N'dbo.ref_Programs', N'U') IS NOT NULL
    DROP TABLE dbo.ref_Programs;
GO

CREATE TABLE [dbo].[Programs](
    [ProgramID]     INT IDENTITY(1,1) NOT NULL,
    [ProgramCode]   VARCHAR(20)       NOT NULL,
    [ProgramName]   VARCHAR(200)      NOT NULL,
    [DurationYears] INT               NULL,
    [IsActive]      BIT               NOT NULL CONSTRAINT [DF_Programs_IsActive] DEFAULT (1),
    [CreatedOn]     DATETIME          NOT NULL CONSTRAINT [DF_Programs_CreatedOn] DEFAULT (GETDATE()),
    CONSTRAINT [PK_Programs] PRIMARY KEY CLUSTERED ([ProgramID] ASC),
    CONSTRAINT [UQ_Programs_ProgramCode] UNIQUE NONCLUSTERED ([ProgramCode] ASC)
) ON [PRIMARY];
GO

SET IDENTITY_INSERT dbo.Programs ON;

INSERT INTO dbo.Programs (ProgramID, ProgramCode, ProgramName, DurationYears, IsActive, CreatedOn)
SELECT
    Uid,
    LEFT(ProgramCode, 20),
    LEFT(CAST(ProgramName AS VARCHAR(200)), 200),
    CAST(DurationYears AS INT),
    IsActive,
    CAST(CreatedAt AS DATETIME)
FROM #ref_Programs_Backup;

SET IDENTITY_INSERT dbo.Programs OFF;
GO

ALTER TABLE dbo.Exams
    ADD CONSTRAINT FK_Exams_Program FOREIGN KEY (ProgramID) REFERENCES dbo.Programs (ProgramID);
ALTER TABLE dbo.FeeStructures
    ADD CONSTRAINT FK_FeeStructures_Program FOREIGN KEY (ProgramID) REFERENCES dbo.Programs (ProgramID);
ALTER TABLE dbo.MarkingScheme
    ADD CONSTRAINT FK_MarkingScheme_Program FOREIGN KEY (ProgramID) REFERENCES dbo.Programs (ProgramID);
ALTER TABLE dbo.StudentEnrollments
    ADD CONSTRAINT FK_Enrollments_Program FOREIGN KEY (ProgramID) REFERENCES dbo.Programs (ProgramID);
ALTER TABLE dbo.StudentGrades
    ADD CONSTRAINT FK_StudentGrades_Program FOREIGN KEY (ProgramID) REFERENCES dbo.Programs (ProgramID);
ALTER TABLE dbo.StudentResults
    ADD CONSTRAINT FK_StudentResults_Program FOREIGN KEY (ProgramID) REFERENCES dbo.Programs (ProgramID);
ALTER TABLE dbo.Students
    ADD CONSTRAINT FK_Students_Program FOREIGN KEY (ProgramID) REFERENCES dbo.Programs (ProgramID);
ALTER TABLE dbo.Teachers
    ADD CONSTRAINT FK_Teachers_Program FOREIGN KEY (ProgramID) REFERENCES dbo.Programs (ProgramID);
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS ProgramsRows FROM dbo.Programs;
SELECT name FROM sys.tables WHERE name IN (N'Programs', N'ref_Programs') ORDER BY name;
