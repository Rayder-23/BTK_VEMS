SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- ============================================================
--  PREP: Legacy VEMS objects (Students_ and fee/login FKs)
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Challans_Students')
    ALTER TABLE dbo.Challans DROP CONSTRAINT FK_Challans_Students;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StudentsLogin_Students')
    ALTER TABLE dbo.StudentsLogin DROP CONSTRAINT FK_StudentsLogin_Students;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SFA_Student')
    ALTER TABLE dbo.StudentFeeAllocations DROP CONSTRAINT FK_SFA_Student;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Students_CreatedBy')
    ALTER TABLE dbo.Students_ DROP CONSTRAINT FK_Students_CreatedBy;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Students_ModifiedBy')
    ALTER TABLE dbo.Students_ DROP CONSTRAINT FK_Students_ModifiedBy;

DROP TABLE IF EXISTS dbo.Students_;
GO

-- ============================================================
--  VEMS - Education Management System
--  Student Tables - SQL Server
--  Version 3 - Explicitly drops FK constraints before tables
-- ============================================================

-- ============================================================
--  SECTION 1: DROP FOREIGN KEY CONSTRAINTS EXPLICITLY
--  Must drop FK constraints before dropping tables
-- ============================================================

-- StudentEnrollments FKs
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Enrollments_Student')
    ALTER TABLE StudentEnrollments DROP CONSTRAINT FK_Enrollments_Student;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Enrollments_Program')
    ALTER TABLE StudentEnrollments DROP CONSTRAINT FK_Enrollments_Program;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Enrollments_Section')
    ALTER TABLE StudentEnrollments DROP CONSTRAINT FK_Enrollments_Section;

-- StudentSiblings FKs
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Siblings_Student')
    ALTER TABLE StudentSiblings DROP CONSTRAINT FK_Siblings_Student;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Siblings_Sibling')
    ALTER TABLE StudentSiblings DROP CONSTRAINT FK_Siblings_Sibling;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Siblings_Relation')
    ALTER TABLE StudentSiblings DROP CONSTRAINT FK_Siblings_Relation;

-- StudentParents FKs
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Parents_Student')
    ALTER TABLE StudentParents DROP CONSTRAINT FK_Parents_Student;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Parents_Relation')
    ALTER TABLE StudentParents DROP CONSTRAINT FK_Parents_Relation;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Parents_City')
    ALTER TABLE StudentParents DROP CONSTRAINT FK_Parents_City;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Parents_Province')
    ALTER TABLE StudentParents DROP CONSTRAINT FK_Parents_Province;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Parents_Country')
    ALTER TABLE StudentParents DROP CONSTRAINT FK_Parents_Country;

-- StudentContacts FKs
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contacts_Student')
    ALTER TABLE StudentContacts DROP CONSTRAINT FK_Contacts_Student;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contacts_Type')
    ALTER TABLE StudentContacts DROP CONSTRAINT FK_Contacts_Type;

-- Students FKs
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Students_Program')
    ALTER TABLE Students DROP CONSTRAINT FK_Students_Program;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Students_City')
    ALTER TABLE Students DROP CONSTRAINT FK_Students_City;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Students_Province')
    ALTER TABLE Students DROP CONSTRAINT FK_Students_Province;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Students_Country')
    ALTER TABLE Students DROP CONSTRAINT FK_Students_Country;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Students_Blood')
    ALTER TABLE Students DROP CONSTRAINT FK_Students_Blood;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Students_Religion')
    ALTER TABLE Students DROP CONSTRAINT FK_Students_Religion;

-- ref_Programs FKs
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Programs_InstType')
    ALTER TABLE ref_Programs DROP CONSTRAINT FK_Programs_InstType;

-- ref_Cities FKs
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Cities_Province')
    ALTER TABLE ref_Cities DROP CONSTRAINT FK_Cities_Province;

-- ref_Provinces FKs
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Provinces_Country')
    ALTER TABLE ref_Provinces DROP CONSTRAINT FK_Provinces_Country;

