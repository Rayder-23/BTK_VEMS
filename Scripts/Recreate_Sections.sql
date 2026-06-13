USE [VEMS];
GO

IF OBJECT_ID(N'dbo.Sections', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Sections](
        [SectionID]   INT IDENTITY(1,1) NOT NULL,
        [SectionName] VARCHAR(20)       NOT NULL,
        [IsActive]    BIT               NOT NULL CONSTRAINT [DF_Sections_IsActive] DEFAULT (1),
        CONSTRAINT [PK_Sections] PRIMARY KEY CLUSTERED ([SectionID] ASC)
    ) ON [PRIMARY];

    PRINT 'Created table dbo.Sections.';
END
ELSE
    PRINT 'Table dbo.Sections already exists.';
GO
