/*
    Seed Matric master courses, sections (9-A, 9-B, 8-A), class-course links,
    and sample timetable rows in TeacherCourseAssignments.
    Idempotent — safe to re-run.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @ProgramId INT = 6;
DECLARE @TeacherId INT = (
    SELECT TOP (1) TeacherID FROM dbo.Teachers WHERE IsActive = 1 ORDER BY TeacherID
);
DECLARE @CreatedBy INT = 1;
DECLARE @AcademicYear SMALLINT = 2026;
DECLARE @Semester NVARCHAR(20) = N'Fall';

IF @TeacherId IS NULL
BEGIN
    RAISERROR('No active teacher found. Add a teacher before seeding timetable rows.', 16, 1);
    RETURN;
END;

/* --- Master courses (12 subjects) --- */
DECLARE @Courses TABLE (
    CourseCode VARCHAR(20) NOT NULL PRIMARY KEY,
    CourseName VARCHAR(200) NOT NULL,
    CreditHours INT NOT NULL
);

INSERT INTO @Courses (CourseCode, CourseName, CreditHours) VALUES
    (N'ENG', N'English', 3),
    (N'URD', N'Urdu', 3),
    (N'MTH', N'Mathematics', 3),
    (N'PHY', N'Physics', 3),
    (N'CHM', N'Chemistry', 3),
    (N'BIO', N'Biology', 3),
    (N'ISL', N'Islamiyat', 2),
    (N'PAK', N'Pakistan Studies', 2),
    (N'HIS', N'History', 3),
    (N'GEO', N'Geography', 3),
    (N'CMP', N'Computer', 3),
    (N'SCI', N'Science', 3);

INSERT INTO dbo.Courses (CourseCode, CourseName, CreditHours, IsActive)
SELECT
    c.CourseCode,
    c.CourseName,
    c.CreditHours,
    1
FROM @Courses c
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Courses existing
    WHERE existing.CourseCode = c.CourseCode
);

/* --- Sections --- */
IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = N'9-A')
BEGIN
    INSERT INTO dbo.Classes (ClassCode, ClassName, SortOrder, IsActive)
    VALUES (N'003', N'9-A', 3, 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = N'9-B')
BEGIN
    INSERT INTO dbo.Classes (ClassCode, ClassName, SortOrder, IsActive)
    VALUES (N'004', N'9-B', 4, 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Classes WHERE ClassName = N'8-A')
BEGIN
    INSERT INTO dbo.Classes (ClassCode, ClassName, SortOrder, IsActive)
    VALUES (N'005', N'8-A', 5, 1);
END;

DECLARE @Class10A INT = (SELECT ClassID FROM dbo.Classes WHERE ClassName = N'10-A');
DECLARE @Class10B INT = (SELECT ClassID FROM dbo.Classes WHERE ClassName = N'10-B');
DECLARE @Class9A INT = (SELECT ClassID FROM dbo.Classes WHERE ClassName = N'9-A');
DECLARE @Class9B INT = (SELECT ClassID FROM dbo.Classes WHERE ClassName = N'9-B');
DECLARE @Class8A INT = (SELECT ClassID FROM dbo.Classes WHERE ClassName = N'8-A');

/* Remove legacy single "Matric Science" links so sections use the full subject list */
DELETE tcc
FROM dbo.TeacherClassCourses tcc
INNER JOIN dbo.ClassCourses cc ON tcc.ClassCourseID = cc.Uid
INNER JOIN dbo.Courses co ON cc.CourseID = co.Uid
WHERE co.ProgramID = @ProgramId
  AND co.CourseCode = N'001'
  AND cc.ClassID IN (@Class10A, @Class10B, @Class9A, @Class9B, @Class8A);

DELETE tca
FROM dbo.TeacherCourseAssignments tca
INNER JOIN dbo.Courses co ON tca.CourseID = co.Uid
WHERE co.ProgramID = @ProgramId
  AND co.CourseCode = N'001'
  AND tca.ClassID IN (@Class10A, @Class10B, @Class9A, @Class9B, @Class8A);

DELETE cc
FROM dbo.ClassCourses cc
INNER JOIN dbo.Courses co ON cc.CourseID = co.Uid
WHERE co.ProgramID = @ProgramId
  AND co.CourseCode = N'001'
  AND cc.ClassID IN (@Class10A, @Class10B, @Class9A, @Class9B, @Class8A);

DECLARE @Links TABLE (
    ClassName NVARCHAR(100) NOT NULL,
    CourseCode VARCHAR(20) NOT NULL,
    PRIMARY KEY (ClassName, CourseCode)
);

INSERT INTO @Links (ClassName, CourseCode) VALUES
    (N'10-A', N'ENG'), (N'10-A', N'URD'), (N'10-A', N'MTH'), (N'10-A', N'PHY'),
    (N'10-A', N'CHM'), (N'10-A', N'BIO'), (N'10-A', N'PAK'), (N'10-A', N'ISL'),
    (N'10-B', N'ENG'), (N'10-B', N'URD'), (N'10-B', N'MTH'), (N'10-B', N'HIS'),
    (N'10-B', N'GEO'), (N'10-B', N'CMP'), (N'10-B', N'PAK'),
    (N'9-A', N'ENG'), (N'9-A', N'URD'), (N'9-A', N'MTH'), (N'9-A', N'PHY'),
    (N'9-A', N'CHM'), (N'9-A', N'BIO'), (N'9-A', N'PAK'), (N'9-A', N'ISL'),
    (N'9-B', N'ENG'), (N'9-B', N'URD'), (N'9-B', N'MTH'), (N'9-B', N'HIS'),
    (N'9-B', N'GEO'), (N'9-B', N'CMP'), (N'9-B', N'PAK'),
    (N'8-A', N'ENG'), (N'8-A', N'MTH'), (N'8-A', N'CMP'), (N'8-A', N'SCI'),
    (N'8-A', N'HIS'), (N'8-A', N'URD');

