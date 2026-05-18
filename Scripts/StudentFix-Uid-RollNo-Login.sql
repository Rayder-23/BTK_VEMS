SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- VEMS - Fix: Uid columns, Students.RollNo, StudentsLogin FK

-- SECTION 1: ref_* tables (Uid mirrors PK; one IDENTITY per table already)
IF COL_LENGTH('dbo.ref_Countries', 'Uid') IS NULL ALTER TABLE dbo.ref_Countries ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.ref_Countries', 'Uid') IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.ref_Countries WHERE [Uid] IS NULL)
    UPDATE dbo.ref_Countries SET [Uid] = CountryID;
GO
IF COL_LENGTH('dbo.ref_Countries', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ref_Countries') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.ref_Countries ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_Countries_Uid')
        ALTER TABLE dbo.ref_Countries ADD CONSTRAINT UQ_ref_Countries_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.ref_Provinces', 'Uid') IS NULL ALTER TABLE dbo.ref_Provinces ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.ref_Provinces', 'Uid') IS NOT NULL
    UPDATE dbo.ref_Provinces SET [Uid] = ProvinceID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.ref_Provinces', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ref_Provinces') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.ref_Provinces ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_Provinces_Uid')
        ALTER TABLE dbo.ref_Provinces ADD CONSTRAINT UQ_ref_Provinces_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.ref_Cities', 'Uid') IS NULL ALTER TABLE dbo.ref_Cities ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.ref_Cities', 'Uid') IS NOT NULL
    UPDATE dbo.ref_Cities SET [Uid] = CityID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.ref_Cities', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ref_Cities') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.ref_Cities ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_Cities_Uid')
        ALTER TABLE dbo.ref_Cities ADD CONSTRAINT UQ_ref_Cities_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.ref_InstitutionTypes', 'Uid') IS NULL ALTER TABLE dbo.ref_InstitutionTypes ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.ref_InstitutionTypes', 'Uid') IS NOT NULL
    UPDATE dbo.ref_InstitutionTypes SET [Uid] = InstTypeID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.ref_InstitutionTypes', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ref_InstitutionTypes') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.ref_InstitutionTypes ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_InstTypes_Uid')
        ALTER TABLE dbo.ref_InstitutionTypes ADD CONSTRAINT UQ_ref_InstTypes_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.ref_Programs', 'Uid') IS NULL ALTER TABLE dbo.ref_Programs ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.ref_Programs', 'Uid') IS NOT NULL
    UPDATE dbo.ref_Programs SET [Uid] = ProgramID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.ref_Programs', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ref_Programs') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.ref_Programs ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_Programs_Uid')
        ALTER TABLE dbo.ref_Programs ADD CONSTRAINT UQ_ref_Programs_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.ref_BloodGroups', 'Uid') IS NULL ALTER TABLE dbo.ref_BloodGroups ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.ref_BloodGroups', 'Uid') IS NOT NULL
    UPDATE dbo.ref_BloodGroups SET [Uid] = BloodGroupID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.ref_BloodGroups', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ref_BloodGroups') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.ref_BloodGroups ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_BloodGroups_Uid')
        ALTER TABLE dbo.ref_BloodGroups ADD CONSTRAINT UQ_ref_BloodGroups_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.ref_Religions', 'Uid') IS NULL ALTER TABLE dbo.ref_Religions ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.ref_Religions', 'Uid') IS NOT NULL
    UPDATE dbo.ref_Religions SET [Uid] = ReligionID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.ref_Religions', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ref_Religions') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.ref_Religions ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_Religions_Uid')
        ALTER TABLE dbo.ref_Religions ADD CONSTRAINT UQ_ref_Religions_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.ref_RelationTypes', 'Uid') IS NULL ALTER TABLE dbo.ref_RelationTypes ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.ref_RelationTypes', 'Uid') IS NOT NULL
    UPDATE dbo.ref_RelationTypes SET [Uid] = RelationTypeID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.ref_RelationTypes', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ref_RelationTypes') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.ref_RelationTypes ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_RelationTypes_Uid')
        ALTER TABLE dbo.ref_RelationTypes ADD CONSTRAINT UQ_ref_RelationTypes_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.ref_ContactTypes', 'Uid') IS NULL ALTER TABLE dbo.ref_ContactTypes ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.ref_ContactTypes', 'Uid') IS NOT NULL
    UPDATE dbo.ref_ContactTypes SET [Uid] = ContactTypeID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.ref_ContactTypes', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ref_ContactTypes') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.ref_ContactTypes ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_ContactTypes_Uid')
        ALTER TABLE dbo.ref_ContactTypes ADD CONSTRAINT UQ_ref_ContactTypes_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.ref_Sections', 'Uid') IS NULL ALTER TABLE dbo.ref_Sections ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.ref_Sections', 'Uid') IS NOT NULL
    UPDATE dbo.ref_Sections SET [Uid] = SectionID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.ref_Sections', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ref_Sections') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.ref_Sections ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_ref_Sections_Uid')
        ALTER TABLE dbo.ref_Sections ADD CONSTRAINT UQ_ref_Sections_Uid UNIQUE ([Uid]);
END;
GO