GO

-- ============================================================
--  SECTION 2: DROP VIEWS
-- ============================================================

DROP VIEW IF EXISTS vw_StudentParentContact;
DROP VIEW IF EXISTS vw_StudentProfile;
GO

-- ============================================================
--  SECTION 3: DROP TABLES (safe now - no FK constraints left)
-- ============================================================

DROP TABLE IF EXISTS StudentEnrollments;
DROP TABLE IF EXISTS StudentSiblings;
DROP TABLE IF EXISTS StudentParents;
DROP TABLE IF EXISTS StudentContacts;
DROP TABLE IF EXISTS Students;
DROP TABLE IF EXISTS ref_Sections;
DROP TABLE IF EXISTS ref_ContactTypes;
DROP TABLE IF EXISTS ref_RelationTypes;
DROP TABLE IF EXISTS ref_Religions;
DROP TABLE IF EXISTS ref_BloodGroups;
DROP TABLE IF EXISTS ref_Programs;
DROP TABLE IF EXISTS ref_InstitutionTypes;
DROP TABLE IF EXISTS ref_Cities;
DROP TABLE IF EXISTS ref_Provinces;
DROP TABLE IF EXISTS ref_Countries;
GO

-- ============================================================
--  SECTION 4: LOOKUP / REFERENCE TABLES
-- ============================================================

CREATE TABLE ref_Countries (
    CountryID       TINYINT         NOT NULL IDENTITY(1,1),
    CountryName     NVARCHAR(100)   NOT NULL,
    CountryCode     CHAR(3)         NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_ref_Countries      PRIMARY KEY (CountryID),
    CONSTRAINT UQ_ref_Countries_Code UNIQUE (CountryCode)
);
GO

CREATE TABLE ref_Provinces (
    ProvinceID      SMALLINT        NOT NULL IDENTITY(1,1),
    CountryID       TINYINT         NOT NULL,
    ProvinceName    NVARCHAR(100)   NOT NULL,
    ProvinceCode    NVARCHAR(10)    NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_ref_Provinces      PRIMARY KEY (ProvinceID),
    CONSTRAINT FK_Provinces_Country  FOREIGN KEY (CountryID)
        REFERENCES ref_Countries (CountryID)
);
GO

CREATE TABLE ref_Cities (
    CityID          SMALLINT        NOT NULL IDENTITY(1,1),
    ProvinceID      SMALLINT        NOT NULL,
    CityName        NVARCHAR(100)   NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_ref_Cities         PRIMARY KEY (CityID),
    CONSTRAINT FK_Cities_Province    FOREIGN KEY (ProvinceID)
        REFERENCES ref_Provinces (ProvinceID)
);
GO

CREATE TABLE ref_InstitutionTypes (
    InstTypeID      TINYINT         NOT NULL IDENTITY(1,1),
    InstTypeCode    CHAR(3)         NOT NULL,
    InstTypeName    NVARCHAR(50)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_ref_InstTypes      PRIMARY KEY (InstTypeID),
    CONSTRAINT UQ_ref_InstTypes_Code UNIQUE (InstTypeCode)
);
GO

CREATE TABLE ref_Programs (
    ProgramID       SMALLINT        NOT NULL IDENTITY(1,1),
    InstTypeID      TINYINT         NOT NULL,
    ProgramCode     NVARCHAR(10)    NOT NULL,
    ProgramName     NVARCHAR(100)   NOT NULL,
    TotalSemesters  TINYINT         NULL,
    TotalGrades     TINYINT         NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_ref_Programs       PRIMARY KEY (ProgramID),
    CONSTRAINT UQ_ref_Programs_Code  UNIQUE (InstTypeID, ProgramCode),
    CONSTRAINT FK_Programs_InstType  FOREIGN KEY (InstTypeID)
        REFERENCES ref_InstitutionTypes (InstTypeID)
);
GO

