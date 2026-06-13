USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

/* --- Drop foreign keys referencing Teachers --- */
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCC_Teacher')
    ALTER TABLE dbo.TeacherClassCourses DROP CONSTRAINT FK_TCC_Teacher;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCA_Teacher')
    ALTER TABLE dbo.TeacherCourseAssignments DROP CONSTRAINT FK_TCA_Teacher;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Teachers_Program')
    ALTER TABLE dbo.Teachers DROP CONSTRAINT FK_Teachers_Program;
GO

IF OBJECT_ID('tempdb..#Teachers_Backup') IS NOT NULL
    DROP TABLE #Teachers_Backup;

SELECT
    Uid,
    EmployeeCode,
    FirstName,
    LastName,
    Phone,
    Email,
    IsActive,
    CreatedAt
INTO #Teachers_Backup
FROM dbo.Teachers;
GO

IF OBJECT_ID(N'dbo.Teachers', N'U') IS NOT NULL
    DROP TABLE dbo.Teachers;
GO

CREATE TABLE [dbo].[Teachers](
    [TeacherID]   INT IDENTITY(1,1) NOT NULL,
    [EmployeeNo]  VARCHAR(50)       NULL,
    [TeacherName] VARCHAR(200)      NOT NULL,
    [MobileNo]    VARCHAR(30)       NULL,
    [Email]       VARCHAR(200)      NULL,
    [IsActive]    BIT               NOT NULL CONSTRAINT [DF_Teachers_IsActive] DEFAULT (1),
    [CreatedOn]   DATETIME          NOT NULL CONSTRAINT [DF_Teachers_CreatedOn] DEFAULT (GETDATE()),
    CONSTRAINT [PK_Teachers] PRIMARY KEY CLUSTERED ([TeacherID] ASC)
) ON [PRIMARY];
GO

SET IDENTITY_INSERT dbo.Teachers ON;

INSERT INTO dbo.Teachers (TeacherID, EmployeeNo, TeacherName, MobileNo, Email, IsActive, CreatedOn)
SELECT
    Uid,
    LEFT(EmployeeCode, 50),
    LEFT(LTRIM(RTRIM(FirstName + ' ' + LastName)), 200),
    LEFT(Phone, 30),
    LEFT(CAST(Email AS VARCHAR(200)), 200),
    IsActive,
    CAST(CreatedAt AS DATETIME)
FROM #Teachers_Backup;

SET IDENTITY_INSERT dbo.Teachers OFF;
GO

ALTER TABLE dbo.TeacherClassCourses
    ADD CONSTRAINT FK_TCC_Teacher FOREIGN KEY (TeacherID) REFERENCES dbo.Teachers (TeacherID);
ALTER TABLE dbo.TeacherCourseAssignments
    ADD CONSTRAINT FK_TCA_Teacher FOREIGN KEY (TeacherID) REFERENCES dbo.Teachers (TeacherID);
GO

/* --- Drop foreign keys referencing Students --- */
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AssignSub_Student')
    ALTER TABLE dbo.AssignmentSubmissions DROP CONSTRAINT FK_AssignSub_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Reminders_Student')
    ALTER TABLE dbo.ChallanReminders DROP CONSTRAINT FK_Reminders_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Challans_Student')
    ALTER TABLE dbo.Challans DROP CONSTRAINT FK_Challans_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Challans_Students')
    ALTER TABLE dbo.Challans DROP CONSTRAINT FK_Challans_Students;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ClassStudents_Student')
    ALTER TABLE dbo.ClassStudents DROP CONSTRAINT FK_ClassStudents_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Concessions_Student')
    ALTER TABLE dbo.Concessions DROP CONSTRAINT FK_Concessions_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ResultDetails_Student')
    ALTER TABLE dbo.ResultDetails DROP CONSTRAINT FK_ResultDetails_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Contacts_Student')
    ALTER TABLE dbo.StudentContacts DROP CONSTRAINT FK_Contacts_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SCE_Student')
    ALTER TABLE dbo.StudentCourseEnrollments DROP CONSTRAINT FK_SCE_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Enrollments_Student')
    ALTER TABLE dbo.StudentEnrollments DROP CONSTRAINT FK_Enrollments_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StudentGrades_Student')
    ALTER TABLE dbo.StudentGrades DROP CONSTRAINT FK_StudentGrades_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StudentMarks_Student')
    ALTER TABLE dbo.StudentMarks DROP CONSTRAINT FK_StudentMarks_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Parents_Student')
    ALTER TABLE dbo.StudentParents DROP CONSTRAINT FK_Parents_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StudentResults_Student')
    ALTER TABLE dbo.StudentResults DROP CONSTRAINT FK_StudentResults_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Siblings_Student')
    ALTER TABLE dbo.StudentSiblings DROP CONSTRAINT FK_Siblings_Student;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Siblings_Sibling')
    ALTER TABLE dbo.StudentSiblings DROP CONSTRAINT FK_Siblings_Sibling;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StudentsLogin_Students')
    ALTER TABLE dbo.StudentsLogin DROP CONSTRAINT FK_StudentsLogin_Students;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Students_Program')
    ALTER TABLE dbo.Students DROP CONSTRAINT FK_Students_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Students_Section')
    ALTER TABLE dbo.Students DROP CONSTRAINT FK_Students_Section;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Students_City')
    ALTER TABLE dbo.Students DROP CONSTRAINT FK_Students_City;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Students_Province')
    ALTER TABLE dbo.Students DROP CONSTRAINT FK_Students_Province;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Students_Country')
    ALTER TABLE dbo.Students DROP CONSTRAINT FK_Students_Country;
