USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCC_Teacher')
    ALTER TABLE dbo.TeacherClassCourses DROP CONSTRAINT FK_TCC_Teacher;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TCA_Teacher')
    ALTER TABLE dbo.TeacherCourseAssignments DROP CONSTRAINT FK_TCA_Teacher;
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

ALTER TABLE dbo.TeacherClassCourses
    ADD CONSTRAINT FK_TCC_Teacher FOREIGN KEY (TeacherID) REFERENCES dbo.Teachers (TeacherID);
ALTER TABLE dbo.TeacherCourseAssignments
    ADD CONSTRAINT FK_TCA_Teacher FOREIGN KEY (TeacherID) REFERENCES dbo.Teachers (TeacherID);
GO

COMMIT TRANSACTION;
GO
