# Database Schema Documentation: Virtual Education Management System

- *VERSION: 1.9*
- *UPDATED AT: 2026-06-12*

### Project-Wide Rules:-
1. **Uid Mandate:** The primary key of every table is `Uid` (except `StudentContacts`, which uses `ContactID` as PK and also carries a separate `Uid` column).
2. READ [DBworkflows](DBworkflows.md) before implementing any workflows involving Database tables.
3. **Configurations** is the single source of truth for all status/type strings — never hardcode dropdown values in application code.


## 1. Core Configuration Registry

### Configurations

Centralized store for all system dropdowns, statuses, and types using plain CSV strings.

| Header       | Type          | Constraints / Role                                        |
| :----------- | :------------ | :-------------------------------------------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)                                     |
| ConfigKey    | NVARCHAR(100) | **UNIQUE**, NOT NULL                                      |
| ConfigValues | NVARCHAR(MAX) | Plain CSV string — e.g. `Active,Inactive,Pending`         |
| Description  | NVARCHAR(300) | Nullable — Context for the configuration key              |
| IsActive     | BIT           | DEFAULT 1 — Soft delete flag                              |
| CreatedAt    | DATETIME2(7)  | DEFAULT sysdatetime()                                     |
| UpdatedAt    | DATETIME2(7)  | DEFAULT sysdatetime()                                     |

---

## 2. Authentication & Security (The "Lock")

These tables handle login credentials and system access roles. Audit trails (`CreatedBy`) reference `EmployeeLogin.Uid`.

### EmployeeLogin

| Header       | Type          | Constraints / Role                                              |
| :----------- | :------------ | :-------------------------------------------------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)                                           |
| EmployeeId   | INT           | **FK** → Employee.Uid, NOT NULL — **ON DELETE CASCADE**         |
| Username     | VARCHAR(50)   | **UNIQUE**, NOT NULL — Indexed for login                        |
| PasswordHash | VARCHAR(MAX)  | NOT NULL — Securely hashed credential                           |
| Role         | NVARCHAR(50)  | NOT NULL — From Configurations `EmployeeRoles`                  |
| Status       | NVARCHAR(50)  | DEFAULT `'Active'` — From Configurations                        |
| CreatedBy    | INT           | **FK** → EmployeeLogin.Uid (self-ref) — Who created this login  |
| CreatedOn    | DATETIME2(7)  | DEFAULT sysdatetime()                                           |
| History      | NVARCHAR(MAX) | Nullable — Audit trail                                          |

### StudentsLogin

| Header             | Type           | Constraints / Role                                          |
| :----------------- | :------------- | :---------------------------------------------------------- |
| **Uid**            | INT            | **PK**, Identity(1,1)                                       |
| StudentId          | INT            | **FK** → Students.Uid, **UNIQUE**, NOT NULL                 |
| Username           | NVARCHAR(100)  | **UNIQUE**, NOT NULL                                        |
| PasswordHash       | NVARCHAR(255)  | NOT NULL — Securely hashed credential                       |
| PasswordSalt       | NVARCHAR(255)  | Nullable                                                    |
| Email              | NVARCHAR(150)  | Nullable                                                    |
| Status             | NVARCHAR(50)   | DEFAULT `'Active'` — From Configurations                    |
| CreatedBy          | INT            | **FK** → EmployeeLogin.Uid — Staff who created this login   |
| CreatedOn          | DATETIME2(7)   | DEFAULT sysutcdatetime()                                    |
| LastLoginAt        | DATETIME2(7)   | Nullable                                                    |
| LastLoginIP        | NVARCHAR(45)   | Nullable                                                    |
| FailedLoginCount   | TINYINT        | DEFAULT 0                                                   |
| LockedUntil        | DATETIME2(7)   | Nullable                                                    |
| ResetToken         | NVARCHAR(255)  | Nullable                                                    |
| ResetTokenExpiry   | DATETIME2(7)   | Nullable                                                    |
| MustChangePassword | BIT            | DEFAULT 1                                                   |
| PasswordChangedAt  | DATETIME2(7)   | Nullable                                                    |
| UpdatedBy          | INT            | Nullable                                                    |
| UpdatedAt          | DATETIME2(7)   | Nullable                                                    |

