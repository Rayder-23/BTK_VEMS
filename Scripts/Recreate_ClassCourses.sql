USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#ClassCourses_Backup') IS NOT NULL
    DROP TABLE #ClassCourses_Backup;

SELECT
    Uid,
    ClassID,
    CourseID,
    TeacherID,
    IsActive,
    CreatedAt
INTO #ClassCourses_Backup
FROM dbo.ClassCourses;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_ClassCourse')
    ALTER TABLE dbo.StudentCourseEnrollments DROP CONSTRAINT FK_SCE_ClassCourse;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassCourses_Class')
    ALTER TABLE dbo.ClassCourses DROP CONSTRAINT FK_ClassCourses_Class;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassCourses_Course')
    ALTER TABLE dbo.ClassCourses DROP CONSTRAINT FK_ClassCourses_Course;
GO

IF OBJECT_ID(N'dbo.ClassCourses', N'U') IS NOT NULL
    DROP TABLE dbo.ClassCourses;
GO

CREATE TABLE [dbo].[ClassCourses](
    [Uid]       INT IDENTITY(1,1)  NOT NULL,
    [ClassID]   INT                NOT NULL,
    [CourseID]  INT                NOT NULL,
    [TeacherID] INT                NULL,
    [IsActive]  BIT                NOT NULL DEFAULT(1),
    [CreatedAt] DATETIME2(7)       NOT NULL DEFAULT(SYSDATETIME()),

    CONSTRAINT [PK_ClassCourses] PRIMARY KEY CLUSTERED ([Uid] ASC),
    CONSTRAINT [UQ_ClassCourses] UNIQUE ([ClassID], [CourseID]),
    CONSTRAINT [FK_ClassCourses_Class] FOREIGN KEY ([ClassID])
        REFERENCES [dbo].[Classes] ([Uid]),
    CONSTRAINT [FK_ClassCourses_Course] FOREIGN KEY ([CourseID])
        REFERENCES [dbo].[Courses] ([Uid])
);
GO

SET IDENTITY_INSERT dbo.ClassCourses ON;

INSERT INTO dbo.ClassCourses (Uid, ClassID, CourseID, TeacherID, IsActive, CreatedAt)
SELECT Uid, ClassID, CourseID, TeacherID, IsActive, CreatedAt
FROM #ClassCourses_Backup;

SET IDENTITY_INSERT dbo.ClassCourses OFF;
GO

ALTER TABLE dbo.StudentCourseEnrollments
    ADD CONSTRAINT FK_SCE_ClassCourse FOREIGN KEY (ClassCourseID) REFERENCES dbo.ClassCourses (Uid);
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS RestoredRows FROM dbo.ClassCourses;
