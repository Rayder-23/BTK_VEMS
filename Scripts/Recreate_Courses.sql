USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#Courses_Backup') IS NOT NULL
    DROP TABLE #Courses_Backup;

SELECT
    Uid,
    ProgramID,
    CourseCode,
    CourseTitle,
    ShortName,
    CreditHours,
    SemesterNo,
    IsMandatory,
    IsActive,
    CreatedAt
INTO #Courses_Backup
FROM dbo.Courses;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassCourses_Course')
    ALTER TABLE dbo.ClassCourses DROP CONSTRAINT FK_ClassCourses_Course;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCA_Course')
    ALTER TABLE dbo.TeacherCourseAssignments DROP CONSTRAINT FK_TCA_Course;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Courses_Prerequisite')
    ALTER TABLE dbo.Courses DROP CONSTRAINT FK_Courses_Prerequisite;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Courses_Program')
    ALTER TABLE dbo.Courses DROP CONSTRAINT FK_Courses_Program;
GO

IF OBJECT_ID(N'dbo.Courses', N'U') IS NOT NULL
    DROP TABLE dbo.Courses;
GO

CREATE TABLE [dbo].[Courses](
    [Uid]         INT IDENTITY(1,1)  NOT NULL,
    [ProgramID]   INT                NOT NULL,
    [CourseCode]  VARCHAR(20)        NOT NULL,
    [CourseTitle] NVARCHAR(150)      NOT NULL,
    [ShortName]   NVARCHAR(50)       NULL,
    [CreditHours] TINYINT            NOT NULL DEFAULT(3),
    [SemesterNo]  TINYINT            NULL,
    [IsMandatory] BIT                NOT NULL DEFAULT(1),
    [IsActive]    BIT                NOT NULL DEFAULT(1),
    [CreatedAt]   DATETIME2(7)       NOT NULL DEFAULT(SYSDATETIME()),

    CONSTRAINT [PK_Courses] PRIMARY KEY CLUSTERED ([Uid] ASC),
    CONSTRAINT [UQ_Courses_Code] UNIQUE ([CourseCode]),
    CONSTRAINT [FK_Courses_Program] FOREIGN KEY ([ProgramID])
        REFERENCES [dbo].[ref_Programs] ([Uid])
);
GO

SET IDENTITY_INSERT dbo.Courses ON;

INSERT INTO dbo.Courses (
    Uid, ProgramID, CourseCode, CourseTitle, ShortName,
    CreditHours, SemesterNo, IsMandatory, IsActive, CreatedAt
)
SELECT
    Uid, ProgramID, CourseCode, CourseTitle, ShortName,
    CreditHours, SemesterNo, IsMandatory, IsActive, CreatedAt
FROM #Courses_Backup;

SET IDENTITY_INSERT dbo.Courses OFF;
GO

ALTER TABLE dbo.ClassCourses
    ADD CONSTRAINT FK_ClassCourses_Course FOREIGN KEY (CourseID) REFERENCES dbo.Courses (Uid);
ALTER TABLE dbo.TeacherCourseAssignments
    ADD CONSTRAINT FK_TCA_Course FOREIGN KEY (CourseID) REFERENCES dbo.Courses (Uid);
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS RestoredRows FROM dbo.Courses;