---

## 3. Profile Management (The "Metadata")

Isolated from authentication data to protect PII and optimize search performance.

### Employee

| Header         | Type          | Constraints / Role                                          |
| :------------- | :------------ | :---------------------------------------------------------- |
| **Uid**        | INT           | **PK**, Identity(1,1)                                       |
| EmployeeId     | VARCHAR(20)   | **UNIQUE**, NOT NULL — System-generated ID e.g. `EMP-001` |
| FullName       | NVARCHAR(100) | NOT NULL                                                    |
| Email          | NVARCHAR(150) | **UNIQUE**, NOT NULL                                        |
| Phone          | NVARCHAR(20)  | Nullable                                                    |
| CNIC           | NVARCHAR(15)  | **UNIQUE**, NOT NULL — National ID                          |
| FatherName     | NVARCHAR(100) | Nullable                                                    |
| DOB            | DATE          | Nullable                                                    |
| Department     | NVARCHAR(100) | Nullable                                                    |
| Designation    | NVARCHAR(100) | Nullable                                                    |
| Specialization | NVARCHAR(150) | Nullable — Primarily for teaching staff                     |
| Qualification  | NVARCHAR(150) | Nullable                                                    |
| EmployeeType   | NVARCHAR(50)  | From Configurations                                         |
| Status         | NVARCHAR(50)  | DEFAULT `'Active'` — From Configurations                    |
| JoinedDate     | DATE          | DEFAULT CONVERT(date, sysdatetime())                        |
| CreatedBy      | INT           | **FK** → EmployeeLogin.Uid                                  |
| CreatedAt      | DATETIME2(7)  | DEFAULT sysdatetime()                                       |
| ModifiedBy     | INT           | **FK** → EmployeeLogin.Uid                                  |
| ModifiedAt     | DATETIME2(7)  | DEFAULT sysdatetime()                                       |
| Notes          | NVARCHAR(MAX) | Nullable                                                    |

### Students

Full student profile. Linked to geographic reference tables and academic program.

| Header             | Type          | Constraints / Role                                          |
| :----------------- | :------------ | :---------------------------------------------------------- |
| **Uid**            | INT           | **PK**, Identity(1,1)                                       |
| RegistrationNo     | NVARCHAR(30)  | **UNIQUE**, NOT NULL — System registration number           |
| ProgramID          | INT           | **FK** → ref_Programs.Uid, NOT NULL                         |
| SectionId          | INT           | **FK** → Sections.Uid — **ON DELETE SET NULL**              |
| AdmissionYear      | SMALLINT      | NOT NULL                                                    |
| AdmissionDate      | DATE          | NOT NULL                                                    |
| FirstName          | NVARCHAR(50)  | NOT NULL                                                    |
| MiddleName         | NVARCHAR(50)  | Nullable                                                    |
| LastName           | NVARCHAR(50)  | NOT NULL                                                    |
| FatherName         | NVARCHAR(100) | NOT NULL                                                    |
| DateOfBirth        | DATE          | NOT NULL                                                    |
| Gender             | CHAR(1)       | NOT NULL — e.g. `'M'`, `'F'`                                |
| Nationality        | NVARCHAR(50)  | DEFAULT `'Pakistani'`                                       |
| NIC_No             | VARCHAR(15)   | Nullable                                                    |
| BFORM_No           | VARCHAR(15)   | Nullable                                                    |
| PassportNo         | NVARCHAR(20)  | Nullable                                                    |
| AddressLine1       | NVARCHAR(150) | NOT NULL                                                    |
| AddressLine2       | NVARCHAR(150) | Nullable                                                    |
| CityID             | INT           | **FK** → ref_Cities.Uid                                     |
| ProvinceID         | INT           | **FK** → ref_Provinces.Uid                                  |
| CountryID          | INT           | **FK** → ref_Countries.Uid                                  |
| PostalCode         | NVARCHAR(10)  | Nullable                                                    |
| RollNo             | NVARCHAR(30)  | Nullable                                                    |
| Religion           | NVARCHAR(50)  | Nullable                                                    |
| BloodGroup         | VARCHAR(5)    | Nullable                                                    |
| PhotoPath          | NVARCHAR(300) | Nullable                                                    |
| DocumentPath       | NVARCHAR(300) | Nullable                                                    |
| PreviousSchool     | NVARCHAR(150) | Nullable                                                    |
| PreviousGradeOrSem | NVARCHAR(20)  | Nullable                                                    |
| TransferCertNo     | NVARCHAR(50)  | Nullable                                                    |
| HasSpecialNeeds    | BIT           | DEFAULT 0                                                   |
| SpecialNeedsDetail | NVARCHAR(300) | Nullable                                                    |
| IsActive           | BIT           | DEFAULT 1 — Soft delete flag                                |
| StatusRemark       | NVARCHAR(200) | Nullable                                                    |
| CreatedBy          | INT           | NOT NULL                                                    |
| CreatedAt          | DATETIME2(7)  | DEFAULT sysutcdatetime()                                    |
| UpdatedBy          | INT           | Nullable                                                    |
| UpdatedAt          | DATETIME2(7)  | Nullable                                                    |

