USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- Drop inbound foreign keys referencing Classes
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassCourses_Class')
    ALTER TABLE dbo.ClassCourses DROP CONSTRAINT FK_ClassCourses_Class;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassStudents_Class')
    ALTER TABLE dbo.ClassStudents DROP CONSTRAINT FK_ClassStudents_Class;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FeeStructures_Class')
    ALTER TABLE dbo.FeeStructures DROP CONSTRAINT FK_FeeStructures_Class;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Enrollments_Class')
    ALTER TABLE dbo.StudentEnrollments DROP CONSTRAINT FK_Enrollments_Class;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCA_Class')
    ALTER TABLE dbo.TeacherCourseAssignments DROP CONSTRAINT FK_TCA_Class;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Classes_Program')
    ALTER TABLE dbo.Classes DROP CONSTRAINT FK_Classes_Program;
GO

IF OBJECT_ID('tempdb..#Classes_Backup') IS NOT NULL
    DROP TABLE #Classes_Backup;

SELECT
    Uid,
    ClassCode,
    ClassName,
    IsActive
INTO #Classes_Backup
FROM dbo.Classes;
GO

IF OBJECT_ID(N'dbo.Classes', N'U') IS NOT NULL
    DROP TABLE dbo.Classes;
GO

CREATE TABLE [dbo].[Classes](
    [ClassID]   INT IDENTITY(1,1) NOT NULL,
    [ClassCode] VARCHAR(20)       NULL,
    [ClassName] VARCHAR(100)      NOT NULL,
    [SortOrder] INT               NULL,
    [IsActive]  BIT               NOT NULL CONSTRAINT [DF_Classes_IsActive] DEFAULT (1),
    CONSTRAINT [PK_Classes] PRIMARY KEY CLUSTERED ([ClassID] ASC)
) ON [PRIMARY];
GO

SET IDENTITY_INSERT dbo.Classes ON;

INSERT INTO dbo.Classes (ClassID, ClassCode, ClassName, SortOrder, IsActive)
SELECT
    Uid,
    LEFT(ClassCode, 20),
    LEFT(CAST(ClassName AS VARCHAR(100)), 100),
    Uid,
    IsActive
FROM #Classes_Backup;

SET IDENTITY_INSERT dbo.Classes OFF;
GO

ALTER TABLE dbo.ClassCourses
    ADD CONSTRAINT FK_ClassCourses_Class FOREIGN KEY (ClassID) REFERENCES dbo.Classes (ClassID);
ALTER TABLE dbo.ClassStudents
    ADD CONSTRAINT FK_ClassStudents_Class FOREIGN KEY (ClassID) REFERENCES dbo.Classes (ClassID);
ALTER TABLE dbo.FeeStructures
    ADD CONSTRAINT FK_FeeStructures_Class FOREIGN KEY (ClassID) REFERENCES dbo.Classes (ClassID);
ALTER TABLE dbo.StudentEnrollments
    ADD CONSTRAINT FK_Enrollments_Class FOREIGN KEY (ClassID) REFERENCES dbo.Classes (ClassID);
ALTER TABLE dbo.TeacherCourseAssignments
    ADD CONSTRAINT FK_TCA_Class FOREIGN KEY (ClassID) REFERENCES dbo.Classes (ClassID);
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS ClassesRows FROM dbo.Classes;
