USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- Drop inbound foreign keys referencing Courses
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassCourses_Course')
    ALTER TABLE dbo.ClassCourses DROP CONSTRAINT FK_ClassCourses_Course;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCA_Course')
    ALTER TABLE dbo.TeacherCourseAssignments DROP CONSTRAINT FK_TCA_Course;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Exams_Course')
    ALTER TABLE dbo.Exams DROP CONSTRAINT FK_Exams_Course;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MarkingScheme_Course')
    ALTER TABLE dbo.MarkingScheme DROP CONSTRAINT FK_MarkingScheme_Course;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ResultDetails_Course')
    ALTER TABLE dbo.ResultDetails DROP CONSTRAINT FK_ResultDetails_Course;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StudentGrades_Course')
    ALTER TABLE dbo.StudentGrades DROP CONSTRAINT FK_StudentGrades_Course;
GO

IF OBJECT_ID('tempdb..#Courses_Backup') IS NOT NULL
    DROP TABLE #Courses_Backup;

SELECT
    Uid,
    CourseCode,
    CourseTitle,
    CreditHours,
    IsActive
INTO #Courses_Backup
FROM dbo.Courses;
GO

IF OBJECT_ID(N'dbo.Courses', N'U') IS NOT NULL
    DROP TABLE dbo.Courses;
GO

CREATE TABLE [dbo].[Courses](
    [CourseID]    INT IDENTITY(1,1) NOT NULL,
    [CourseCode]  VARCHAR(20)       NULL,
    [CourseName]  VARCHAR(200)      NOT NULL,
    [CreditHours] INT               NULL,
    [IsActive]    BIT               NOT NULL CONSTRAINT [DF_Courses_IsActive] DEFAULT (1),
    CONSTRAINT [PK_Courses] PRIMARY KEY CLUSTERED ([CourseID] ASC)
) ON [PRIMARY];
GO

SET IDENTITY_INSERT dbo.Courses ON;

INSERT INTO dbo.Courses (CourseID, CourseCode, CourseName, CreditHours, IsActive)
SELECT
    Uid,
    LEFT(CourseCode, 20),
    LEFT(CAST(CourseTitle AS VARCHAR(200)), 200),
    CAST(CreditHours AS INT),
    IsActive
FROM #Courses_Backup;

SET IDENTITY_INSERT dbo.Courses OFF;
GO

ALTER TABLE dbo.ClassCourses
    ADD CONSTRAINT FK_ClassCourses_Course FOREIGN KEY (CourseID) REFERENCES dbo.Courses (CourseID);
ALTER TABLE dbo.TeacherCourseAssignments
    ADD CONSTRAINT FK_TCA_Course FOREIGN KEY (CourseID) REFERENCES dbo.Courses (CourseID);
IF OBJECT_ID(N'dbo.Exams', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.Exams', N'CourseID') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Exams_Course')
    ALTER TABLE dbo.Exams
        ADD CONSTRAINT FK_Exams_Course FOREIGN KEY (CourseID) REFERENCES dbo.Courses (CourseID);
IF OBJECT_ID(N'dbo.MarkingScheme', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.MarkingScheme', N'CourseID') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MarkingScheme_Course')
    ALTER TABLE dbo.MarkingScheme
        ADD CONSTRAINT FK_MarkingScheme_Course FOREIGN KEY (CourseID) REFERENCES dbo.Courses (CourseID);
IF OBJECT_ID(N'dbo.ResultDetails', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.ResultDetails', N'CourseID') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ResultDetails_Course')
    ALTER TABLE dbo.ResultDetails
        ADD CONSTRAINT FK_ResultDetails_Course FOREIGN KEY (CourseID) REFERENCES dbo.Courses (CourseID);
IF OBJECT_ID(N'dbo.StudentGrades', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.StudentGrades', N'CourseID') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StudentGrades_Course')
    ALTER TABLE dbo.StudentGrades
        ADD CONSTRAINT FK_StudentGrades_Course FOREIGN KEY (CourseID) REFERENCES dbo.Courses (CourseID);
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS CoursesRows FROM dbo.Courses;
