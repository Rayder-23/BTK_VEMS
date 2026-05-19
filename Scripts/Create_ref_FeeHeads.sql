USE [VEMS];
GO

-- If legacy table dbo.FeeHeads exists (same schema, wrong name), rename it instead of recreating.
IF OBJECT_ID(N'dbo.ref_FeeHeads', N'U') IS NULL AND OBJECT_ID(N'dbo.FeeHeads', N'U') IS NOT NULL
BEGIN
    EXEC sp_rename N'dbo.FeeHeads', N'ref_FeeHeads';
    PRINT 'Renamed dbo.FeeHeads to dbo.ref_FeeHeads.';
END
GO

IF OBJECT_ID(N'dbo.ref_FeeHeads', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ref_FeeHeads](
        [Uid]          [smallint] IDENTITY(1,1) NOT NULL,
        [HeadCode]     [varchar](20)    NOT NULL,
        [HeadName]     [nvarchar](100)  NOT NULL,
        [Category]     [nvarchar](50)   NOT NULL,
        [IsMandatory]  [bit]            NOT NULL   CONSTRAINT [DF_ref_FeeHeads_IsMandatory] DEFAULT (1),
        [IsActive]     [bit]            NOT NULL   CONSTRAINT [DF_ref_FeeHeads_IsActive]    DEFAULT (1),
        [Description]  [nvarchar](300)  NULL,
        [CreatedBy]    [int]            NOT NULL,
        [CreatedAt]    [datetime2](7)   NOT NULL   CONSTRAINT [DF_ref_FeeHeads_CreatedAt]   DEFAULT (sysutcdatetime()),
        [UpdatedBy]    [int]            NULL,
        [UpdatedAt]    [datetime2](7)   NULL,
        CONSTRAINT [PK_ref_FeeHeads] PRIMARY KEY CLUSTERED ([Uid] ASC),
        CONSTRAINT [UQ_ref_FeeHeads_Code] UNIQUE NONCLUSTERED ([HeadCode] ASC)
    ) ON [PRIMARY];

    PRINT 'Created table dbo.ref_FeeHeads.';
END
ELSE
BEGIN
    PRINT 'Table dbo.ref_FeeHeads already exists.';
END
GO
