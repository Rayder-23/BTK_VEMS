USE [VEMS];
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Timetables', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Timetables](
        [TimetableID]     INT IDENTITY(1,1) NOT NULL,
        [DayName]         VARCHAR(20)       NOT NULL,
        [PeriodID]        INT               NOT NULL,
        [ClassSectionID]  INT               NULL,
        [CourseSectionID] INT               NULL,
        [CourseID]        INT               NOT NULL,
        [TeacherID]       INT               NOT NULL,
        [RoomNo]          VARCHAR(50)       NULL,
        CONSTRAINT [PK_Timetables] PRIMARY KEY CLUSTERED ([TimetableID] ASC),
        CONSTRAINT [FK_Timetables_Period] FOREIGN KEY ([PeriodID])
            REFERENCES [dbo].[Periods] ([PeriodID]),
        CONSTRAINT [FK_Timetables_ClassSection] FOREIGN KEY ([ClassSectionID])
            REFERENCES [dbo].[ClassSections] ([ClassSectionID]),
        CONSTRAINT [FK_Timetables_CourseSection] FOREIGN KEY ([CourseSectionID])
            REFERENCES [dbo].[CourseSections] ([CourseSectionID]),
        CONSTRAINT [FK_Timetables_Course] FOREIGN KEY ([CourseID])
            REFERENCES [dbo].[Courses] ([CourseID]),
        CONSTRAINT [FK_Timetables_Teacher] FOREIGN KEY ([TeacherID])
            REFERENCES [dbo].[Teachers] ([TeacherID])
    ) ON [PRIMARY];
END;
GO

SELECT OBJECT_ID(N'dbo.Timetables', N'U') AS TimetablesExists;
GO