GO

IF OBJECT_ID('tempdb..#Students_Backup') IS NOT NULL
    DROP TABLE #Students_Backup;

SELECT
    Uid,
    RegistrationNo,
    FirstName,
    MiddleName,
    LastName,
    IsActive,
    CreatedAt
INTO #Students_Backup
FROM dbo.Students;
GO

IF OBJECT_ID(N'dbo.Students', N'U') IS NOT NULL
    DROP TABLE dbo.Students;
GO

CREATE TABLE [dbo].[Students](
    [StudentID]      INT IDENTITY(1,1) NOT NULL,
    [RegistrationNo] VARCHAR(50)       NULL,
    [StudentName]    VARCHAR(200)      NOT NULL,
    [MobileNo]       VARCHAR(30)       NULL,
    [Email]          VARCHAR(200)      NULL,
    [IsActive]       BIT               NOT NULL CONSTRAINT [DF_Students_IsActive] DEFAULT (1),
    [CreatedOn]      DATETIME          NOT NULL CONSTRAINT [DF_Students_CreatedOn] DEFAULT (GETDATE()),
    CONSTRAINT [PK_Students] PRIMARY KEY CLUSTERED ([StudentID] ASC)
) ON [PRIMARY];
GO

SET IDENTITY_INSERT dbo.Students ON;

INSERT INTO dbo.Students (StudentID, RegistrationNo, StudentName, MobileNo, Email, IsActive, CreatedOn)
SELECT
    Uid,
    LEFT(RegistrationNo, 50),
    LEFT(LTRIM(RTRIM(
        FirstName
        + ISNULL(' ' + NULLIF(LTRIM(RTRIM(MiddleName)), ''), '')
        + ' ' + LastName)), 200),
    NULL,
    NULL,
    IsActive,
    CAST(CreatedAt AS DATETIME)
FROM #Students_Backup;

SET IDENTITY_INSERT dbo.Students OFF;
GO

ALTER TABLE dbo.AssignmentSubmissions ADD CONSTRAINT FK_AssignSub_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.ChallanReminders ADD CONSTRAINT FK_Reminders_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Challans') AND name = N'StudentID')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Challans_Student')
        ALTER TABLE dbo.Challans ADD CONSTRAINT FK_Challans_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Challans_Students')
        ALTER TABLE dbo.Challans ADD CONSTRAINT FK_Challans_Students FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
END
ALTER TABLE dbo.ClassStudents ADD CONSTRAINT FK_ClassStudents_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.Concessions ADD CONSTRAINT FK_Concessions_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.ResultDetails ADD CONSTRAINT FK_ResultDetails_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.StudentContacts ADD CONSTRAINT FK_Contacts_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.StudentCourseEnrollments ADD CONSTRAINT FK_SCE_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.StudentEnrollments ADD CONSTRAINT FK_Enrollments_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.StudentGrades ADD CONSTRAINT FK_StudentGrades_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.StudentMarks ADD CONSTRAINT FK_StudentMarks_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.StudentParents ADD CONSTRAINT FK_Parents_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.StudentResults ADD CONSTRAINT FK_StudentResults_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.StudentSiblings ADD CONSTRAINT FK_Siblings_Student FOREIGN KEY (StudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.StudentSiblings ADD CONSTRAINT FK_Siblings_Sibling FOREIGN KEY (SiblingStudentID) REFERENCES dbo.Students (StudentID);
ALTER TABLE dbo.StudentsLogin ADD CONSTRAINT FK_StudentsLogin_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students (StudentID);
GO

IF OBJECT_ID(N'dbo.TeacherCourses', N'U') IS NOT NULL
    DROP TABLE dbo.TeacherCourses;
GO

CREATE TABLE [dbo].[TeacherCourses](
    [UID]       INT IDENTITY(1,1) NOT NULL,
    [TeacherID] INT               NOT NULL,
    [CourseID]  INT               NOT NULL,
    CONSTRAINT [PK_TeacherCourses] PRIMARY KEY CLUSTERED ([UID] ASC),
    CONSTRAINT [FK_TeacherCourses_Teacher] FOREIGN KEY ([TeacherID]) REFERENCES [dbo].[Teachers] ([TeacherID]),
    CONSTRAINT [FK_TeacherCourses_Course] FOREIGN KEY ([CourseID]) REFERENCES [dbo].[Courses] ([CourseID])
) ON [PRIMARY];
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS TeachersRows FROM dbo.Teachers;
SELECT COUNT(*) AS StudentsRows FROM dbo.Students;
