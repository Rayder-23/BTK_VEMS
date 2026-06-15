USE [VEMS];
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Classes', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Classes](
        [ClassID]   INT IDENTITY(1,1) NOT NULL,
        [ClassCode] VARCHAR(20)       NULL,
        [ClassName] VARCHAR(100)      NOT NULL,
        [SortOrder] INT               NULL,
        [IsActive]  BIT               NOT NULL CONSTRAINT [DF_Classes_IsActive] DEFAULT (1),
        CONSTRAINT [PK_Classes] PRIMARY KEY CLUSTERED ([ClassID] ASC)
    ) ON [PRIMARY];
END;
GO

SELECT OBJECT_ID(N'dbo.Classes', N'U') AS ClassesExists;
GO
