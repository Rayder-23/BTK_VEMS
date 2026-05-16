# Database Schema Documentation: Virtual Education Management System

- *VERSION: 1.7*
- *UPDATED AT: 2026-05-16 11:05am*

### Project-Wide Rules:-
1.  **Uid Mandate:** The primary key of every table is `Uid`.
2. READ [DBworkflows](DBworkflows.md) before implementing and workflows involving Database tables.


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

### EmployeeLogin (Admins & Teachers)

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

### Fees

Manages the pricing structure for specific programs.

| Header        | Type          | Constraints / Role                            |
| :------------ | :------------ | :-------------------------------------------- |
| **Uid**       | INT           | **PK**, Identity(1,1)                         |
| **ProgramId** | INT           | **FK** (Ref: Programs.id)                     |
| Amount        | DECIMAL(15,2) | **CHECK (amount >= 0)**                       |
| Currency      | CHAR(3)       | DEFAULT 'PKR'                                 |
| Effectivedate | DATE          | NOT NULL (When the price point begins)        |
| Notes         | NVARCHAR(500) | Nullable, billing details                     |
| FeeType       | NVARCHAR(50)  | DEFAULT 'Monthly' (e.g., 'Admission', 'Exam') |
| Status        | NVARCHAR(50)  | DEFAULT 'Active'                              |

---

### 5. Financial Transactions

#### Payments

Tracks actual cash flow and transaction history for student fees.

| Header        | Type          | Constraints / Role                              |
| :------------ | :------------ | :---------------------------------------------- |
| **Uid**       | INT           | **PK**, Identity(1,1)                           |
| StudentId     | INT           | **FK** (Ref: Students.student_id)               |
| FeeId         | INT           | **FK** (Ref: Fees.id)                           |
| Amount        | DECIMAL(15,2) | NOT NULL (CHECK Amount >= 0)                    |
| Currency      | CHAR(3)       | DEFAULT 'PKR'                                   |
| PaymentMethod | NVARCHAR(50)  | e.g., 'Cash', 'Bank Transfer', 'EasyPaisa'      |
| TransactionId | NVARCHAR(100) | Nullable (Reference for bank/online receipts)   |
| PaidAt        | DATETIME2(7)  | DEFAULT sysdatetime()                           |
| Status        | NVARCHAR(50)  | DEFAULT 'Pending' (e.g., 'Completed', 'Failed') |
| Notes         | NVARCHAR(500) | Internal comments regarding the payment         |

---

### Logic Summary for Backend Integration:

1.  **Validation Handshake:** Before an `INSERT` or `UPDATE` on `Programs.status` or `Fees.fee_type`, the application layer must verify the value exists within the corresponding `Configurations.config_values` JSON array.
2.  **Audit Control:** The `created_at` and `updated_at` timestamps in the `Programs` table provide record-level auditing separate from the `EmployeeLogin` creation history.
3.  **Financial Precision:** The use of `DECIMAL(15,2)` ensures zero rounding errors for financial reporting within the management system.

### Logic Summary for Payments:

1.  **Direct Fee Linking:** By using `FeeId` instead of an `enrollment_id`, you can precisely track which specific fee (Admission, Monthly, Exam) this payment is covering.
2.  **Payment Methods:** Values for `PaymentMethod` should be pulled from the `Configurations` table under a new `PaymentMethod` key.
3.  **Transaction Tracking:** The addition of `TransactionId` allows the registrar to input digital receipt numbers for cross-referencing with bank statements.


