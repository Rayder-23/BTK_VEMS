USE [VEMS];
GO

IF OBJECT_ID(N'dbo.Programs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Programs](
        [ProgramID]     INT IDENTITY(1,1) NOT NULL,
        [ProgramCode]   VARCHAR(20)       NOT NULL,
        [ProgramName]   VARCHAR(200)      NOT NULL,
        [DurationYears] INT               NULL,
        [IsActive]      BIT               NOT NULL CONSTRAINT [DF_Programs_IsActive] DEFAULT (1),
        [CreatedOn]     DATETIME          NOT NULL CONSTRAINT [DF_Programs_CreatedOn] DEFAULT (GETDATE()),
        CONSTRAINT [PK_Programs] PRIMARY KEY CLUSTERED ([ProgramID] ASC),
        CONSTRAINT [UQ_Programs_ProgramCode] UNIQUE NONCLUSTERED ([ProgramCode] ASC)
    ) ON [PRIMARY];

    PRINT 'Created table dbo.Programs.';
END
ELSE
    PRINT 'Table dbo.Programs already exists.';
GO
