/*
    Allow separate fee structures per class/section (e.g. Matric 10-A and 10-B)
    within the same program, semester, and academic year.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.FeeStructures')
      AND name = N'ClassID'
)
BEGIN
    ALTER TABLE dbo.FeeStructures ADD ClassID INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FeeStructures_Class')
BEGIN
    ALTER TABLE dbo.FeeStructures
        ADD CONSTRAINT FK_FeeStructures_Class
        FOREIGN KEY (ClassID) REFERENCES dbo.Classes (Uid);
END;
GO

DECLARE @legacyConstraint sysname;

SELECT TOP (1) @legacyConstraint = kc.name
FROM sys.key_constraints kc
INNER JOIN sys.index_columns ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
INNER JOIN sys.columns c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.FeeStructures')
  AND kc.type = 'UQ'
GROUP BY kc.name
HAVING COUNT(DISTINCT c.name) = 3
   AND SUM(CASE WHEN c.name IN (N'ProgramID', N'Semester', N'AcademicYear') THEN 1 ELSE 0 END) = 3;

IF @legacyConstraint IS NOT NULL
BEGIN
    DECLARE @dropSql nvarchar(400) = N'ALTER TABLE dbo.FeeStructures DROP CONSTRAINT ' + QUOTENAME(@legacyConstraint) + N';';
    EXEC sys.sp_executesql @dropSql;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.FeeStructures')
      AND name = N'UX_FeeStructures_Program_Semester_Year'
      AND is_unique = 1
)
BEGIN
    DROP INDEX UX_FeeStructures_Program_Semester_Year ON dbo.FeeStructures;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.FeeStructures')
      AND name = N'UX_FeeStructures_Program_Semester_Year_NoClass'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_FeeStructures_Program_Semester_Year_NoClass
        ON dbo.FeeStructures (ProgramID, Semester, AcademicYear)
        WHERE ClassID IS NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.FeeStructures')
      AND name = N'UX_FeeStructures_Program_Semester_Year_Class'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_FeeStructures_Program_Semester_Year_Class
        ON dbo.FeeStructures (ProgramID, Semester, AcademicYear, ClassID)
        WHERE ClassID IS NOT NULL;
END;
GO
