# Database Schema Documentation: Virtual Education Management System

- *VERSION: 1.7*
- *UPDATED AT: 2026-05-16 12:23pm*

### Project-Wide Rules:-
1. **Uid Mandate:** The primary key of every table is `Uid`.
2. READ [DBworkflows](DBworkflows.md) before implementing any workflows involving Database tables.


## 1. Core Configuration Registry

### Configurations

Centralized store for all system dropdowns, statuses, and types using JSON arrays.

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

These tables handle login credentials and system access roles. Audit trails (`CreatedBy`) are stored here to keep profile tables lean.

### EmployeeLogin

| Header       | Type          | Constraints / Role                                              |
| :----------- | :------------ | :-------------------------------------------------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)                                           |
| EmployeeId   | INT           | **FK** → Employee.Uid, NOT NULL                                 |
| Username     | VARCHAR(50)   | **UNIQUE**, NOT NULL — Indexed for login                        |
| PasswordHash | VARCHAR(MAX)  | NOT NULL — Securely hashed credential                           |
| Role         | NVARCHAR(50)  | NOT NULL — From Configurations e.g. `'Teacher'`, `'Admin'`      |
| Status       | NVARCHAR(50)  | DEFAULT `'Active'` — From Configurations                        |
| CreatedBy    | INT           | **FK** → EmployeeLogin.Uid (self-ref) — Who created this login  |
| CreatedOn    | DATETIME2(7)  | DEFAULT sysdatetime()                                           |
| History      | NVARCHAR(MAX) | Nullable — Audit trail                                          |

### StudentsLogin

| Header       | Type          | Constraints / Role                                          |
| :----------- | :------------ | :---------------------------------------------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)                                       |
| StudentId    | INT           | **FK** → Students.Uid, NOT NULL                             |
| Username     | VARCHAR(50)   | **UNIQUE**, NOT NULL — Indexed for login                    |
| PasswordHash | VARCHAR(MAX)  | NOT NULL — Securely hashed credential                       |
| Status       | NVARCHAR(50)  | DEFAULT `'Active'` — From Configurations                    |
| CreatedBy    | INT           | **FK** → EmployeeLogin.Uid — Staff who created this login   |
| CreatedOn    | DATETIME2(7)  | DEFAULT sysdatetime()                                       |
| History      | NVARCHAR(MAX) | Nullable — Audit trail                                      |

---

## 3. Profile Management (The "Metadata")

Isolated from authentication data to protect PII and optimize search performance.

### Employee

| Header        | Type          | Constraints / Role                                          |
| :------------ | :------------ | :---------------------------------------------------------- |
| **Uid**       | INT           | **PK**, Identity(1,1)                                       |
| EmployeeId    | VARCHAR(20)   | **UNIQUE**, NOT NULL — System-generated ID e.g. `EMP-001`   |
| FullName      | NVARCHAR(100) | NOT NULL                                                    |
| Email         | NVARCHAR(150) | **UNIQUE**, NOT NULL                                        |
| Phone         | NVARCHAR(20)  | Nullable                                                    |
| CNIC          | NVARCHAR(15)  | **UNIQUE**, NOT NULL — National ID                          |
| FatherName    | NVARCHAR(100) | Nullable                                                    |
| DOB           | DATE          | Nullable                                                    |
| Department    | NVARCHAR(100) | Nullable — e.g. `'Sciences'`, `'Administration'`            |
| Designation   | NVARCHAR(100) | Nullable — e.g. `'Head of Dept'`, `'HR Officer'`            |
| Specialization| NVARCHAR(150) | Nullable — Primarily for teaching staff                     |
| Qualification | NVARCHAR(150) | Nullable                                                    |
| EmployeeType  | NVARCHAR(50)  | From Configurations — e.g. `'Permanent'`, `'Contract'`      |
| Status        | NVARCHAR(50)  | DEFAULT `'Active'` — From Configurations                    |
| JoinedDate    | DATE          | DEFAULT sysdatetime()                                       |
| CreatedBy     | INT           | **FK** → EmployeeLogin.Uid — Who created this record        |
| CreatedAt     | DATETIME2(7)  | DEFAULT sysdatetime()                                       |
| ModifiedBy    | INT           | **FK** → EmployeeLogin.Uid — Who last modified this record  |
| ModifiedAt    | DATETIME2(7)  | DEFAULT sysdatetime()                                       |
| Notes         | NVARCHAR(MAX) | Nullable — General purpose remarks                          |

### Students

