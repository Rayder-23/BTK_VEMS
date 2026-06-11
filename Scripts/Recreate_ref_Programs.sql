USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- Backup rows that map to the new schema (preserve Uid for FK integrity)
IF OBJECT_ID('tempdb..#ref_Programs_Backup') IS NOT NULL
    DROP TABLE #ref_Programs_Backup;

SELECT
    Uid,
    ProgramCode,
    ProgramName,
    ShortName,
    DurationYears,
    IsActive,
    CreatedAt
INTO #ref_Programs_Backup
FROM dbo.ref_Programs;
GO

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
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Sections_Program')
    ALTER TABLE dbo.Sections DROP CONSTRAINT FK_Sections_Program;
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

IF OBJECT_ID(N'dbo.ref_Programs', N'U') IS NOT NULL
    DROP TABLE dbo.ref_Programs;
GO

CREATE TABLE [dbo].[ref_Programs](
    [Uid]           INT IDENTITY(1,1)   NOT NULL,
    [ProgramCode]   NVARCHAR(10)        NOT NULL,
    [ProgramName]   NVARCHAR(100)       NOT NULL,
    [ShortName]     NVARCHAR(50)        NULL,
    [DurationYears] TINYINT             NULL,
    [IsActive]      BIT                 NOT NULL DEFAULT(1),
    [CreatedAt]     DATETIME2(7)        NOT NULL DEFAULT(SYSDATETIME()),

    CONSTRAINT [PK_Programs] PRIMARY KEY CLUSTERED ([Uid] ASC),
    CONSTRAINT [UQ_Programs_ProgramCode] UNIQUE ([ProgramCode])
);
GO

SET IDENTITY_INSERT dbo.ref_Programs ON;

INSERT INTO dbo.ref_Programs (Uid, ProgramCode, ProgramName, ShortName, DurationYears, IsActive, CreatedAt)
SELECT Uid, ProgramCode, ProgramName, ShortName, DurationYears, IsActive, CreatedAt
FROM #ref_Programs_Backup;

SET IDENTITY_INSERT dbo.ref_Programs OFF;
GO

-- Restore inbound foreign keys
ALTER TABLE dbo.Classes
    ADD CONSTRAINT FK_Classes_Program FOREIGN KEY (ProgramID) REFERENCES dbo.ref_Programs (Uid);
ALTER TABLE dbo.Courses
    ADD CONSTRAINT FK_Courses_Program FOREIGN KEY (ProgramID) REFERENCES dbo.ref_Programs (Uid);
ALTER TABLE dbo.Exams
    ADD CONSTRAINT FK_Exams_Program FOREIGN KEY (ProgramID) REFERENCES dbo.ref_Programs (Uid);
ALTER TABLE dbo.FeeStructures
    ADD CONSTRAINT FK_FeeStructures_Program FOREIGN KEY (ProgramID) REFERENCES dbo.ref_Programs (Uid);
ALTER TABLE dbo.MarkingScheme
    ADD CONSTRAINT FK_MarkingScheme_Program FOREIGN KEY (ProgramID) REFERENCES dbo.ref_Programs (Uid);
ALTER TABLE dbo.Sections
    ADD CONSTRAINT FK_Sections_Program FOREIGN KEY (ProgramId) REFERENCES dbo.ref_Programs (Uid);
ALTER TABLE dbo.StudentEnrollments
    ADD CONSTRAINT FK_Enrollments_Program FOREIGN KEY (ProgramID) REFERENCES dbo.ref_Programs (Uid);
ALTER TABLE dbo.StudentGrades
    ADD CONSTRAINT FK_StudentGrades_Program FOREIGN KEY (ProgramID) REFERENCES dbo.ref_Programs (Uid);
ALTER TABLE dbo.StudentResults
    ADD CONSTRAINT FK_StudentResults_Program FOREIGN KEY (ProgramID) REFERENCES dbo.ref_Programs (Uid);
ALTER TABLE dbo.Students
    ADD CONSTRAINT FK_Students_Program FOREIGN KEY (ProgramID) REFERENCES dbo.ref_Programs (Uid);
ALTER TABLE dbo.Teachers
    ADD CONSTRAINT FK_Teachers_Program FOREIGN KEY (ProgramID) REFERENCES dbo.ref_Programs (Uid);
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS RestoredRows FROM dbo.ref_Programs;
