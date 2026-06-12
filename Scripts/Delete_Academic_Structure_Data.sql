/* Delete rows from the requested academic structure tables (child-first order).
   Table names in DB: ClassStudents, TeacherCourseAssignments. */
USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Deleted TABLE (TableName sysname NOT NULL, RowsDeleted int NOT NULL);

DELETE FROM dbo.StudentCourseEnrollments;
INSERT INTO @Deleted VALUES (N'StudentCourseEnrollments', @@ROWCOUNT);

DELETE FROM dbo.TeacherClassCourses;
INSERT INTO @Deleted VALUES (N'TeacherClassCourses', @@ROWCOUNT);

DELETE FROM dbo.TeacherCourseAssignments;
INSERT INTO @Deleted VALUES (N'TeacherCourseAssignments', @@ROWCOUNT);

DELETE FROM dbo.ClassStudents;
INSERT INTO @Deleted VALUES (N'ClassStudents', @@ROWCOUNT);

DELETE FROM dbo.StudentEnrollments;
INSERT INTO @Deleted VALUES (N'StudentEnrollments', @@ROWCOUNT);

DELETE FROM dbo.ClassCourses;
INSERT INTO @Deleted VALUES (N'ClassCourses', @@ROWCOUNT);

DELETE FROM dbo.Classes;
INSERT INTO @Deleted VALUES (N'Classes', @@ROWCOUNT);

DELETE FROM dbo.Courses;
INSERT INTO @Deleted VALUES (N'Courses', @@ROWCOUNT);

-- ref_Programs: blocked while dbo.Students (ProgramID), dbo.FeeStructures, dbo.Teachers still reference programs.
UPDATE dbo.Teachers SET ProgramID = NULL WHERE ProgramID IS NOT NULL;
INSERT INTO @Deleted VALUES (N'Teachers.ProgramID cleared', @@ROWCOUNT);

DELETE FROM dbo.ref_Programs
WHERE Uid NOT IN (SELECT ProgramID FROM dbo.Students)
  AND Uid NOT IN (SELECT ProgramID FROM dbo.FeeStructures WHERE ProgramID IS NOT NULL);

INSERT INTO @Deleted VALUES (N'ref_Programs (unreferenced only)', @@ROWCOUNT);

COMMIT TRANSACTION;

SELECT TableName, RowsDeleted FROM @Deleted ORDER BY TableName;

SELECT
    (SELECT COUNT(*) FROM dbo.StudentCourseEnrollments) AS StudentCourseEnrollments,
    (SELECT COUNT(*) FROM dbo.TeacherClassCourses) AS TeacherClassCourses,
    (SELECT COUNT(*) FROM dbo.TeacherCourseAssignments) AS TeacherCourseAssignments,
    (SELECT COUNT(*) FROM dbo.ClassStudents) AS ClassStudents,
    (SELECT COUNT(*) FROM dbo.StudentEnrollments) AS StudentEnrollments,
    (SELECT COUNT(*) FROM dbo.ClassCourses) AS ClassCourses,
    (SELECT COUNT(*) FROM dbo.Classes) AS Classes,
    (SELECT COUNT(*) FROM dbo.Courses) AS Courses,
    (SELECT COUNT(*) FROM dbo.ref_Programs) AS ref_Programs,
    (SELECT COUNT(*) FROM dbo.Students) AS Students_still_referencing_programs,
    (SELECT COUNT(*) FROM dbo.FeeStructures) AS FeeStructures_still_referencing_programs;