### StudentParents

Guardian/parent records linked to a student.

| Header             | Type           | Constraints / Role                               |
| :----------------- | :------------- | :----------------------------------------------- |
| **Uid**            | INT            | **PK**, Identity(1,1)                            |
| StudentID          | INT            | **FK** → Students.Uid, NOT NULL                  |
| RelationType       | NVARCHAR(50)   | Nullable — e.g. `'Father'`, `'Mother'`           |
| FirstName          | NVARCHAR(50)   | NOT NULL                                         |
| LastName           | NVARCHAR(50)   | NOT NULL                                         |
| DateOfBirth        | DATE           | Nullable                                         |
| NIC_No             | VARCHAR(15)    | Nullable                                         |
| Nationality        | NVARCHAR(50)   | Nullable                                         |
| Education          | NVARCHAR(100)  | Nullable                                         |
| Occupation         | NVARCHAR(100)  | Nullable                                         |
| OrganizationName   | NVARCHAR(150)  | Nullable                                         |
| Designation        | NVARCHAR(100)  | Nullable                                         |
| MonthlyIncome      | DECIMAL(12,2)  | Nullable                                         |
| AddressLine1       | NVARCHAR(150)  | Nullable                                         |
| AddressLine2       | NVARCHAR(150)  | Nullable                                         |
| CityID             | INT            | **FK** → ref_Cities.Uid                          |
| ProvinceID         | INT            | **FK** → ref_Provinces.Uid                       |
| CountryID          | INT            | **FK** → ref_Countries.Uid                       |
| MobileNo           | NVARCHAR(20)   | Nullable                                         |
| WhatsAppNo         | NVARCHAR(20)   | Nullable                                         |
| EmailAddress       | NVARCHAR(150)  | Nullable                                         |
| OfficeNo           | NVARCHAR(20)   | Nullable                                         |
| IsPrimaryContact   | BIT            | DEFAULT 0                                        |
| IsAuthorizedPickup | BIT            | DEFAULT 1                                        |
| PhotoPath          | NVARCHAR(300)  | Nullable                                         |
| IsAlive            | BIT            | DEFAULT 1                                        |
| IsActive           | BIT            | DEFAULT 1                                        |
| CreatedBy          | INT            | NOT NULL                                         |
| CreatedAt          | DATETIME2(7)   | DEFAULT sysutcdatetime()                         |
| UpdatedBy          | INT            | Nullable                                         |
| UpdatedAt          | DATETIME2(7)   | Nullable                                         |

### StudentContacts

Multiple contact methods per student (phone, email, etc.).

| Header       | Type           | Constraints / Role                               |
| :----------- | :------------- | :----------------------------------------------- |
| **ContactID**| INT            | **PK**, Identity(1,1)                            |
| Uid          | INT            | **UNIQUE**, NOT NULL — Secondary identifier      |
| StudentID    | INT            | **FK** → Students.Uid, NOT NULL                  |
| ContactType  | NVARCHAR(30)   | Nullable — e.g. `'Mobile'`, `'Email'`            |
| ContactValue | NVARCHAR(150)  | NOT NULL                                         |
| IsPrimary    | BIT            | DEFAULT 0                                        |
| IsVerified   | BIT            | DEFAULT 0                                        |
| Remarks      | NVARCHAR(100)  | Nullable                                         |
| IsActive     | BIT            | DEFAULT 1                                        |
| CreatedBy    | INT            | NOT NULL                                         |
| CreatedAt    | DATETIME2(7)   | DEFAULT sysutcdatetime()                         |
| UpdatedBy    | INT            | Nullable                                         |
| UpdatedAt    | DATETIME2(7)   | Nullable                                         |

