USE [VEMS];
GO

IF OBJECT_ID(N'dbo.AcademicYears', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AcademicYears](
        [AcademicYearID] INT      IDENTITY(1,1) NOT NULL,
        [YearName]       VARCHAR(20)            NOT NULL,
        [StartDate]      DATE                   NULL,
        [EndDate]        DATE                   NULL,
        [IsCurrent]      BIT                    NOT NULL CONSTRAINT [DF_AcademicYears_IsCurrent] DEFAULT (0),
        [IsActive]       BIT                    NOT NULL CONSTRAINT [DF_AcademicYears_IsActive] DEFAULT (1),
        [CreatedOn]      DATETIME               NOT NULL CONSTRAINT [DF_AcademicYears_CreatedOn] DEFAULT (GETDATE()),
        CONSTRAINT [PK_AcademicYears] PRIMARY KEY CLUSTERED ([AcademicYearID] ASC)
    ) ON [PRIMARY];

    PRINT 'Created table dbo.AcademicYears.';
END
ELSE
    PRINT 'Table dbo.AcademicYears already exists.';
GO
