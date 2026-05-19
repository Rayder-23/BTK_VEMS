USE [VEMS];
GO

IF COL_LENGTH(N'dbo.ref_Programs', N'ShortName') IS NULL
    ALTER TABLE dbo.ref_Programs ADD ShortName nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.ref_Programs', N'DegreeLevel') IS NULL
    ALTER TABLE dbo.ref_Programs ADD DegreeLevel nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.ref_Programs', N'DurationYears') IS NULL
    ALTER TABLE dbo.ref_Programs ADD DurationYears tinyint NULL;
IF COL_LENGTH(N'dbo.ref_Programs', N'TotalCreditHours') IS NULL
    ALTER TABLE dbo.ref_Programs ADD TotalCreditHours smallint NULL;
IF COL_LENGTH(N'dbo.ref_Programs', N'CreatedBy') IS NULL
    ALTER TABLE dbo.ref_Programs ADD CreatedBy int NULL;
GO

UPDATE dbo.ref_Programs
SET DegreeLevel = COALESCE(DegreeLevel, ProgramLevel, 'Other'),
    DurationYears = COALESCE(DurationYears, 4),
    CreatedBy = COALESCE(CreatedBy, 1)
WHERE DegreeLevel IS NULL OR DurationYears IS NULL OR CreatedBy IS NULL;
GO

INSERT INTO [dbo].[ref_Programs]
    ([InstTypeId], [ProgramCode], [ProgramName], [ShortName], [DegreeLevel], [DurationYears], [TotalSemesters], [TotalCreditHours], [CreatedBy], [IsActive], [Status])
SELECT v.InstTypeId, v.ProgramCode, v.ProgramName, v.ShortName, v.DegreeLevel, v.DurationYears, v.TotalSemesters, v.TotalCreditHours, v.CreatedBy, 1, N'Active'
FROM (VALUES
    (1, 'MATRIC',   'Secondary School Certificate',            'SSC',     'Matriculation',  2,  NULL, NULL, 1),
    (2, 'INTER',    'Higher Secondary School Certificate',     'HSSC',    'Intermediate',   2,  NULL, NULL, 1),
    (3, 'BS-CS',    'Bachelor of Science in Computer Science', 'BS CS',   'BS',             4,  8,    130,  1),
    (3, 'BS-SE',    'Bachelor of Science in Software Engg',    'BS SE',   'BS',             4,  8,    130,  1),
    (3, 'BS-IT',    'Bachelor of Science in Info Technology',  'BS IT',   'BS',             4,  8,    130,  1),
    (3, 'BS-EE',    'Bachelor of Science in Electrical Engg',  'BS EE',   'BS',             4,  8,    136,  1),
    (3, 'BS-ME',    'Bachelor of Science in Mechanical Engg',  'BS ME',   'BS',             4,  8,    136,  1),
    (3, 'BS-BBA',   'Bachelor of Business Administration',     'BBA',     'BS',             4,  8,    124,  1),
    (3, 'MS-CS',    'Master of Science in Computer Science',   'MS CS',   'MS',             2,  4,    36,   1),
    (3, 'MS-SE',    'Master of Science in Software Engg',      'MS SE',   'MS',             2,  4,    36,   1),
    (3, 'PhD-CS',   'Doctor of Philosophy in Comp Science',    'PhD CS',  'PhD',            3,  NULL, NULL, 1),
    (4, 'DIP-IT',   'Diploma in Information Technology',       'DIP IT',  'Diploma',        1,  2,    60,   1),
    (4, 'CERT-WD',  'Certificate in Web Development',          'CERT WD', 'Certificate',    1,  NULL, 30,   1)
) AS v(InstTypeId, ProgramCode, ProgramName, ShortName, DegreeLevel, DurationYears, TotalSemesters, TotalCreditHours, CreatedBy)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ref_Programs p WHERE p.ProgramCode = v.ProgramCode
);
GO

SELECT COUNT(*) AS inserted_or_skipped FROM dbo.ref_Programs WHERE ProgramCode IN (
    'MATRIC','INTER','BS-CS','BS-SE','BS-IT','BS-EE','BS-ME','BS-BBA','MS-CS','MS-SE','PhD-CS','DIP-IT','CERT-WD'
);
GO
