/* Add billing period labels to Challans (idempotent). */
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.Challans', 'ChallanMonth') IS NULL
    ALTER TABLE dbo.Challans ADD ChallanMonth NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.Challans', 'ChallanYear') IS NULL
    ALTER TABLE dbo.Challans ADD ChallanYear NVARCHAR(9) NULL;
GO

UPDATE dbo.Challans
SET
    ChallanMonth = COALESCE(NULLIF(LTRIM(RTRIM(ChallanMonth)), N''), DATENAME(MONTH, IssueDate)),
    ChallanYear = COALESCE(NULLIF(LTRIM(RTRIM(ChallanYear)), N''), CAST(YEAR(IssueDate) AS NVARCHAR(9)))
WHERE ChallanMonth IS NULL
   OR ChallanYear IS NULL;
GO

PRINT 'Challans billing period columns migration complete.';
GO
