USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#StudentCourseEnrollments_Backup') IS NOT NULL
    DROP TABLE #StudentCourseEnrollments_Backup;

SELECT
    Uid,
    EnrollmentID,
    StudentID,
    ClassCourseID,
    Status,
    IsActive,
    CreatedAt
INTO #StudentCourseEnrollments_Backup
FROM dbo.StudentCourseEnrollments;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_Enrollment')
    ALTER TABLE dbo.StudentCourseEnrollments DROP CONSTRAINT FK_SCE_Enrollment;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_Student')
    ALTER TABLE dbo.StudentCourseEnrollments DROP CONSTRAINT FK_SCE_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_ClassCourse')
    ALTER TABLE dbo.StudentCourseEnrollments DROP CONSTRAINT FK_SCE_ClassCourse;
GO

IF OBJECT_ID(N'dbo.StudentCourseEnrollments', N'U') IS NOT NULL
    DROP TABLE dbo.StudentCourseEnrollments;
GO

CREATE TABLE [dbo].[StudentCourseEnrollments](
    [Uid]           INT IDENTITY(1,1)  NOT NULL,
    [EnrollmentID]  INT                NOT NULL,
    [StudentID]     INT                NOT NULL,
    [ClassCourseID] INT                NOT NULL,
    [Status]        NVARCHAR(20)       NOT NULL DEFAULT('Active'),
    [IsActive]      BIT                NOT NULL DEFAULT(1),
    [CreatedAt]     DATETIME2(7)       NOT NULL DEFAULT(SYSDATETIME()),

    CONSTRAINT [PK_StudentCourseEnrollments] PRIMARY KEY CLUSTERED ([Uid] ASC),
    CONSTRAINT [UQ_StudentCourseEnrollments] UNIQUE ([StudentID], [ClassCourseID]),
    CONSTRAINT [FK_SCE_Enrollment] FOREIGN KEY ([EnrollmentID])
        REFERENCES [dbo].[StudentEnrollments] ([Uid]),
    CONSTRAINT [FK_SCE_Student] FOREIGN KEY ([StudentID])
        REFERENCES [dbo].[Students] ([Uid]),
    CONSTRAINT [FK_SCE_ClassCourse] FOREIGN KEY ([ClassCourseID])
        REFERENCES [dbo].[ClassCourses] ([Uid])
);
GO

SET IDENTITY_INSERT dbo.StudentCourseEnrollments ON;

INSERT INTO dbo.StudentCourseEnrollments (
    Uid,
    EnrollmentID,
    StudentID,
    ClassCourseID,
    Status,
    IsActive,
    CreatedAt
)
SELECT
    Uid,
    EnrollmentID,
    StudentID,
    ClassCourseID,
    Status,
    IsActive,
    CreatedAt
FROM #StudentCourseEnrollments_Backup;

SET IDENTITY_INSERT dbo.StudentCourseEnrollments OFF;
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS RestoredRows FROM dbo.StudentCourseEnrollments;
