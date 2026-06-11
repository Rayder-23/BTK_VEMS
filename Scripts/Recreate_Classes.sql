USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#Classes_Backup') IS NOT NULL
    DROP TABLE #Classes_Backup;

SELECT
    Uid,
    ProgramID,
    ClassCode,
    ClassName,
    SemesterNo,
    Semester,
    AcademicYear,
    Section,
    Shift,
    RoomNo,
    MaxStrength,
    IsActive,
    CreatedAt
INTO #Classes_Backup
FROM dbo.Classes;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassCourses_Class')
    ALTER TABLE dbo.ClassCourses DROP CONSTRAINT FK_ClassCourses_Class;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassStudents_Class')
    ALTER TABLE dbo.ClassStudents DROP CONSTRAINT FK_ClassStudents_Class;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCA_Class')
    ALTER TABLE dbo.TeacherCourseAssignments DROP CONSTRAINT FK_TCA_Class;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Classes_Program')
    ALTER TABLE dbo.Classes DROP CONSTRAINT FK_Classes_Program;
GO

IF OBJECT_ID(N'dbo.Classes', N'U') IS NOT NULL
    DROP TABLE dbo.Classes;
GO

CREATE TABLE [dbo].[Classes](
    [Uid]          INT IDENTITY(1,1)  NOT NULL,
    [ProgramID]    INT                NOT NULL,
    [ClassCode]    VARCHAR(30)        NOT NULL,
    [ClassName]    NVARCHAR(100)      NOT NULL,
    [SemesterNo]   TINYINT            NOT NULL,
    [Semester]     NVARCHAR(20)       NOT NULL,
    [AcademicYear] SMALLINT           NOT NULL,
    [Section]      NVARCHAR(10)       NULL,
    [Shift]        NVARCHAR(20)       NULL,
    [RoomNo]       NVARCHAR(30)       NULL,
    [MaxStrength]  SMALLINT           NOT NULL DEFAULT(40),
    [IsActive]     BIT                NOT NULL DEFAULT(1),
    [CreatedAt]    DATETIME2(7)       NOT NULL DEFAULT(SYSDATETIME()),

    CONSTRAINT [PK_Classes] PRIMARY KEY CLUSTERED ([Uid] ASC),
    CONSTRAINT [UQ_Classes_Code] UNIQUE ([ClassCode]),
    CONSTRAINT [FK_Classes_Program] FOREIGN KEY ([ProgramID])
        REFERENCES [dbo].[ref_Programs] ([Uid])
);
GO

SET IDENTITY_INSERT dbo.Classes ON;

INSERT INTO dbo.Classes (
    Uid, ProgramID, ClassCode, ClassName, SemesterNo, Semester,
    AcademicYear, Section, Shift, RoomNo, MaxStrength, IsActive, CreatedAt
)
SELECT
    Uid, ProgramID, ClassCode, ClassName, SemesterNo, Semester,
    AcademicYear, Section, Shift, RoomNo, MaxStrength, IsActive, CreatedAt
FROM #Classes_Backup;

SET IDENTITY_INSERT dbo.Classes OFF;
GO

ALTER TABLE dbo.ClassCourses
    ADD CONSTRAINT FK_ClassCourses_Class FOREIGN KEY (ClassID) REFERENCES dbo.Classes (Uid);
ALTER TABLE dbo.ClassStudents
    ADD CONSTRAINT FK_ClassStudents_Class FOREIGN KEY (ClassID) REFERENCES dbo.Classes (Uid);
ALTER TABLE dbo.TeacherCourseAssignments
    ADD CONSTRAINT FK_TCA_Class FOREIGN KEY (ClassID) REFERENCES dbo.Classes (Uid);
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS RestoredRows FROM dbo.Classes;