CREATE TABLE ref_BloodGroups (
    BloodGroupID    TINYINT         NOT NULL IDENTITY(1,1),
    BloodGroupName  VARCHAR(5)      NOT NULL,
    CONSTRAINT PK_ref_BloodGroups    PRIMARY KEY (BloodGroupID)
);
GO

CREATE TABLE ref_Religions (
    ReligionID      TINYINT         NOT NULL IDENTITY(1,1),
    ReligionName    NVARCHAR(50)    NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_ref_Religions      PRIMARY KEY (ReligionID)
);
GO

CREATE TABLE ref_RelationTypes (
    RelationTypeID  TINYINT         NOT NULL IDENTITY(1,1),
    RelationName    NVARCHAR(50)    NOT NULL,
    CONSTRAINT PK_ref_RelationTypes  PRIMARY KEY (RelationTypeID)
);
GO

CREATE TABLE ref_ContactTypes (
    ContactTypeID   TINYINT         NOT NULL IDENTITY(1,1),
    ContactTypeName NVARCHAR(30)    NOT NULL,
    CONSTRAINT PK_ref_ContactTypes   PRIMARY KEY (ContactTypeID)
);
GO

CREATE TABLE ref_Sections (
    SectionID       TINYINT         NOT NULL IDENTITY(1,1),
    SectionName     NVARCHAR(10)    NOT NULL,
    CONSTRAINT PK_ref_Sections       PRIMARY KEY (SectionID)
);
GO

-- ============================================================
--  SECTION 5: CORE STUDENT TABLE
-- ============================================================

CREATE TABLE Students (
    StudentID           INT             NOT NULL IDENTITY(1,1),
    RegistrationNo      NVARCHAR(30)    NOT NULL,
    ProgramID           SMALLINT        NOT NULL,
    AdmissionYear       SMALLINT        NOT NULL,
    AdmissionDate       DATE            NOT NULL,

    FirstName           NVARCHAR(50)    NOT NULL,
    MiddleName          NVARCHAR(50)    NULL,
    LastName            NVARCHAR(50)    NOT NULL,
    FatherName          NVARCHAR(100)   NOT NULL,
    DateOfBirth         DATE            NOT NULL,
    Gender              CHAR(1)         NOT NULL,
    BloodGroupID        TINYINT         NULL,
    ReligionID          TINYINT         NULL,
    Nationality         NVARCHAR(50)    NULL DEFAULT 'Pakistani',

    NIC_No              VARCHAR(15)     NULL,
    BFORM_No            VARCHAR(15)     NULL,
    PassportNo          NVARCHAR(20)    NULL,

    AddressLine1        NVARCHAR(150)   NOT NULL,
    AddressLine2        NVARCHAR(150)   NULL,
    CityID              SMALLINT        NOT NULL,
    ProvinceID          SMALLINT        NOT NULL,
    CountryID           TINYINT         NOT NULL DEFAULT 1,
    PostalCode          NVARCHAR(10)    NULL,

    PhotoPath           NVARCHAR(300)   NULL,
    DocumentPath        NVARCHAR(300)   NULL,

    PreviousSchool      NVARCHAR(150)   NULL,
    PreviousGradeOrSem  NVARCHAR(20)    NULL,
    TransferCertNo      NVARCHAR(50)    NULL,

    HasSpecialNeeds     BIT             NOT NULL DEFAULT 0,
    SpecialNeedsDetail  NVARCHAR(300)   NULL,

    IsActive            BIT             NOT NULL DEFAULT 1,
    StatusRemark        NVARCHAR(200)   NULL,

    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy           INT             NULL,
    UpdatedAt           DATETIME2       NULL,

    CONSTRAINT PK_Students           PRIMARY KEY (StudentID),
    CONSTRAINT UQ_Students_RegNo     UNIQUE      (RegistrationNo),
    CONSTRAINT CK_Students_Gender    CHECK       (Gender IN ('M','F','O')),
    CONSTRAINT CK_Students_DOB       CHECK       (DateOfBirth < CAST(GETDATE() AS DATE)),
    CONSTRAINT FK_Students_Program   FOREIGN KEY (ProgramID)   REFERENCES ref_Programs      (ProgramID),
    CONSTRAINT FK_Students_City      FOREIGN KEY (CityID)      REFERENCES ref_Cities        (CityID),
    CONSTRAINT FK_Students_Province  FOREIGN KEY (ProvinceID)  REFERENCES ref_Provinces     (ProvinceID),
    CONSTRAINT FK_Students_Country   FOREIGN KEY (CountryID)   REFERENCES ref_Countries     (CountryID),
    CONSTRAINT FK_Students_Blood     FOREIGN KEY (BloodGroupID)REFERENCES ref_BloodGroups   (BloodGroupID),
    CONSTRAINT FK_Students_Religion  FOREIGN KEY (ReligionID)  REFERENCES ref_Religions     (ReligionID)
);
GO

