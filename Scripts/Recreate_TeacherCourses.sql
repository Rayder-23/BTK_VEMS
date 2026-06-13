USE [VEMS];
GO

SET NOCOUNT ON;

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