-- SECTION 2: student tables
IF COL_LENGTH('dbo.Students', 'Uid') IS NULL ALTER TABLE dbo.Students ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.Students', 'Uid') IS NOT NULL
    UPDATE dbo.Students SET [Uid] = StudentID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.Students', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Students') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.Students ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_Students_Uid')
        ALTER TABLE dbo.Students ADD CONSTRAINT UQ_Students_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.StudentContacts', 'Uid') IS NULL ALTER TABLE dbo.StudentContacts ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.StudentContacts', 'Uid') IS NOT NULL
    UPDATE dbo.StudentContacts SET [Uid] = ContactID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.StudentContacts', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.StudentContacts') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.StudentContacts ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_StudentContacts_Uid')
        ALTER TABLE dbo.StudentContacts ADD CONSTRAINT UQ_StudentContacts_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.StudentParents', 'Uid') IS NULL ALTER TABLE dbo.StudentParents ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.StudentParents', 'Uid') IS NOT NULL
    UPDATE dbo.StudentParents SET [Uid] = ParentID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.StudentParents', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.StudentParents') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.StudentParents ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_StudentParents_Uid')
        ALTER TABLE dbo.StudentParents ADD CONSTRAINT UQ_StudentParents_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.StudentSiblings', 'Uid') IS NULL ALTER TABLE dbo.StudentSiblings ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.StudentSiblings', 'Uid') IS NOT NULL
    UPDATE dbo.StudentSiblings SET [Uid] = SiblingID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.StudentSiblings', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.StudentSiblings') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.StudentSiblings ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_StudentSiblings_Uid')
        ALTER TABLE dbo.StudentSiblings ADD CONSTRAINT UQ_StudentSiblings_Uid UNIQUE ([Uid]);
END;
GO

IF COL_LENGTH('dbo.StudentEnrollments', 'Uid') IS NULL ALTER TABLE dbo.StudentEnrollments ADD [Uid] INT NULL;
GO
IF COL_LENGTH('dbo.StudentEnrollments', 'Uid') IS NOT NULL
    UPDATE dbo.StudentEnrollments SET [Uid] = EnrollmentID WHERE [Uid] IS NULL;
GO
IF COL_LENGTH('dbo.StudentEnrollments', 'Uid') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.StudentEnrollments') AND name = 'Uid' AND is_nullable = 1)
        ALTER TABLE dbo.StudentEnrollments ALTER COLUMN [Uid] INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_StudentEnrollments_Uid')
        ALTER TABLE dbo.StudentEnrollments ADD CONSTRAINT UQ_StudentEnrollments_Uid UNIQUE ([Uid]);
END;
GO

-- SECTION 3: RollNo
IF COL_LENGTH('dbo.Students', 'RollNo') IS NULL ALTER TABLE dbo.Students ADD [RollNo] NVARCHAR(30) NULL;
GO
UPDATE s SET s.RollNo = e.RollNo
FROM dbo.Students s
JOIN dbo.StudentEnrollments e ON s.StudentID = e.StudentID AND e.EnrollmentStatus = 'Active';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Students_RollNo' AND object_id = OBJECT_ID('dbo.Students'))
    CREATE INDEX IX_Students_RollNo ON dbo.Students (RollNo) WHERE RollNo IS NOT NULL;
GO

-- SECTION 4: StudentsLogin FK
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StudentsLogin_Student')
    ALTER TABLE dbo.StudentsLogin DROP CONSTRAINT FK_StudentsLogin_Student;
GO
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StudentsLogin_Students')
    ALTER TABLE dbo.StudentsLogin DROP CONSTRAINT FK_StudentsLogin_Students;
GO
DELETE sl FROM dbo.StudentsLogin sl
WHERE NOT EXISTS (SELECT 1 FROM dbo.Students s WHERE s.StudentID = sl.StudentId);
GO
ALTER TABLE dbo.StudentsLogin ALTER COLUMN [StudentId] INT NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StudentsLogin_Students')
    ALTER TABLE dbo.StudentsLogin ADD CONSTRAINT FK_StudentsLogin_Students
        FOREIGN KEY ([StudentId]) REFERENCES dbo.Students ([StudentID]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_StudentsLogin_StudentId')
    ALTER TABLE dbo.StudentsLogin ADD CONSTRAINT UQ_StudentsLogin_StudentId UNIQUE ([StudentId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentsLogin_StudentId' AND object_id = OBJECT_ID('dbo.StudentsLogin'))
    CREATE INDEX IX_StudentsLogin_StudentId ON dbo.StudentsLogin ([StudentId]);
GO

-- SECTION 5: VERIFY
SELECT t.name AS TableName, c.name AS ColumnName, tp.name AS DataType, c.is_nullable AS IsNullable
FROM sys.tables t
JOIN sys.columns c ON t.object_id = c.object_id
JOIN sys.types tp ON c.user_type_id = tp.user_type_id
WHERE t.name IN (
    'ref_Countries','ref_Provinces','ref_Cities','ref_InstitutionTypes','ref_Programs',
    'ref_BloodGroups','ref_Religions','ref_RelationTypes','ref_ContactTypes','ref_Sections',
    'Students','StudentContacts','StudentParents','StudentSiblings','StudentEnrollments','StudentsLogin')
  AND c.name = 'Uid'
ORDER BY t.name;

SELECT fk.name AS ForeignKeyName, cp.name AS ParentColumn, tr.name AS ReferencedTable, cr.name AS ReferencedColumn
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
WHERE tp.name = 'StudentsLogin' AND fk.name = 'FK_StudentsLogin_Students';

SELECT StudentID, RegistrationNo, FirstName + ' ' + LastName AS StudentName, RollNo, [Uid]
FROM dbo.Students ORDER BY StudentID;
GO
