USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#StudentEnrollments_Backup') IS NOT NULL
    DROP TABLE #StudentEnrollments_Backup;

SELECT
    se.Uid,
    se.StudentID,
    se.ProgramID,
    COALESCE(
        (SELECT TOP 1 c.Uid FROM dbo.Classes c WHERE c.ProgramID = se.ProgramID ORDER BY c.Uid),
        (SELECT MIN(Uid) FROM dbo.Classes)
    ) AS ClassID,
    se.RollNo,
    se.AcademicYear,
    se.GradeOrSemester,
    se.EnrollmentDate,
    se.EnrollmentStatus,
    CAST(1 AS bit) AS IsActive,
    se.CreatedAt
INTO #StudentEnrollments_Backup
FROM dbo.StudentEnrollments se;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_Enrollment')
    ALTER TABLE dbo.StudentCourseEnrollments DROP CONSTRAINT FK_SCE_Enrollment;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Enrollments_Program')
    ALTER TABLE dbo.StudentEnrollments DROP CONSTRAINT FK_Enrollments_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Enrollments_Student')
    ALTER TABLE dbo.StudentEnrollments DROP CONSTRAINT FK_Enrollments_Student;
GO

IF OBJECT_ID(N'dbo.StudentEnrollments', N'U') IS NOT NULL
    DROP TABLE dbo.StudentEnrollments;
GO

CREATE TABLE [dbo].[StudentEnrollments](
    [Uid]              INT IDENTITY(1,1)  NOT NULL,
    [StudentID]        INT                NOT NULL,
    [ProgramID]        INT                NOT NULL,
    [ClassID]          INT                NOT NULL,
    [RollNo]           NVARCHAR(30)       NOT NULL,
    [AcademicYear]     SMALLINT           NOT NULL,
    [GradeOrSemester]  TINYINT            NOT NULL,
    [EnrollmentDate]   DATE               NOT NULL DEFAULT(GETDATE()),
    [EnrollmentStatus] NVARCHAR(20)       NOT NULL DEFAULT('Active'),
    [IsActive]         BIT                NOT NULL DEFAULT(1),
    [CreatedAt]        DATETIME2(7)       NOT NULL DEFAULT(SYSDATETIME()),

    CONSTRAINT [PK_Enrollments] PRIMARY KEY CLUSTERED ([Uid] ASC),
    CONSTRAINT [UQ_Enrollments_Period] UNIQUE ([StudentID], [ProgramID], [AcademicYear], [GradeOrSemester]),
    CONSTRAINT [FK_Enrollments_Student] FOREIGN KEY ([StudentID])
        REFERENCES [dbo].[Students] ([Uid]),
    CONSTRAINT [FK_Enrollments_Program] FOREIGN KEY ([ProgramID])
        REFERENCES [dbo].[ref_Programs] ([Uid]),
    CONSTRAINT [FK_Enrollments_Class] FOREIGN KEY ([ClassID])
        REFERENCES [dbo].[Classes] ([Uid])
);
GO

SET IDENTITY_INSERT dbo.StudentEnrollments ON;

INSERT INTO dbo.StudentEnrollments (
    Uid,
    StudentID,
    ProgramID,
    ClassID,
    RollNo,
    AcademicYear,
    GradeOrSemester,
    EnrollmentDate,
    EnrollmentStatus,
    IsActive,
    CreatedAt
)
SELECT
    Uid,
    StudentID,
    ProgramID,
    ClassID,
    RollNo,
    AcademicYear,
    GradeOrSemester,
    EnrollmentDate,
    EnrollmentStatus,
    IsActive,
    CreatedAt
FROM #StudentEnrollments_Backup;

SET IDENTITY_INSERT dbo.StudentEnrollments OFF;
GO

ALTER TABLE dbo.StudentCourseEnrollments
    ADD CONSTRAINT FK_SCE_Enrollment FOREIGN KEY (EnrollmentID) REFERENCES dbo.StudentEnrollments (Uid);
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS RestoredRows FROM dbo.StudentEnrollments;
