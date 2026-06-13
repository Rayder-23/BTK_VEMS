USE [VEMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Sections_Program')
    ALTER TABLE dbo.Sections DROP CONSTRAINT FK_Sections_Program;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Students_Section')
    ALTER TABLE dbo.Students DROP CONSTRAINT FK_Students_Section;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TeacherSections_Section')
    ALTER TABLE dbo.TeacherSections DROP CONSTRAINT FK_TeacherSections_Section;
GO

IF OBJECT_ID('tempdb..#Sections_Backup') IS NOT NULL
    DROP TABLE #Sections_Backup;

SELECT
    Uid,
    SectionName,
    IsActive
INTO #Sections_Backup
FROM dbo.Sections;
GO

IF OBJECT_ID(N'dbo.Sections', N'U') IS NOT NULL
    DROP TABLE dbo.Sections;
GO

CREATE TABLE [dbo].[Sections](
    [SectionID]   INT IDENTITY(1,1) NOT NULL,
    [SectionName] VARCHAR(20)       NOT NULL,
    [IsActive]    BIT               NOT NULL CONSTRAINT [DF_Sections_IsActive] DEFAULT (1),
    CONSTRAINT [PK_Sections] PRIMARY KEY CLUSTERED ([SectionID] ASC)
) ON [PRIMARY];
GO

SET IDENTITY_INSERT dbo.Sections ON;

INSERT INTO dbo.Sections (SectionID, SectionName, IsActive)
SELECT
    Uid,
    LEFT(CAST(SectionName AS VARCHAR(20)), 20),
    IsActive
FROM #Sections_Backup;

SET IDENTITY_INSERT dbo.Sections OFF;
GO

ALTER TABLE dbo.Students
    ADD CONSTRAINT FK_Students_Section FOREIGN KEY (SectionId) REFERENCES dbo.Sections (SectionID) ON DELETE SET NULL;
ALTER TABLE dbo.TeacherSections
    ADD CONSTRAINT FK_TeacherSections_Section FOREIGN KEY (SectionId) REFERENCES dbo.Sections (SectionID);
GO

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS SectionsRows FROM dbo.Sections;