| Header        | Type          | Constraints / Role                                         |
| :------------ | :------------ | :--------------------------------------------------------- |
| **Uid**       | INT           | **PK**, Identity(1,1)                                      |
| FullName      | NVARCHAR(100) | NOT NULL                                                   |
| Email         | NVARCHAR(150) | Nullable                                                   |
| Phone         | NVARCHAR(20)  | Nullable                                                   |
| GuardianName  | NVARCHAR(100) | NOT NULL                                                   |
| GuardianPhone | NVARCHAR(20)  | NOT NULL                                                   |
| GradeLevel    | NVARCHAR(20)  | Nullable — From Configurations                             |
| City          | NVARCHAR(100) | Nullable                                                   |
| Status        | NVARCHAR(50)  | DEFAULT `'Active'` — From Configurations                   |
| EnrolledDate  | DATE          | DEFAULT sysdatetime()                                      |
| CreatedBy     | INT           | **FK** → EmployeeLogin.Uid — Who created this record       |
| CreatedAt     | DATETIME2(7)  | DEFAULT sysdatetime()                                      |
| ModifiedBy    | INT           | **FK** → EmployeeLogin.Uid — Who last modified this record |
| ModifiedAt    | DATETIME2(7)  | DEFAULT sysdatetime()                                      |

## 4. Academic & Financial Infrastructure

These tables define the educational offerings and the associated costs, now fully synchronized with the literal string status logic and specialized academic fields.

### Programs

Represents the primary educational tracks (e.g., "Matriculation", "O-Levels").

| Header       | Type          | Constraints / Role                               |
| :----------- | :------------ | :----------------------------------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)                            |
| ProgramCode  | VARCHAR(20)   | **UNIQUE**, NOT NULL (Internal ID)               |
| Name         | VARCHAR(100)  | NOT NULL (Display Name)                          |
| ProgramLevel | VARCHAR(50)   | NOT NULL (e.g., 'Secondary', 'Higher Secondary') |
| ProgramType  | VARCHAR(50)   | NOT NULL (e.g., 'Science', 'Arts', 'Commerce')   |
| Description  | NVARCHAR(MAX) | Nullable, detailed overview                      |
| CreatedAt    | DATETIME2(7)  | DEFAULT GETDATE()                                |
| UpdatedAt    | DATETIME2(7)  | DEFAULT GETDATE()                                |
| Status       | NVARCHAR(50)  | DEFAULT 'Active' (Validated vs Configs)          |

### FeeTypes

| Header      | Type          | Constraints / Role                                      |
| :---------- | :------------ | :------------------------------------------------------ |
| **Uid**     | INT           | **PK**, Identity(1,1)                                   |
| FeeName     | NVARCHAR(100) | NOT NULL — e.g. `'Tuition'`, `'Admission'`, `'Exam'`    |
| Category    | NVARCHAR(20)  | NOT NULL — From Configurations `FeeCategories`          |
| Frequency   | NVARCHAR(20)  | NOT NULL — From Configurations `FeeFrequencies`         |
| IsActive    | BIT           | DEFAULT 1 — Soft delete flag                            |
| Description | NVARCHAR(MAX) | Nullable                                                |
| CreatedAt   | DATETIME2(7)  | DEFAULT sysdatetime()                                   |

### FeeStructures

| Header        | Type          | Constraints / Role                                           |
| :------------ | :------------ | :----------------------------------------------------------- |
| **Uid**       | INT           | **PK**, Identity(1,1)                                        |
| FeeTypeId     | INT           | **FK** → FeeTypes.Uid, NOT NULL                              |
| ProgramId     | INT           | **FK** → Programs.Uid, NOT NULL                              |
| Amount        | DECIMAL(10,2) | NOT NULL — Base fee amount                                   |
| LateFeeAmount | DECIMAL(10,2) | NOT NULL DEFAULT 0.00 — Surcharge applied after grace period |
| LateFeeDays   | INT           | NOT NULL DEFAULT 0 — Grace period in days before late fee    |
| AcademicYear  | NVARCHAR(9)   | NOT NULL — e.g. `'2025-2026'`                                |
| DueDate       | DATE          | Nullable — Default due date for this fee structure           |
| IsActive      | BIT           | DEFAULT 1 — Soft delete flag                                 |
| CreatedAt     | DATETIME2(7)  | DEFAULT sysdatetime()                                        |

### FeeDiscounts

| Header        | Type          | Constraints / Role                                      |
| :------------ | :------------ | :------------------------------------------------------ |
| **Uid**       | INT           | **PK**, Identity(1,1)                                   |
| DiscountName  | NVARCHAR(100) | NOT NULL — e.g. `'Sibling Discount'`, `'Merit Award'`   |
| DiscountType  | NVARCHAR(10)  | NOT NULL — From Configurations `DiscountTypes`          |
| DiscountValue | DECIMAL(10,2) | NOT NULL — Amount or percentage value                   |
| IsActive      | BIT           | DEFAULT 1 — Soft delete flag                            |
| Criteria      | NVARCHAR(MAX) | Nullable — Human readable eligibility description       |
| CreatedAt     | DATETIME2(7)  | DEFAULT sysdatetime()                                   |

### StudentFeeAllocations