CREATE INDEX IX_Students_Name        ON Students (LastName, FirstName);
CREATE INDEX IX_Students_DOB         ON Students (DateOfBirth);
CREATE INDEX IX_Students_ProgramYear ON Students (ProgramID, AdmissionYear);
CREATE INDEX IX_Students_NIC         ON Students (NIC_No)   WHERE NIC_No   IS NOT NULL;
CREATE INDEX IX_Students_BForm       ON Students (BFORM_No) WHERE BFORM_No IS NOT NULL;
GO

-- ============================================================
--  SECTION 6: STUDENT CONTACT INFORMATION
-- ============================================================

CREATE TABLE StudentContacts (
    ContactID       INT             NOT NULL IDENTITY(1,1),
    StudentID       INT             NOT NULL,
    ContactTypeID   TINYINT         NOT NULL,
    ContactValue    NVARCHAR(150)   NOT NULL,
    IsPrimary       BIT             NOT NULL DEFAULT 0,
    IsVerified      BIT             NOT NULL DEFAULT 0,
    Remarks         NVARCHAR(100)   NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,

    CreatedBy       INT             NOT NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy       INT             NULL,
    UpdatedAt       DATETIME2       NULL,

    CONSTRAINT PK_StudentContacts    PRIMARY KEY (ContactID),
    CONSTRAINT FK_Contacts_Student   FOREIGN KEY (StudentID)      REFERENCES Students        (StudentID),
    CONSTRAINT FK_Contacts_Type      FOREIGN KEY (ContactTypeID)  REFERENCES ref_ContactTypes(ContactTypeID)
);
GO

CREATE INDEX IX_Contacts_Student ON StudentContacts (StudentID);
GO

-- ============================================================
--  SECTION 7: STUDENT PARENTS / GUARDIAN
-- ============================================================

CREATE TABLE StudentParents (
    ParentID            INT             NOT NULL IDENTITY(1,1),
    StudentID           INT             NOT NULL,
    RelationTypeID      TINYINT         NOT NULL,

    FirstName           NVARCHAR(50)    NOT NULL,
    LastName            NVARCHAR(50)    NOT NULL,
    DateOfBirth         DATE            NULL,
    NIC_No              VARCHAR(15)     NULL,
    Nationality         NVARCHAR(50)    NULL,

    Education           NVARCHAR(100)   NULL,
    Occupation          NVARCHAR(100)   NULL,
    OrganizationName    NVARCHAR(150)   NULL,
    Designation         NVARCHAR(100)   NULL,
    MonthlyIncome       DECIMAL(12,2)   NULL,

    AddressLine1        NVARCHAR(150)   NULL,
    AddressLine2        NVARCHAR(150)   NULL,
    CityID              SMALLINT        NULL,
    ProvinceID          SMALLINT        NULL,
    CountryID           TINYINT         NULL,

    MobileNo            NVARCHAR(20)    NULL,
    WhatsAppNo          NVARCHAR(20)    NULL,
    EmailAddress        NVARCHAR(150)   NULL,
    OfficeNo            NVARCHAR(20)    NULL,

    IsPrimaryContact    BIT             NOT NULL DEFAULT 0,
    IsAuthorizedPickup  BIT             NOT NULL DEFAULT 1,
    PhotoPath           NVARCHAR(300)   NULL,
    IsAlive             BIT             NOT NULL DEFAULT 1,
    IsActive            BIT             NOT NULL DEFAULT 1,

    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy           INT             NULL,
    UpdatedAt           DATETIME2       NULL,

    CONSTRAINT PK_StudentParents     PRIMARY KEY (ParentID),
    CONSTRAINT FK_Parents_Student    FOREIGN KEY (StudentID)      REFERENCES Students     (StudentID),
    CONSTRAINT FK_Parents_Relation   FOREIGN KEY (RelationTypeID) REFERENCES ref_RelationTypes(RelationTypeID),
    CONSTRAINT FK_Parents_City       FOREIGN KEY (CityID)         REFERENCES ref_Cities   (CityID),
    CONSTRAINT FK_Parents_Province   FOREIGN KEY (ProvinceID)     REFERENCES ref_Provinces(ProvinceID),
    CONSTRAINT FK_Parents_Country    FOREIGN KEY (CountryID)      REFERENCES ref_Countries(CountryID)
);
GO