### StudentSiblings

Sibling records — may link to another enrolled student or store external sibling info.

| Header            | Type           | Constraints / Role                               |
| :---------------- | :------------- | :----------------------------------------------- |
| **Uid**           | INT            | **PK**, Identity(1,1)                            |
| StudentID         | INT            | **FK** → Students.Uid, NOT NULL                  |
| SiblingStudentID  | INT            | **FK** → Students.Uid — Nullable internal link   |
| RelationType      | NVARCHAR(50)   | Nullable                                         |
| FirstName         | NVARCHAR(50)   | Nullable                                         |
| LastName          | NVARCHAR(50)   | Nullable                                         |
| DateOfBirth       | DATE           | Nullable                                         |
| Gender            | CHAR(1)        | Nullable                                         |
| InstitutionName   | NVARCHAR(150)  | Nullable                                         |
| GradeOrClass      | NVARCHAR(20)   | Nullable                                         |
| IsActive          | BIT            | DEFAULT 1                                        |
| CreatedBy         | INT            | NOT NULL                                         |
| CreatedAt         | DATETIME2(7)   | DEFAULT sysutcdatetime()                         |
| UpdatedBy         | INT            | Nullable                                         |
| UpdatedAt         | DATETIME2(7)   | Nullable                                         |

---

## 4. Geographic & Institutional Reference Data

### ref_Countries

| Header      | Type           | Constraints / Role     |
| :---------- | :------------- | :--------------------- |
| **Uid**     | INT            | **PK**, Identity(1,1) |
| CountryName | NVARCHAR(100)  | NOT NULL               |
| CountryCode | CHAR(3)        | **UNIQUE**, NOT NULL   |
| IsActive    | BIT            | DEFAULT 1              |

### ref_Provinces

| Header       | Type           | Constraints / Role                    |
| :----------- | :------------- | :------------------------------------ |
| **Uid**      | INT            | **PK**, Identity(1,1)                 |
| CountryID    | INT            | **FK** → ref_Countries.Uid, NOT NULL |
| ProvinceName | NVARCHAR(100)  | NOT NULL                              |
| ProvinceCode | NVARCHAR(10)   | Nullable                              |
| IsActive     | BIT            | DEFAULT 1                             |

### ref_Cities

| Header     | Type           | Constraints / Role                     |
| :--------- | :------------- | :------------------------------------- |
| **Uid**    | INT            | **PK**, Identity(1,1)                  |
| ProvinceID | INT            | **FK** → ref_Provinces.Uid, NOT NULL   |
| CityName   | NVARCHAR(100)  | NOT NULL                               |
| IsActive   | BIT            | DEFAULT 1                              |

### ref_InstitutionTypes

| Header       | Type          | Constraints / Role     |
| :----------- | :------------ | :----------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)   |
| InstTypeCode | CHAR(3)       | **UNIQUE**, NOT NULL     |
| InstTypeName | NVARCHAR(50)  | NOT NULL                 |
| IsActive     | BIT           | DEFAULT 1                |

### ref_Programs

Primary academic program catalog (replaces legacy `Programs` table).

| Header        | Type          | Constraints / Role            |
| :------------ | :------------ | :---------------------------- |
| **Uid**       | INT           | **PK**, Identity(1,1)         |
| ProgramCode   | NVARCHAR(10)  | **UNIQUE**, NOT NULL          |
| ProgramName   | NVARCHAR(100) | NOT NULL                      |
| ShortName     | NVARCHAR(50)  | Nullable                      |
| DurationYears | TINYINT       | Nullable                      |
| IsActive      | BIT           | DEFAULT 1                     |
| CreatedAt     | DATETIME2(7)  | DEFAULT sysdatetime()         |

### ref_FeeHeads