| Header          | Type          | Constraints / Role                                           |
| :-------------- | :------------ | :----------------------------------------------------------- |
| **Uid**         | INT           | **PK**, Identity(1,1)                                        |
| StudentId       | INT           | **FK** → Students.Uid, NOT NULL                              |
| FeeStructureId  | INT           | **FK** → FeeStructures.Uid, NOT NULL                         |
| DiscountId      | INT           | **FK** → FeeDiscounts.Uid, Nullable                          |
| FinalAmount     | DECIMAL(10,2) | NOT NULL — Amount after discount applied                     |
| Status          | NVARCHAR(50)  | DEFAULT `'Active'` — From Configurations `AllocationStatus`  |
| AllocatedAt     | DATETIME2(7)  | DEFAULT sysdatetime()                                        |

### StudentEnrollments

| Header      | Type          | Constraints / Role                                          |
| :---------- | :------------ | :---------------------------------------------------------- |
| **Uid**     | INT           | **PK**, Identity(1,1)                                       |
| StudentId   | INT           | **FK** → Students.Uid, NOT NULL                             |
| ProgramId   | INT           | **FK** → Programs.Uid, NOT NULL                             |
| Status      | NVARCHAR(50)  | DEFAULT `'Active'` — From Configurations `EnrollmentStatus` |
| EnrolledAt  | DATETIME2(7)  | DEFAULT sysdatetime()                                       |
| DroppedAt   | DATETIME2(7)  | Nullable — Populated when Status = `'Dropped'`              |
| CreatedBy   | INT           | **FK** → EmployeeLogin.Uid                                  |
| Notes       | NVARCHAR(500) | Nullable                                                    |

Unique constraint on `(StudentId, ProgramId)` — a student cannot have duplicate active enrollments in the same program.

---

## 5. Financial Transactions

### Challans

| Header          | Type           | Constraints / Role                                          |
| :-------------- | :------------- | :---------------------------------------------------------- |
| **Uid**         | INT            | **PK**, Identity(1,1)                                       |
| StudentId       | INT            | **FK** → Students.Uid, NOT NULL                             |
| ChallanNo       | NVARCHAR(30)   | **UNIQUE**, NOT NULL — System generated voucher number      |
| AcademicYear    | NVARCHAR(9)    | NOT NULL — e.g. `'2025-2026'`                               |
| MonthYear       | NVARCHAR(7)    | Nullable — e.g. `'2025-09'` for monthly fees                |
| TotalAmount     | DECIMAL(10,2)  | NOT NULL                                                    |
| PaidAmount      | DECIMAL(10,2)  | NOT NULL DEFAULT 0.00                                       |
| Balance         | DECIMAL(10,2)  | **Computed** — `TotalAmount - PaidAmount`                   |
| Status          | NVARCHAR(50)   | DEFAULT `'Unpaid'` — From Configurations `ChallanStatus`    |
| IssueDate       | DATE           | NOT NULL                                                    |
| DueDate         | DATE           | NOT NULL                                                    |
| DueDateHistory  | NVARCHAR(MAX)  | Nullable — CSV of previous due dates, updated on extensions |
| PaidDate        | DATE           | Nullable — Populated when fully paid                        |
| PaymentMode     | NVARCHAR(50)   | Nullable — From Configurations `PaymentModes`               |
| BankRefNo       | NVARCHAR(100)  | Nullable — Bank or online transaction reference             |
| CreatedBy       | INT            | **FK** → EmployeeLogin.Uid                                  |
| CreatedAt       | DATETIME2(7)   | DEFAULT sysdatetime()                                       |
| Remarks         | NVARCHAR(MAX)  | Nullable                                                    |

### ChallanItems

| Header          | Type          | Constraints / Role                                      |
| :-------------- | :------------ | :------------------------------------------------------ |
| **Uid**         | INT           | **PK**, Identity(1,1)                                   |
| ChallanId       | INT           | **FK** → Challans.Uid, NOT NULL — Cascades on delete    |
| FeeStructureId  | INT           | **FK** → FeeStructures.Uid, Nullable                    |
| FeeName         | NVARCHAR(100) | NOT NULL — Snapshot of fee name at time of challan      |
| Amount          | DECIMAL(10,2) | NOT NULL — Base amount before discount                  |
| Discount        | DECIMAL(10,2) | NOT NULL DEFAULT 0.00                                   |
| NetAmount       | DECIMAL(10,2) | NOT NULL — Final line item amount after discount        |

### FeePayments

| Header         | Type          | Constraints / Role                                       |
| :------------- | :------------ | :------------------------------------------------------- |
| **Uid**        | INT           | **PK**, Identity(1,1)                                    |
| ChallanId      | INT           | **FK** → Challans.Uid, NOT NULL                          |
| CollectedBy    | INT           | **FK** → EmployeeLogin.Uid, NOT NULL                     |
| AmountPaid     | DECIMAL(10,2) | NOT NULL                                                 |
| PaymentMode    | NVARCHAR(50)  | NOT NULL — From Configurations `PaymentModes`            |
| TransactionRef | NVARCHAR(100) | Nullable — Bank or online transaction reference          |
| PaymentDate    | DATE          | NOT NULL                                                 |
| ReceiptNo      | NVARCHAR(30)  | **UNIQUE**, NOT NULL — System generated receipt number   |
| CreatedAt      | DATETIME2(7)  | DEFAULT sysdatetime()                                    |

---