INSERT INTO dbo.ClassCourses (ClassID, CourseID, TeacherID, IsActive, CreatedAt)
SELECT
    c.ClassID,
    co.Uid,
    @TeacherId,
    1,
    SYSDATETIME()
FROM @Links l
INNER JOIN dbo.Classes c ON c.ClassName = l.ClassName
INNER JOIN dbo.Courses co ON co.CourseCode = l.CourseCode AND co.ProgramID = @ProgramId
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.ClassCourses existing
    WHERE existing.ClassID = c.ClassID
      AND existing.CourseID = co.Uid
);

/* --- Sample timetable for 10-A (Mon–Thu) --- */
DECLARE @Slots TABLE (
    ClassName NVARCHAR(100) NOT NULL,
    CourseCode VARCHAR(20) NOT NULL,
    DayOfWeek NVARCHAR(20) NOT NULL,
    StartTime TIME(0) NOT NULL,
    EndTime TIME(0) NOT NULL,
    RoomNo NVARCHAR(30) NOT NULL,
    PRIMARY KEY (ClassName, CourseCode, DayOfWeek)
);

INSERT INTO @Slots (ClassName, CourseCode, DayOfWeek, StartTime, EndTime, RoomNo) VALUES
    (N'10-A', N'ENG', N'Monday',    '08:00', '08:45', N'R-101'),
    (N'10-A', N'URD', N'Monday',    '08:45', '09:30', N'R-101'),
    (N'10-A', N'MTH', N'Tuesday',   '08:00', '08:45', N'R-101'),
    (N'10-A', N'PHY', N'Tuesday',   '08:45', '09:30', N'Lab-1'),
    (N'10-A', N'CHM', N'Wednesday', '08:00', '08:45', N'Lab-2'),
    (N'10-A', N'BIO', N'Wednesday', '08:45', '09:30', N'Lab-2'),
    (N'10-A', N'PAK', N'Thursday',  '08:00', '08:45', N'R-101'),
    (N'10-A', N'ISL', N'Thursday',  '08:45', '09:30', N'R-101'),
    (N'10-B', N'ENG', N'Monday',    '09:30', '10:15', N'R-102'),
    (N'10-B', N'URD', N'Tuesday',   '09:30', '10:15', N'R-102'),
    (N'10-B', N'MTH', N'Wednesday', '09:30', '10:15', N'R-102'),
    (N'10-B', N'HIS', N'Thursday',  '09:30', '10:15', N'R-102'),
    (N'10-B', N'GEO', N'Friday',    '09:30', '10:15', N'R-102'),
    (N'10-B', N'CMP', N'Friday',    '10:15', '11:00', N'Lab-3'),
    (N'10-B', N'PAK', N'Monday',    '10:15', '11:00', N'R-102');

INSERT INTO dbo.TeacherCourseAssignments (
    TeacherID, ClassID, CourseID, Semester, AcademicYear,
    DayOfWeek, StartTime, EndTime, RoomNo, IsActive, CreatedBy, CreatedAt
)
SELECT
    @TeacherId,
    cl.ClassID,
    co.Uid,
    @Semester,
    @AcademicYear,
    s.DayOfWeek,
    s.StartTime,
    s.EndTime,
    s.RoomNo,
    1,
    @CreatedBy,
    SYSDATETIME()
FROM @Slots s
INNER JOIN dbo.Classes cl ON cl.ClassName = s.ClassName
INNER JOIN dbo.Courses co ON co.CourseCode = s.CourseCode AND co.ProgramID = @ProgramId
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.TeacherCourseAssignments tca
    WHERE tca.TeacherID = @TeacherId
      AND tca.ClassID = cl.ClassID
      AND tca.CourseID = co.Uid
      AND tca.Semester = @Semester
      AND tca.AcademicYear = @AcademicYear
      AND (
            (tca.DayOfWeek IS NULL AND s.DayOfWeek IS NULL)
            OR tca.DayOfWeek = s.DayOfWeek
          )
);

PRINT 'Matric seed complete.';
SELECT COUNT(*) AS CourseCount FROM dbo.Courses WHERE ProgramID = @ProgramId AND IsActive = 1;
SELECT COUNT(*) AS ClassCount FROM dbo.Classes WHERE IsActive = 1;
SELECT COUNT(*) AS ClassCourseCount
FROM dbo.ClassCourses cc
INNER JOIN dbo.Classes c ON cc.ClassID = c.ClassID
WHERE cc.IsActive = 1;
SELECT COUNT(*) AS TimetableSlotCount
FROM dbo.TeacherCourseAssignments tca
INNER JOIN dbo.Classes c ON tca.ClassID = c.ClassID
WHERE tca.IsActive = 1 AND tca.DayOfWeek IS NOT NULL;