CREATE INDEX IX_Parents_Student ON StudentParents (StudentID);
CREATE INDEX IX_Parents_NIC     ON StudentParents (NIC_No) WHERE NIC_No IS NOT NULL;
GO

-- ============================================================
--  SECTION 8: SIBLING INFORMATION
-- ============================================================

CREATE TABLE StudentSiblings (
    SiblingID           INT             NOT NULL IDENTITY(1,1),
    StudentID           INT             NOT NULL,
    SiblingStudentID    INT             NULL,

    FirstName           NVARCHAR(50)    NULL,
    LastName            NVARCHAR(50)    NULL,
    DateOfBirth         DATE            NULL,
    Gender              CHAR(1)         NULL,
    RelationTypeID      TINYINT         NOT NULL,

    InstitutionName     NVARCHAR(150)   NULL,
    GradeOrClass        NVARCHAR(20)    NULL,

    IsActive            BIT             NOT NULL DEFAULT 1,

    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy           INT             NULL,
    UpdatedAt           DATETIME2       NULL,

    CONSTRAINT PK_StudentSiblings    PRIMARY KEY (SiblingID),
    CONSTRAINT FK_Siblings_Student   FOREIGN KEY (StudentID)         REFERENCES Students       (StudentID),
    CONSTRAINT FK_Siblings_Sibling   FOREIGN KEY (SiblingStudentID)  REFERENCES Students       (StudentID),
    CONSTRAINT FK_Siblings_Relation  FOREIGN KEY (RelationTypeID)    REFERENCES ref_RelationTypes(RelationTypeID),
    CONSTRAINT CK_Siblings_Gender    CHECK (Gender IN ('M','F','O')),
    CONSTRAINT CK_Siblings_NotSelf   CHECK (StudentID <> SiblingStudentID)
);
GO

CREATE INDEX IX_Siblings_Student ON StudentSiblings (StudentID);
CREATE INDEX IX_Siblings_Linked  ON StudentSiblings (SiblingStudentID) WHERE SiblingStudentID IS NOT NULL;
GO

-- ============================================================
--  SECTION 9: STUDENT ENROLLMENTS & ROLL NUMBER
-- ============================================================

