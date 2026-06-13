USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

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

ALTER TABLE dbo.ClassCourses
    ADD CONSTRAINT FK_ClassCourses_Course FOREIGN KEY (CourseID) REFERENCES dbo.Courses (CourseID);
ALTER TABLE dbo.TeacherCourseAssignments
    ADD CONSTRAINT FK_TCA_Course FOREIGN KEY (CourseID) REFERENCES dbo.Courses (CourseID);
GO

COMMIT TRANSACTION;
GO
