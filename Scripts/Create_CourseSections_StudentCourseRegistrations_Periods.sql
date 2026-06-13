USE [VEMS];
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.CourseSections', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CourseSections](
        [CourseSectionID] INT IDENTITY(1,1) NOT NULL,
        [AcademicYearID]    INT               NOT NULL,
        [CourseID]          INT               NOT NULL,
        [SectionName]       VARCHAR(20)       NULL,
        [Capacity]          INT               NULL,
        CONSTRAINT [PK_CourseSections] PRIMARY KEY CLUSTERED ([CourseSectionID] ASC),
        CONSTRAINT [FK_CourseSections_AcademicYear] FOREIGN KEY ([AcademicYearID])
            REFERENCES [dbo].[AcademicYears] ([AcademicYearID]),
        CONSTRAINT [FK_CourseSections_Course] FOREIGN KEY ([CourseID])
            REFERENCES [dbo].[Courses] ([CourseID])
    ) ON [PRIMARY];
END;
GO

IF OBJECT_ID(N'dbo.StudentCourseRegistrations', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StudentCourseRegistrations](
        [UID]              INT IDENTITY(1,1) NOT NULL,
        [StudentID]        INT               NOT NULL,
        [CourseSectionID]  INT               NOT NULL,
        [RegistrationDate] DATE              NOT NULL CONSTRAINT DF_StudentCourseRegistrations_RegistrationDate DEFAULT (CONVERT(date, GETDATE())),
        CONSTRAINT [PK_StudentCourseRegistrations] PRIMARY KEY CLUSTERED ([UID] ASC),
        CONSTRAINT [FK_StudentCourseRegistrations_Student] FOREIGN KEY ([StudentID])
            REFERENCES [dbo].[Students] ([StudentID]),
        CONSTRAINT [FK_StudentCourseRegistrations_CourseSection] FOREIGN KEY ([CourseSectionID])
            REFERENCES [dbo].[CourseSections] ([CourseSectionID])
    ) ON [PRIMARY];
END;
GO

IF OBJECT_ID(N'dbo.Periods', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Periods](
        [PeriodID]   INT IDENTITY(1,1) NOT NULL,
        [PeriodName] VARCHAR(50)       NULL,
        [StartTime]  TIME(0)           NULL,
        [EndTime]    TIME(0)           NULL,
        CONSTRAINT [PK_Periods] PRIMARY KEY CLUSTERED ([PeriodID] ASC)
    ) ON [PRIMARY];
END;
GO

SELECT
    OBJECT_ID(N'dbo.CourseSections', N'U')              AS CourseSectionsExists,
    OBJECT_ID(N'dbo.StudentCourseRegistrations', N'U') AS StudentCourseRegistrationsExists,
    OBJECT_ID(N'dbo.Periods', N'U')                     AS PeriodsExists;
GO