Master list of fee line-item types (replaces legacy `FeeTypes` table).

| Header      | Type           | Constraints / Role                               |
| :---------- | :------------- | :----------------------------------------------- |
| **Uid**     | SMALLINT       | **PK**, Identity(1,1)                            |
| HeadCode    | VARCHAR(20)    | **UNIQUE**, NOT NULL                             |
| HeadName    | NVARCHAR(100)  | NOT NULL — e.g. `'Tuition'`, `'Admission'`       |
| Category    | NVARCHAR(50)   | NOT NULL — From Configurations                   |
| IsMandatory | BIT            | DEFAULT 1                                        |
| IsActive    | BIT            | DEFAULT 1                                        |
| Description | NVARCHAR(300)  | Nullable                                         |
| CreatedBy   | INT            | NOT NULL                                         |
| CreatedAt   | DATETIME2(7)   | DEFAULT sysutcdatetime()                         |
| UpdatedBy   | INT            | Nullable                                         |
| UpdatedAt   | DATETIME2(7)   | Nullable                                         |

---

## 5. Academic Structure

### Sections

Class sections within a program, grade, and academic year.

| Header       | Type          | Constraints / Role                                          |
| :----------- | :------------ | :---------------------------------------------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)                                       |
| ProgramId    | INT           | **FK** → ref_Programs.Uid, NOT NULL                         |
| SectionName  | NVARCHAR(10)  | NOT NULL — e.g. `'A'`, `'B'`                                |
| GradeLevel   | NVARCHAR(20)  | NOT NULL                                                    |
| AcademicYear | NVARCHAR(9)   | NOT NULL — e.g. `'2025-2026'`                               |
| MaxCapacity  | INT           | Nullable                                                    |
| IsActive     | BIT           | DEFAULT 1                                                   |
| CreatedAt    | DATETIME2(7)  | DEFAULT sysdatetime()                                       |

Unique constraint on `(ProgramId, GradeLevel, AcademicYear, SectionName)`.

### StudentEnrollments

Per-period enrollment linking a student to a program, section, and roll number.

| Header           | Type          | Constraints / Role                                          |
| :--------------- | :------------ | :---------------------------------------------------------- |
| **Uid**          | INT           | **PK**, Identity(1,1)                                       |
| StudentID        | INT           | **FK** → Students.Uid, NOT NULL                             |
| ProgramID        | INT           | **FK** → ref_Programs.Uid, NOT NULL                         |
| ClassID          | INT           | **FK** → Classes.Uid — Nullable                             |
| AcademicYear     | SMALLINT      | NOT NULL — e.g. `2025`                                      |
| GradeOrSemester  | TINYINT       | NOT NULL                                                    |
| RollNo           | NVARCHAR(30)  | NOT NULL                                                    |
| EnrollmentDate   | DATE          | NOT NULL                                                    |
| EnrollmentStatus | NVARCHAR(20)  | DEFAULT `'Active'` — From Configurations                    |
| IsActive         | BIT           | DEFAULT 1                                                   |
| CreatedAt        | DATETIME2(7)  | DEFAULT sysdatetime()                                       |
| [REMOVED] SectionID | INT        | Replaced by `ClassID` in live schema                        |
| [REMOVED] CompletionDate | DATE   | No longer present in live schema                            |
| [REMOVED] FeeStatus | NVARCHAR(20) | No longer present in live schema                          |
| [REMOVED] Remarks | NVARCHAR(300) | No longer present in live schema                           |
| [REMOVED] CreatedBy | INT       | No longer present in live schema                            |
| [REMOVED] UpdatedBy | INT       | No longer present in live schema                            |
| [REMOVED] UpdatedAt | DATETIME2(7) | No longer present in live schema                         |

Unique constraints:
- `(StudentID, ProgramID, AcademicYear, GradeOrSemester)` — one enrollment per period
- `(ProgramID, AcademicYear, GradeOrSemester, RollNo)` — roll numbers unique within cohort

### TeacherSections

Assigns an employee (teacher) to a section for a given academic year.

