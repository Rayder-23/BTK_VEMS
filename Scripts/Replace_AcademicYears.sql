USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#AcademicYears_Backup') IS NOT NULL
    DROP TABLE #AcademicYears_Backup;

SELECT
    AcademicYearID,
    AcademicYearName,
    StartDate,
    EndDate,
    IsCurrent,
    IsActive
INTO #AcademicYears_Backup
FROM dbo.AcademicYear;
GO

IF OBJECT_ID(N'dbo.AcademicYear', N'U') IS NOT NULL
    DROP TABLE dbo.AcademicYear;
GO

IF OBJECT_ID(N'dbo.AcademicYears', N'U') IS NOT NULL
    DROP TABLE dbo.AcademicYears;
GO

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
GO

SET IDENTITY_INSERT dbo.AcademicYears ON;

INSERT INTO dbo.AcademicYears (AcademicYearID, YearName, StartDate, EndDate, IsCurrent, IsActive, CreatedOn)
SELECT
    AcademicYearID,
    LEFT(COALESCE(NULLIF(LTRIM(RTRIM(AcademicYearName)), ''), CONCAT('Year-', AcademicYearID)), 20),
    StartDate,
    EndDate,
    COALESCE(IsCurrent, 0),
    COALESCE(IsActive, 1),
    GETDATE()
FROM #AcademicYears_Backup;

SET IDENTITY_INSERT dbo.AcademicYears OFF;
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS AcademicYearsRows FROM dbo.AcademicYears;
