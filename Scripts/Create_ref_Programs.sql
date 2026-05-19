USE [VEMS];
GO

-- Prefer renaming legacy dbo.Programs (preserves FKs and data) over DROP/CREATE.
IF OBJECT_ID(N'dbo.ref_Programs', N'U') IS NULL AND OBJECT_ID(N'dbo.Programs', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Programs', N'ProgramName') IS NULL AND COL_LENGTH(N'dbo.Programs', N'Name') IS NOT NULL
        EXEC sp_rename N'dbo.Programs.Name', N'ProgramName', N'COLUMN';

    EXEC sp_rename N'dbo.Programs', N'ref_Programs';
    PRINT 'Renamed dbo.Programs to dbo.ref_Programs.';
END
GO

IF OBJECT_ID(N'dbo.ref_Programs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ref_Programs](
        [Uid]              [smallint]      IDENTITY(1,1) NOT NULL,
        [ProgramCode]      [varchar](20)   NOT NULL,
        [ProgramName]      [nvarchar](150) NOT NULL,
        [ShortName]        [nvarchar](50)  NULL,
        [DegreeLevel]      [nvarchar](50)  NOT NULL,
        [DurationYears]    [tinyint]       NOT NULL,
        [TotalSemesters]   [tinyint]       NULL,
        [TotalCreditHours] [smallint]      NULL,
        [DepartmentID]     [smallint]      NULL,
        [FacultyID]        [smallint]      NULL,
        [IsActive]         [bit]           NOT NULL  CONSTRAINT [DF_ref_Programs_IsActive]  DEFAULT (1),
        [Description]      [nvarchar](500) NULL,
        [CreatedBy]        [int]           NOT NULL,
        [CreatedAt]        [datetime2](7)  NOT NULL  CONSTRAINT [DF_ref_Programs_CreatedAt] DEFAULT (sysutcdatetime()),
        [UpdatedBy]        [int]           NULL,
        [UpdatedAt]        [datetime2](7)  NULL,
        CONSTRAINT [PK_ref_Programs]      PRIMARY KEY CLUSTERED  ([Uid] ASC),
        CONSTRAINT [UQ_ref_Programs_Code] UNIQUE NONCLUSTERED    ([ProgramCode] ASC),
        CONSTRAINT [CK_ref_Programs_DegreeLevel] CHECK ([DegreeLevel] IN (
            'Matriculation','Intermediate','BS','MS','PhD',
            'Diploma','Certificate','Associate','Other'
        )),
        CONSTRAINT [CK_ref_Programs_Duration] CHECK ([DurationYears] BETWEEN 1 AND 10)
    ) ON [PRIMARY];

    PRINT 'Created table dbo.ref_Programs.';
END
ELSE
    PRINT 'Table dbo.ref_Programs already exists.';
GO