| Header       | Type          | Constraints / Role                                          |
| :----------- | :------------ | :---------------------------------------------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)                                       |
| SectionId    | INT           | **FK** → Sections.Uid, NOT NULL                             |
| EmployeeId   | INT           | **FK** → Employee.Uid, NOT NULL                             |
| AcademicYear | NVARCHAR(9)   | NOT NULL — e.g. `'2025-2026'`                               |
| IsActive     | BIT           | DEFAULT 1                                                   |
| AssignedAt   | DATETIME2(7)  | DEFAULT sysdatetime()                                       |
| AssignedBy   | INT           | **FK** → EmployeeLogin.Uid                                  |

Unique constraint on `(SectionId, EmployeeId, AcademicYear)`.

---

## 6. Fee & Billing Infrastructure

Billing chain: `StudentEnrollments` → `ref_Programs` → `FeeStructures` → `FeeStructureDetails` → `ref_FeeHeads`.

### FeeStructures

Header record for a program's fee schedule in a given semester and academic year.

| Header        | Type           | Constraints / Role                                          |
| :------------ | :------------- | :------------------------------------------------------------ |
| **Uid**       | INT            | **PK**, Identity(1,1)                                         |
| StructureName | NVARCHAR(150)  | NOT NULL                                                      |
| ProgramID     | INT            | **FK** → ref_Programs.Uid, NOT NULL                           |
| ClassID       | INT            | **FK** → Classes.Uid — Nullable (section-specific fee schedule) |
| Semester      | NVARCHAR(20)   | NOT NULL                                                      |
| AcademicYear  | SMALLINT       | NOT NULL — e.g. `2025`                                        |
| IsActive      | BIT            | DEFAULT 1                                                     |
| CreatedBy     | INT            | NOT NULL                                                      |
| CreatedAt     | DATETIME2(7)   | DEFAULT sysutcdatetime()                                      |
| UpdatedBy     | INT            | Nullable                                                      |
| UpdatedAt     | DATETIME2(7)   | Nullable                                                      |

Filtered unique indexes:
- `(ProgramID, Semester, AcademicYear)` where `ClassID IS NULL` — one program-wide structure per period
- `(ProgramID, Semester, AcademicYear, ClassID)` where `ClassID IS NOT NULL` — one structure per class/section per period

### FeeStructureDetails

Line items within a fee structure — one row per fee head.

| Header         | Type          | Constraints / Role                                          |
| :------------- | :------------ | :---------------------------------------------------------- |
| **Uid**        | INT           | **PK**, Identity(1,1)                                       |
| StructureID    | INT           | **FK** → FeeStructures.Uid, NOT NULL                        |
| FeeHeadID      | SMALLINT      | **FK** → ref_FeeHeads.Uid, NOT NULL                         |
| Amount         | DECIMAL(10,2) | NOT NULL — Base fee amount                                    |
| DueDate        | DATE          | Nullable — Default due date for this line item              |
| LateFinePerDay | DECIMAL(8,2)  | DEFAULT 0.00 — Daily late fine rate                         |
| MaxLateFine    | DECIMAL(10,2) | DEFAULT 0.00 — Cap on total late fine                         |
| CreatedBy      | INT           | NOT NULL                                                    |
| CreatedAt      | DATETIME2(7)  | DEFAULT sysutcdatetime()                                    |
| UpdatedBy      | INT           | Nullable                                                    |
| UpdatedAt      | DATETIME2(7)  | Nullable                                                    |

Unique constraint on `(StructureID, FeeHeadID)`.

### Concessions

Per-student fee discounts/concessions, optionally scoped to a specific fee head.

| Header          | Type          | Constraints / Role                                          |
| :-------------- | :------------ | :------------------------------------------------------------ |
| **Uid**         | INT           | **PK**, Identity(1,1)                                         |
| StudentID       | INT           | **FK** → Students.Uid, NOT NULL                               |
| FeeHeadID       | SMALLINT      | **FK** → ref_FeeHeads.Uid — Nullable (applies to all if NULL) |
| ConcessionType  | NVARCHAR(50)  | NOT NULL — From Configurations                                |
| DiscountPercent | DECIMAL(5,2)  | DEFAULT 0.00                                                  |
| DiscountAmount  | DECIMAL(10,2) | DEFAULT 0.00                                                  |
| ApprovedBy      | NVARCHAR(100) | Nullable                                                      |
| ApprovalDate    | DATE          | Nullable                                                      |
| ValidFrom       | DATE          | NOT NULL                                                      |
| ValidTo         | DATE          | Nullable                                                      |
| Remarks         | NVARCHAR(300) | Nullable                                                      |
| IsActive        | BIT           | DEFAULT 1                                                     |
| CreatedBy       | INT           | NOT NULL                                                      |
| CreatedAt       | DATETIME2(7)  | DEFAULT sysutcdatetime()                                      |
| UpdatedBy       | INT           | Nullable                                                      |
| UpdatedAt       | DATETIME2(7)  | Nullable                                                      |