CREATE TABLE StudentEnrollments (
    EnrollmentID        INT             NOT NULL IDENTITY(1,1),
    StudentID           INT             NOT NULL,
    ProgramID           SMALLINT        NOT NULL,
    AcademicYear        SMALLINT        NOT NULL,
    GradeOrSemester     TINYINT         NOT NULL,
    SectionID           TINYINT         NULL,
    RollNo              NVARCHAR(30)    NOT NULL,
    EnrollmentDate      DATE            NOT NULL,
    CompletionDate      DATE            NULL,
    EnrollmentStatus    NVARCHAR(20)    NOT NULL DEFAULT 'Active',
    FeeStatus           NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
    Remarks             NVARCHAR(300)   NULL,

    CreatedBy           INT             NOT NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy           INT             NULL,
    UpdatedAt           DATETIME2       NULL,

    CONSTRAINT PK_Enrollments          PRIMARY KEY (EnrollmentID),
    CONSTRAINT UQ_Enrollments_RollNo   UNIQUE (ProgramID, AcademicYear, GradeOrSemester, RollNo),
    CONSTRAINT UQ_Enrollments_Period   UNIQUE (StudentID, ProgramID, AcademicYear, GradeOrSemester),
    CONSTRAINT FK_Enrollments_Student  FOREIGN KEY (StudentID)  REFERENCES Students    (StudentID),
    CONSTRAINT FK_Enrollments_Program  FOREIGN KEY (ProgramID)  REFERENCES ref_Programs(ProgramID),
    CONSTRAINT FK_Enrollments_Section  FOREIGN KEY (SectionID)  REFERENCES ref_Sections(SectionID),
    CONSTRAINT CK_Enrollments_Status   CHECK (EnrollmentStatus IN
        ('Active','Passed','Failed','Withdrawn','Transferred','Expelled')),
    CONSTRAINT CK_Enrollments_Fee      CHECK (FeeStatus IN
        ('Pending','Paid','Partial','Waived'))
);
GO

CREATE INDEX IX_Enrollments_Student   ON StudentEnrollments (StudentID);
CREATE INDEX IX_Enrollments_RollNo    ON StudentEnrollments (RollNo);
CREATE INDEX IX_Enrollments_YearGrade ON StudentEnrollments (AcademicYear, GradeOrSemester);
GO

-- ============================================================
--  SECTION 10: VIEWS
-- ============================================================

CREATE VIEW vw_StudentProfile AS
SELECT
    s.StudentID,
    s.RegistrationNo,
    s.FirstName + ' ' + ISNULL(s.MiddleName + ' ', '') + s.LastName  AS FullName,
    s.FirstName,
    s.MiddleName,
    s.LastName,
    s.FatherName,
    s.DateOfBirth,
    DATEDIFF(YEAR, s.DateOfBirth, GETDATE())                          AS Age,
    s.Gender,
    bg.BloodGroupName,
    r.ReligionName,
    s.Nationality,
    s.NIC_No,
    s.BFORM_No,
    s.PassportNo,
    s.AddressLine1,
    s.AddressLine2,
    ci.CityName,
    pr.ProvinceName,
    co.CountryName,
    s.PostalCode,
    s.PhotoPath,
    s.HasSpecialNeeds,
    s.SpecialNeedsDetail,
    s.PreviousSchool,
    s.AdmissionYear,
    s.AdmissionDate,
    p.ProgramCode,
    p.ProgramName,
    it.InstTypeCode,
    it.InstTypeName,
    e.EnrollmentID,
    e.AcademicYear,
    e.GradeOrSemester,
    sec.SectionName,
    e.RollNo,
    e.EnrollmentStatus,
    e.FeeStatus,
    s.IsActive,
    s.StatusRemark,
    s.CreatedAt
FROM      Students             s
JOIN      ref_Programs         p   ON s.ProgramID    = p.ProgramID
JOIN      ref_InstitutionTypes it  ON p.InstTypeID   = it.InstTypeID
JOIN      ref_Cities           ci  ON s.CityID       = ci.CityID
JOIN      ref_Provinces        pr  ON s.ProvinceID   = pr.ProvinceID
JOIN      ref_Countries        co  ON s.CountryID    = co.CountryID
LEFT JOIN ref_BloodGroups      bg  ON s.BloodGroupID = bg.BloodGroupID
LEFT JOIN ref_Religions        r   ON s.ReligionID   = r.ReligionID
LEFT JOIN StudentEnrollments   e   ON s.StudentID    = e.StudentID
    AND e.EnrollmentStatus = 'Active'
LEFT JOIN ref_Sections         sec ON e.SectionID    = sec.SectionID;
GO

