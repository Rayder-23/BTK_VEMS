# Database Schema Documentation: Virtual Education Management System

- *VERSION: 1.6*
- *UPDATED AT: 2026-05-12 01:20pm*

### Project-Wide Rules:-
1.  **Uid Mandate:** The primary key of every table is `Uid`.


## 1. Core Configuration Registry

### Configurations

Centralized store for all system dropdowns, statuses, and types using JSON arrays.

| Header       | Type          | Constraints / Role                                |
| :----------- | :------------ | :------------------------------------------------ |
| **Uid**      | INT           | **PK**, Identity(1,1)                             |
| ConfigKey    | NVARCHAR(100) | **UNIQUE**, NOT NULL                              |
| ConfigValues | NVARCHAR(MAX) | **ISJSON CHECK**, stores arrays like `["A", "B"]` |
| Description  | NVARCHAR(300) | Context for the configuration key                 |
| IsActive     | BIT           | DEFAULT 1 (Soft Delete)                           |
| CreatedAt    | DATETIME2(7)  | DEFAULT sysdatetime()                             |
| UpdatedAt    | DATETIME2(7)  | DEFAULT sysdatetime()                             |

---

## 2. Authentication & Security (The "Lock")

These tables handle login credentials and system access roles. Audit trails (`CreatedBy`) are stored here to keep profile tables lean.

### EmployeeLogin (Admins & Teachers)

| Header       | Type          | Constraints / Role                            |
| :----------- | :------------ | :-------------------------------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)                         |
| EmployeeId   | VARCHAR(20)   | **FK** (Points to `admin_id` or `teacher_id`) |
| Username     | VARCHAR(50)   | **UNIQUE**, Indexed for Login                 |
| PasswordHash | VARCHAR(MAX)  | Securely hashed credential                    |
| Role         | INT           | Bitmask (1=Teacher, 2=Admin, 3=Both)          |
| Status       | NVARCHAR(50)  | DEFAULT 'Active'                              |
| CreatedBy    | INT           | ID of the Admin who created this account      |
| CreatedOn    | DATETIME2     | DEFAULT GETDATE()                             |
| History      | NVARCHAR(MAX) | Audit log (JSON/Text)                         |

### StudentsLogin

| Header       | Type          | Constraints / Role                       |
| :----------- | :------------ | :--------------------------------------- |
| **Uid**      | INT           | **PK**, Identity(1,1)                    |
| StudentId    | INT           | **FK** (Ref: Students.student_id)        |
| Username     | VARCHAR(50)   | **UNIQUE**, Indexed for Login            |
| PasswordHash | VARCHAR(MAX)  | Securely hashed credential               |
| Status       | NVARCHAR(50)  | DEFAULT 'Active'                         |
| CreatedBy    | INT           | ID of the Admin who created this account |
| CreatedOn    | DATETIME2     | DEFAULT GETDATE()                        |
| History      | NVARCHAR(MAX) | Audit log (JSON/Text)                    |

---

## 3. Profile Management (The "Metadata")

Isolated from authentication data to protect PII and optimize search performance.

### Admins

| Header    | Type          | Constraints / Role                      |
| :-------- | :------------ | :-------------------------------------- |
| **Uid**   | INT           | **PK**, Identity(1,1)                   |
| FullName  | NVARCHAR(100) | NOT NULL                                |
| Email     | NVARCHAR(150) | **UNIQUE**, NOT NULL                    |
| AdminRole | NVARCHAR(50)  | Job Title (e.g., 'SuperAdmin', 'Clerk') |
| Status    | NVARCHAR(50)  | DEFAULT 'Active'                        |
| CreatedAt | DATETIME2(7)  | DEFAULT sysdatetime()                   |

### Teachers

| Header         | Type          | Constraints / Role                 |
| :------------- | :------------ | :--------------------------------- |
| **Uid**        | INT           | **PK**, Identity(1,1)              |
| Fullname       | NVARCHAR(100) | NOT NULL                           |
| Email          | NVARCHAR(150) | **UNIQUE**, NOT NULL               |
| Phone          | NVARCHAR(20)  | Nullable                           |
| CNIC           | NVARCHAR(15)  | **UNIQUE**, NOT NULL (National ID) |
| TeacherType    | NVARCHAR(50)  | e.g., 'Permanent', 'Visiting'      |
| Specialization | NVARCHAR(150) | Academic focus                     |
| Qualification  | NVARCHAR(150) | Degree status                      |
| Status         | NVARCHAR(50)  | DEFAULT 'Active'                   |
| JoinedDate     | DATE          | DEFAULT sysdatetime()              |

### Students

| Header        | Type          | Constraints / Role                |
| :------------ | :------------ | :-------------------------------- |
| **Uid**       | INT           | **PK**, Identity(1,1)             |
| FullName      | NVARCHAR(100) | NOT NULL                          |
| Email         | NVARCHAR(150) | Nullable                          |
| Phone         | NVARCHAR(20)  | Nullable                          |
| GuardianName  | NVARCHAR(100) | NOT NULL                          |
| GuardianPhone | NVARCHAR(20)  | NOT NULL                          |
| GradeLevel    | NVARCHAR(20)  | Current academic level            |
| City          | NVARCHAR(100) | Nullable                          |
| Status        | NVARCHAR(50)  | **DF_Students_Status** ('Active') |
| EnrolledDate  | DATE          | DEFAULT sysdatetime()             |

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