---

## 7. Financial Transactions

### Challans

Parent payment voucher record for a student or admission applicant.

| Header         | Type          | Constraints / Role                                          |
| :------------- | :------------ | :---------------------------------------------------------- |
| **Uid**        | INT           | **PK**, Identity(1,1)                                       |
| ChallanNo      | NVARCHAR(30)  | **UNIQUE**, NOT NULL — System generated voucher number      |
| StudentID      | INT           | **FK** → Students.Uid — Nullable (student fee challans)     |
| ApplicationUid | INT           | **FK** → StudentApplications.Uid — Nullable (admission challans) |
| StructureID    | INT           | **FK** → FeeStructures.Uid — Nullable                       |
| Semester       | NVARCHAR(20)  | NOT NULL                                                    |
| AcademicYear   | SMALLINT      | NOT NULL — e.g. `2025`                                      |
| IssueDate      | DATE          | DEFAULT CONVERT(date, sysutcdatetime())                     |
| DueDate        | DATE          | NOT NULL                                                    |
| TotalAmount    | DECIMAL(10,2) | NOT NULL — Sum of line items before discounts               |
| DiscountAmount | DECIMAL(10,2) | DEFAULT 0.00                                                |
| LateFineAmount | DECIMAL(10,2) | DEFAULT 0.00                                                |
| NetPayable     | DECIMAL(10,2) | NOT NULL — Amount due after discounts and late fines        |
| AmountPaid     | DECIMAL(10,2) | DEFAULT 0.00 — Updated by application on each payment       |
| Status         | NVARCHAR(20)  | DEFAULT `'Unpaid'` — From Configurations                    |
| Remarks        | NVARCHAR(300) | Nullable                                                    |
| IsActive       | BIT           | DEFAULT 1                                                   |
| CreatedBy      | INT           | NOT NULL                                                    |
| CreatedAt      | DATETIME2(7)  | DEFAULT sysutcdatetime()                                    |
| UpdatedBy      | INT           | Nullable                                                    |
| UpdatedAt      | DATETIME2(7)  | Nullable                                                    |

### ChallanDetails

Itemized line items per challan — one row per fee head.

| Header         | Type          | Constraints / Role                                          |
| :------------- | :------------ | :---------------------------------------------------------- |
| **Uid**        | INT           | **PK**, Identity(1,1)                                       |
| ChallanID      | INT           | **FK** → Challans.Uid, NOT NULL                             |
| FeeHeadID      | SMALLINT      | **FK** → ref_FeeHeads.Uid, NOT NULL                         |
| Amount         | DECIMAL(10,2) | NOT NULL — Base amount before discount                      |
| DiscountAmount | DECIMAL(10,2) | DEFAULT 0.00                                                |
| LateFine       | DECIMAL(10,2) | DEFAULT 0.00                                                |
| NetAmount      | DECIMAL(10,2) | NOT NULL — Final line item amount                           |
| CreatedBy      | INT           | NOT NULL                                                    |
| CreatedAt      | DATETIME2(7)  | DEFAULT sysutcdatetime()                                    |

Unique constraint on `(ChallanID, FeeHeadID)`.

### Payments

Payment ledger — multiple rows per challan are valid (partial payments).