CREATE VIEW vw_StudentParentContact AS
SELECT
    s.StudentID,
    s.RegistrationNo,
    s.FirstName + ' ' + s.LastName        AS StudentName,
    rt.RelationName                        AS ParentRelation,
    sp.FirstName + ' ' + sp.LastName       AS ParentName,
    sp.MobileNo,
    sp.WhatsAppNo,
    sp.EmailAddress,
    sp.Occupation,
    sp.OrganizationName,
    sp.IsPrimaryContact,
    sp.IsAuthorizedPickup,
    sp.IsAlive
FROM      Students          s
JOIN      StudentParents    sp ON s.StudentID       = sp.StudentID AND sp.IsActive = 1
JOIN      ref_RelationTypes rt ON sp.RelationTypeID = rt.RelationTypeID;
GO

-- ============================================================
--  SECTION 11: SEED REFERENCE DATA
-- ============================================================

INSERT INTO ref_Countries (CountryName, CountryCode) VALUES
    ('Pakistan',               'PAK'),
    ('United Kingdom',         'GBR'),
    ('United Arab Emirates',   'ARE'),
    ('Saudi Arabia',           'SAU'),
    ('United States',          'USA');

INSERT INTO ref_InstitutionTypes (InstTypeCode, InstTypeName) VALUES
    ('SCH', 'School'),
    ('COL', 'College'),
    ('UNI', 'University'),
    ('CER', 'Certification'),
    ('SHP', 'Short Program');

INSERT INTO ref_BloodGroups (BloodGroupName) VALUES
    ('A+'),('A-'),('B+'),('B-'),('O+'),('O-'),('AB+'),('AB-');

INSERT INTO ref_Religions (ReligionName) VALUES
    ('Islam'),('Christianity'),('Hinduism'),('Sikhism'),('Other');

INSERT INTO ref_RelationTypes (RelationName) VALUES
    ('Father'),('Mother'),('Guardian'),('Brother'),('Sister'),('Spouse');

INSERT INTO ref_ContactTypes (ContactTypeName) VALUES
    ('Mobile'),('WhatsApp'),('Email'),('Home Phone'),('Office Phone'),('Emergency');

INSERT INTO ref_Sections (SectionName) VALUES
    ('A'),('B'),('C'),('D'),('E'),('F');

-- Programs - School
INSERT INTO ref_Programs (InstTypeID, ProgramCode, ProgramName, TotalGrades) VALUES
    (1, 'GEN',  'General School Program',            12);

-- Programs - College
INSERT INTO ref_Programs (InstTypeID, ProgramCode, ProgramName, TotalSemesters) VALUES
    (2, 'FSC',  'Faculty of Science (Pre-Medical)',  4),
    (2, 'ICS',  'Intermediate Computer Science',     4),
    (2, 'ICOM', 'Intermediate Commerce',             4),
    (2, 'FA',   'Faculty of Arts',                   4);

-- Programs - University
INSERT INTO ref_Programs (InstTypeID, ProgramCode, ProgramName, TotalSemesters) VALUES
    (3, 'BCS',  'BS Computer Science',               8),
    (3, 'MBA',  'Master of Business Administration', 4),
    (3, 'BSEE', 'BS Electrical Engineering',         8),
    (3, 'MBBS', 'Bachelor of Medicine',              10),
    (3, 'LLB',  'Bachelor of Laws',                  6);

-- Programs - Certification
INSERT INTO ref_Programs (InstTypeID, ProgramCode, ProgramName, TotalSemesters) VALUES
    (4, 'WD',   'Web Development Certification',     2),
    (4, 'DA',   'Data Analytics Certification',      2),
    (4, 'CLD',  'Cloud Computing Certification',     2);

-- Programs - Short Programs
INSERT INTO ref_Programs (InstTypeID, ProgramCode, ProgramName, TotalSemesters) VALUES
    (5, 'ENG',  'English Language Program',          1),
    (5, 'ITF',  'IT Fundamentals Short Program',     1);
GO
