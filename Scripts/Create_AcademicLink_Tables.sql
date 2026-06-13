USE [VEMS];
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'tempdb..#StudentEnrollments_Backup', N'U') IS NOT NULL
    DROP TABLE #StudentEnrollments_Backup;

CREATE TABLE #StudentEnrollments_Backup (
    OldUid           INT           NOT NULL,
    StudentID        INT           NOT NULL,
    ProgramID        INT           NOT NULL,
    ClassID          INT           NULL,
    RollNo           NVARCHAR(30)  NOT NULL,
    AcademicYear     SMALLINT      NOT NULL,
    EnrollmentDate   DATE          NOT NULL
);

IF OBJECT_ID(N'dbo.StudentEnrollments', N'U') IS NOT NULL
BEGIN
    INSERT INTO #StudentEnrollments_Backup (
        OldUid, StudentID, ProgramID, ClassID, RollNo, AcademicYear, EnrollmentDate)
    SELECT
        Uid,
        StudentID,
        ProgramID,
        ClassID,
        RollNo,
        AcademicYear,
        EnrollmentDate
    FROM dbo.StudentEnrollments;
END;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_Enrollment')
    ALTER TABLE dbo.StudentCourseEnrollments DROP CONSTRAINT FK_SCE_Enrollment;

IF OBJECT_ID(N'dbo.StudentEnrollments', N'U') IS NOT NULL
    DROP TABLE dbo.StudentEnrollments;
GO

CREATE TABLE [dbo].[StudentEnrollments](
    [UID]             INT IDENTITY(1,1) NOT NULL,
    [AcademicYearID]  INT               NOT NULL,
    [StudentID]       INT               NOT NULL,
    [ProgramID]       INT               NOT NULL,
    [ClassSectionID]  INT               NULL,
    [RollNo]          INT               NULL,
    [EnrollmentDate]  DATE              NOT NULL CONSTRAINT DF_StudentEnrollments_EnrollmentDate DEFAULT (CONVERT(date, GETDATE())),
    CONSTRAINT [PK_StudentEnrollments] PRIMARY KEY CLUSTERED ([UID] ASC),
    CONSTRAINT [FK_StudentEnrollments_AcademicYear] FOREIGN KEY ([AcademicYearID])
        REFERENCES [dbo].[AcademicYears] ([AcademicYearID]),
    CONSTRAINT [FK_StudentEnrollments_Student] FOREIGN KEY ([StudentID])
        REFERENCES [dbo].[Students] ([StudentID]),
    CONSTRAINT [FK_StudentEnrollments_Program] FOREIGN KEY ([ProgramID])
        REFERENCES [dbo].[Programs] ([ProgramID]),
    CONSTRAINT [FK_StudentEnrollments_ClassSection] FOREIGN KEY ([ClassSectionID])
        REFERENCES [dbo].[ClassSections] ([ClassSectionID])
) ON [PRIMARY];
GO

SET IDENTITY_INSERT dbo.StudentEnrollments ON;

INSERT INTO dbo.StudentEnrollments (
    UID,
    AcademicYearID,
    StudentID,
    ProgramID,
    ClassSectionID,
    RollNo,
    EnrollmentDate)
SELECT
    b.OldUid,
    COALESCE(
        ay.AcademicYearID,
        (SELECT TOP 1 AcademicYearID FROM dbo.AcademicYears WHERE IsCurrent = 1 ORDER BY AcademicYearID),
        (SELECT TOP 1 AcademicYearID FROM dbo.AcademicYears ORDER BY AcademicYearID)
    ),
    b.StudentID,
    b.ProgramID,
    cs.ClassSectionID,
    TRY_CAST(b.RollNo AS INT),
    b.EnrollmentDate
FROM #StudentEnrollments_Backup b
OUTER APPLY (
    SELECT TOP 1 ay2.AcademicYearID
    FROM dbo.AcademicYears ay2
    WHERE ay2.YearName LIKE CAST(b.AcademicYear AS varchar(4)) + N'%'
       OR ay2.YearName LIKE N'%-' + CAST(b.AcademicYear AS varchar(4))
    ORDER BY ay2.IsCurrent DESC, ay2.AcademicYearID
) ay
OUTER APPLY (
    SELECT MIN(cs2.ClassSectionID) AS ClassSectionID
    FROM dbo.ClassSections cs2
    WHERE cs2.ClassID = b.ClassID
) cs;

SET IDENTITY_INSERT dbo.StudentEnrollments OFF;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_Enrollment')
    ALTER TABLE dbo.StudentCourseEnrollments
        ADD CONSTRAINT FK_SCE_Enrollment FOREIGN KEY (EnrollmentID)
            REFERENCES dbo.StudentEnrollments (UID);
GO

IF OBJECT_ID(N'dbo.ClassSectionCourses', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ClassSectionCourses](
        [UID]             INT IDENTITY(1,1) NOT NULL,
        [ClassSectionID]  INT               NOT NULL,
        [CourseID]        INT               NOT NULL,
        CONSTRAINT [PK_ClassSectionCourses] PRIMARY KEY CLUSTERED ([UID] ASC),
        CONSTRAINT [UQ_ClassSectionCourses] UNIQUE ([ClassSectionID], [CourseID]),
        CONSTRAINT [FK_ClassSectionCourses_ClassSection] FOREIGN KEY ([ClassSectionID])
            REFERENCES [dbo].[ClassSections] ([ClassSectionID]),
        CONSTRAINT [FK_ClassSectionCourses_Course] FOREIGN KEY ([CourseID])
            REFERENCES [dbo].[Courses] ([CourseID])
    ) ON [PRIMARY];
END;
GO

IF OBJECT_ID(N'dbo.TeacherAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TeacherAssignments](
        [UID]             INT IDENTITY(1,1) NOT NULL,
        [AcademicYearID]  INT               NOT NULL,
        [TeacherID]       INT               NOT NULL,
        [CourseID]        INT               NOT NULL,
        [ClassSectionID]  INT               NULL,
        CONSTRAINT [PK_TeacherAssignments] PRIMARY KEY CLUSTERED ([UID] ASC),
        CONSTRAINT [FK_TeacherAssignments_AcademicYear] FOREIGN KEY ([AcademicYearID])
            REFERENCES [dbo].[AcademicYears] ([AcademicYearID]),
        CONSTRAINT [FK_TeacherAssignments_Teacher] FOREIGN KEY ([TeacherID])
            REFERENCES [dbo].[Teachers] ([TeacherID]),
        CONSTRAINT [FK_TeacherAssignments_Course] FOREIGN KEY ([CourseID])
            REFERENCES [dbo].[Courses] ([CourseID]),
        CONSTRAINT [FK_TeacherAssignments_ClassSection] FOREIGN KEY ([ClassSectionID])
            REFERENCES [dbo].[ClassSections] ([ClassSectionID])
    ) ON [PRIMARY];
END;
GO

SELECT COUNT(*) AS StudentEnrollmentRows FROM dbo.StudentEnrollments;
SELECT COUNT(*) AS ClassSectionCourseRows FROM dbo.ClassSectionCourses;
SELECT COUNT(*) AS TeacherAssignmentRows FROM dbo.TeacherAssignments;
GO