| Header         | Type          | Constraints / Role                                          |
| :------------- | :------------ | :---------------------------------------------------------- |
| **Uid**        | INT           | **PK**, Identity(1,1)                                       |
| ChallanID      | INT           | **FK** → Challans.Uid, NOT NULL                             |
| AmountPaid     | DECIMAL(10,2) | NOT NULL                                                    |
| PaymentDate    | DATE          | DEFAULT CONVERT(date, sysutcdatetime())                     |
| PaymentMode    | NVARCHAR(30)  | NOT NULL — From Configurations                              |
| TransactionRef | NVARCHAR(100) | Nullable — Bank or online transaction reference             |
| BankName       | NVARCHAR(100) | Nullable                                                    |
| BranchName     | NVARCHAR(100) | Nullable                                                    |
| ChequeNo       | NVARCHAR(50)  | Nullable                                                    |
| ChequeDate     | DATE          | Nullable                                                    |
| Status         | NVARCHAR(20)  | DEFAULT `'Pending'` — From Configurations                   |
| VerifiedBy     | INT           | Nullable                                                    |
| VerifiedAt     | DATETIME2(7)  | Nullable                                                    |
| Remarks        | NVARCHAR(300) | Nullable                                                    |
| IsActive       | BIT           | DEFAULT 1                                                   |
| CreatedBy      | INT           | NOT NULL                                                    |
| CreatedAt      | DATETIME2(7)  | DEFAULT sysutcdatetime()                                    |
| UpdatedBy      | INT           | Nullable                                                    |
| UpdatedAt      | DATETIME2(7)  | Nullable                                                    |

Application must update `Challans.AmountPaid` on every `Payments` insert — there is no trigger enforcing this.

### PaymentReceipts

Receipt records issued per payment transaction.

| Header        | Type          | Constraints / Role                                          |
| :------------ | :------------ | :---------------------------------------------------------- |
| **Uid**       | INT           | **PK**, Identity(1,1)                                       |
| PaymentID     | INT           | **FK** → Payments.Uid, **UNIQUE**, NOT NULL                 |
| ReceiptNo     | NVARCHAR(30)  | **UNIQUE**, NOT NULL — System generated receipt number      |
| IssuedAt      | DATETIME2(7)  | DEFAULT sysutcdatetime()                                    |
| IssuedBy      | NVARCHAR(100) | Nullable                                                    |
| PrintCount    | INT           | DEFAULT 0                                                   |
| LastPrintedAt | DATETIME2(7)  | Nullable                                                    |
| CreatedBy     | INT           | NOT NULL                                                    |
| CreatedAt     | DATETIME2(7)  | DEFAULT sysutcdatetime()                                    |

### ChallanReminders

Tracks payment reminder notifications sent to students.

| Header       | Type          | Constraints / Role                                          |
| :----------- | :------------ | :---------------------------------------------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)                                       |
| ChallanID    | INT           | **FK** → Challans.Uid, NOT NULL                             |
| StudentID    | INT           | **FK** → Students.Uid, NOT NULL                             |
| ReminderDate | DATE          | NOT NULL                                                    |
| Channel      | NVARCHAR(20)  | NOT NULL — e.g. `'SMS'`, `'Email'`                          |
| MessageBody  | NVARCHAR(500) | Nullable                                                    |
| Status       | NVARCHAR(20)  | DEFAULT `'Pending'` — From Configurations                     |
| SentAt       | DATETIME2(7)  | Nullable — Populated when reminder is dispatched            |
| CreatedBy    | INT           | NOT NULL                                                    |
| CreatedAt    | DATETIME2(7)  | DEFAULT sysutcdatetime()                                    |

---

## 8. Billing Flow Summary

```
StudentEnrollments → ref_Programs → FeeStructures → FeeStructureDetails → ref_FeeHeads
                                              ↓
                                          Challans (StructureID)
                                              ↓
                                        ChallanDetails (FeeHeadID)
                                              ↓
                                          Payments → PaymentReceipts
```

- **Challan generation** begins by querying `StudentEnrollments` for active enrollments, then resolves `FeeStructures` for the student's program/semester/year.
- **Concessions** are checked per student (and optionally per `FeeHeadID`) before writing `ChallanDetails.DiscountAmount`.
- **Late fines** use `FeeStructureDetails.LateFinePerDay` and `MaxLateFine` — calculate at challan generation or payment time based on days past `Challans.DueDate`.
- **`Challans.AmountPaid`** must be updated by the application on every `Payments` insert; `Status` should reflect full/partial payment state.

---
