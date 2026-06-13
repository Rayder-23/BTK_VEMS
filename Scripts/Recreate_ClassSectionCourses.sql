USE [VEMS];
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'tempdb..#ClassCourses_Backup', N'U') IS NOT NULL
    DROP TABLE #ClassCourses_Backup;
IF OBJECT_ID(N'tempdb..#ClassCourseMap', N'U') IS NOT NULL
    DROP TABLE #ClassCourseMap;

CREATE TABLE #ClassCourses_Backup (
    Uid      INT NOT NULL,
    ClassID  INT NOT NULL,
    CourseID INT NOT NULL
);

IF OBJECT_ID(N'dbo.ClassCourses', N'U') IS NOT NULL
BEGIN
    INSERT INTO #ClassCourses_Backup (Uid, ClassID, CourseID)
    SELECT Uid, ClassID, CourseID
    FROM dbo.ClassCourses;
END;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_ClassCourse')
    ALTER TABLE dbo.StudentCourseEnrollments DROP CONSTRAINT FK_SCE_ClassCourse;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_ClassSectionCourse')
    ALTER TABLE dbo.StudentCourseEnrollments DROP CONSTRAINT FK_SCE_ClassSectionCourse;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCC_ClassCourse')
    ALTER TABLE dbo.TeacherClassCourses DROP CONSTRAINT FK_TCC_ClassCourse;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCC_ClassSectionCourse')
    ALTER TABLE dbo.TeacherClassCourses DROP CONSTRAINT FK_TCC_ClassSectionCourse;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassCourses_Class')
    ALTER TABLE dbo.ClassCourses DROP CONSTRAINT FK_ClassCourses_Class;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassCourses_Course')
    ALTER TABLE dbo.ClassCourses DROP CONSTRAINT FK_ClassCourses_Course;

IF OBJECT_ID(N'dbo.ClassCourses', N'U') IS NOT NULL
    DROP TABLE dbo.ClassCourses;

IF OBJECT_ID(N'dbo.ClassSectionCourses', N'U') IS NOT NULL
    DROP TABLE dbo.ClassSectionCourses;

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

COMMIT TRANSACTION;
GO

INSERT INTO dbo.ClassSectionCourses (ClassSectionID, CourseID)
SELECT
    cs.ClassSectionID,
    cc.CourseID
FROM #ClassCourses_Backup cc
INNER JOIN (
    SELECT ClassID, MIN(ClassSectionID) AS ClassSectionID
    FROM dbo.ClassSections
    GROUP BY ClassID
) cs ON cs.ClassID = cc.ClassID
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.ClassSectionCourses existing
    WHERE existing.ClassSectionID = cs.ClassSectionID
      AND existing.CourseID = cc.CourseID
);

SELECT
    cc.Uid AS OldUid,
    csc.UID AS NewUid
INTO #ClassCourseMap
FROM #ClassCourses_Backup cc
INNER JOIN (
    SELECT ClassID, MIN(ClassSectionID) AS ClassSectionID
    FROM dbo.ClassSections
    GROUP BY ClassID
) cs ON cs.ClassID = cc.ClassID
INNER JOIN dbo.ClassSectionCourses csc
    ON csc.ClassSectionID = cs.ClassSectionID
   AND csc.CourseID = cc.CourseID;
GO

IF COL_LENGTH(N'dbo.StudentCourseEnrollments', N'ClassSectionCourseID') IS NULL
    ALTER TABLE dbo.StudentCourseEnrollments ADD ClassSectionCourseID INT NULL;
GO

UPDATE sce
SET sce.ClassSectionCourseID = map.NewUid
FROM dbo.StudentCourseEnrollments sce
INNER JOIN #ClassCourseMap map ON map.OldUid = sce.ClassCourseID
WHERE COL_LENGTH(N'dbo.StudentCourseEnrollments', N'ClassCourseID') IS NOT NULL;
GO

DELETE FROM dbo.StudentCourseEnrollments
WHERE ClassSectionCourseID IS NULL
  AND COL_LENGTH(N'dbo.StudentCourseEnrollments', N'ClassCourseID') IS NOT NULL;
GO

IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = N'UQ_StudentCourseEnrollments'
      AND parent_object_id = OBJECT_ID(N'dbo.StudentCourseEnrollments')
)
    ALTER TABLE dbo.StudentCourseEnrollments DROP CONSTRAINT UQ_StudentCourseEnrollments;
GO

IF COL_LENGTH(N'dbo.StudentCourseEnrollments', N'ClassCourseID') IS NOT NULL
    ALTER TABLE dbo.StudentCourseEnrollments DROP COLUMN ClassCourseID;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.StudentCourseEnrollments WHERE ClassSectionCourseID IS NULL)
    ALTER TABLE dbo.StudentCourseEnrollments ALTER COLUMN ClassSectionCourseID INT NOT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = N'UQ_StudentCourseEnrollments'
      AND parent_object_id = OBJECT_ID(N'dbo.StudentCourseEnrollments')
)
    ALTER TABLE dbo.StudentCourseEnrollments
        ADD CONSTRAINT UQ_StudentCourseEnrollments UNIQUE (StudentID, ClassSectionCourseID);
GO

UPDATE tcc
SET tcc.ClassCourseID = map.NewUid
FROM dbo.TeacherClassCourses tcc
INNER JOIN #ClassCourseMap map ON map.OldUid = tcc.ClassCourseID
WHERE COL_LENGTH(N'dbo.TeacherClassCourses', N'ClassCourseID') IS NOT NULL;
GO

DELETE FROM dbo.TeacherClassCourses
WHERE COL_LENGTH(N'dbo.TeacherClassCourses', N'ClassCourseID') IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.ClassSectionCourses csc
      WHERE csc.UID = TeacherClassCourses.ClassCourseID
  );
GO

IF COL_LENGTH(N'dbo.TeacherClassCourses', N'ClassCourseID') IS NOT NULL
    EXEC sp_rename N'dbo.TeacherClassCourses.ClassCourseID', N'ClassSectionCourseID', N'COLUMN';
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_ClassSectionCourse')
    ALTER TABLE dbo.StudentCourseEnrollments
        ADD CONSTRAINT FK_SCE_ClassSectionCourse FOREIGN KEY (ClassSectionCourseID)
            REFERENCES dbo.ClassSectionCourses (UID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCC_ClassSectionCourse')
    ALTER TABLE dbo.TeacherClassCourses
        ADD CONSTRAINT FK_TCC_ClassSectionCourse FOREIGN KEY (ClassSectionCourseID)
            REFERENCES dbo.ClassSectionCourses (UID);
GO

SELECT COUNT(*) AS ClassSectionCourseRows FROM dbo.ClassSectionCourses;
GO
