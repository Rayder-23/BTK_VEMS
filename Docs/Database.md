# Database Schema Documentation: Virtual Education Management System

## *VERSION: 1.5*

## *UPDATED AT: 2026-05-12 11:31:00*

## 1. Core Configuration Registry

### Configurations

Centralized store for all system dropdowns, statuses, and types using JSON arrays.

| Header        | Type          | Constraints / Role                                |
| :------------ | :------------ | :------------------------------------------------ |
| **config_id** | INT           | **PK**, Identity(1,1)                             |
| config_key    | NVARCHAR(100) | **UNIQUE**, NOT NULL                              |
| config_values | NVARCHAR(MAX) | **ISJSON CHECK**, stores arrays like `["A", "B"]` |
| description   | NVARCHAR(300) | Context for the configuration key                 |
| is_active     | BIT           | DEFAULT 1 (Soft Delete)                           |
| created_at    | DATETIME2(7)  | DEFAULT sysdatetime()                             |
| updated_at    | DATETIME2(7)  | DEFAULT sysdatetime()                             |

---

## 2. Authentication & Security (The "Lock")

These tables handle login credentials and system access roles. Audit trails (`CreatedBy`) are stored here to keep profile tables lean.

### EmployeeLogin (Admins & Teachers)

| Header       | Type          | Constraints / Role                            |
| :----------- | :------------ | :-------------------------------------------- |
| **uid**      | INT           | **PK**, Identity(1,1)                         |
| EmployeeId   | VARCHAR(20)   | **FK** (Points to `admin_id` or `teacher_id`) |
| Username     | VARCHAR(50)   | **UNIQUE**, Indexed for Login                 |
| PasswordHash | VARCHAR(MAX)  | Securely hashed credential                    |
| Role         | INT           | Bitmask (1=Teacher, 2=Admin, 3=Both)          |
| status       | NVARCHAR(50)  | DEFAULT 'Active'                              |
| CreatedBy    | INT           | ID of the Admin who created this account      |
| CreatedOn    | DATETIME2     | DEFAULT GETDATE()                             |
| History      | NVARCHAR(MAX) | Audit log (JSON/Text)                         |

### StudentsLogin

| Header       | Type          | Constraints / Role                       |
| :----------- | :------------ | :--------------------------------------- |
| **uid**      | INT           | **PK**, Identity(1,1)                    |
| StudentId    | INT           | **FK** (Ref: Students.student_id)        |
| Username     | VARCHAR(50)   | **UNIQUE**, Indexed for Login            |
| PasswordHash | VARCHAR(MAX)  | Securely hashed credential               |
| status       | NVARCHAR(50)  | DEFAULT 'Active'                         |
| CreatedBy    | INT           | ID of the Admin who created this account |
| CreatedOn    | DATETIME2     | DEFAULT GETDATE()                        |
| History      | NVARCHAR(MAX) | Audit log (JSON/Text)                    |

---

## 3. Profile Management (The "Metadata")

Isolated from authentication data to protect PII and optimize search performance.

### Admins

| Header       | Type          | Constraints / Role                      |
| :----------- | :------------ | :-------------------------------------- |
| **admin_id** | INT           | **PK**, Identity(1,1)                   |
| full_name    | NVARCHAR(100) | NOT NULL                                |
| email        | NVARCHAR(150) | **UNIQUE**, NOT NULL                    |
| admin_role   | NVARCHAR(50)  | Job Title (e.g., 'SuperAdmin', 'Clerk') |
| status       | NVARCHAR(50)  | DEFAULT 'Active'                        |
| created_at   | DATETIME2(7)  | DEFAULT sysdatetime()                   |

### Teachers

| Header         | Type          | Constraints / Role                 |
| :------------- | :------------ | :--------------------------------- |
| **teacher_id** | INT           | **PK**, Identity(1,1)              |
| full_name      | NVARCHAR(100) | NOT NULL                           |
| email          | NVARCHAR(150) | **UNIQUE**, NOT NULL               |
| phone          | NVARCHAR(20)  | Nullable                           |
| cnic           | NVARCHAR(15)  | **UNIQUE**, NOT NULL (National ID) |
| teacher_type   | NVARCHAR(50)  | e.g., 'Permanent', 'Visiting'      |
| specialization | NVARCHAR(150) | Academic focus                     |
| qualification  | NVARCHAR(150) | Degree status                      |
| status         | NVARCHAR(50)  | DEFAULT 'Active'                   |
| joined_date    | DATE          | DEFAULT sysdatetime()              |

### Students

| Header         | Type          | Constraints / Role                |
| :------------- | :------------ | :-------------------------------- |
| **student_id** | INT           | **PK**, Identity(1,1)             |
| full_name      | NVARCHAR(100) | NOT NULL                          |
| email          | NVARCHAR(150) | Nullable                          |
| phone          | NVARCHAR(20)  | Nullable                          |
| guardian_name  | NVARCHAR(100) | NOT NULL                          |
| guardian_phone | NVARCHAR(20)  | NOT NULL                          |
| grade_level    | NVARCHAR(20)  | Current academic level            |
| city           | NVARCHAR(100) | Nullable                          |
| status         | NVARCHAR(50)  | **DF_Students_Status** ('Active') |
| enrolled_date  | DATE          | DEFAULT sysdatetime()             |

## 4. Academic & Financial Infrastructure

These tables define the educational offerings and the associated costs, now fully synchronized with the literal string status logic and specialized academic fields.

### Programs

Represents the primary educational tracks (e.g., "Matriculation", "O-Levels").

| Header        | Type          | Constraints / Role                               |
| :------------ | :------------ | :----------------------------------------------- |
| **id**        | INT           | **PK**, Identity(1,1)                            |
| program_code  | VARCHAR(20)   | **UNIQUE**, NOT NULL (Internal ID)               |
| name          | VARCHAR(100)  | NOT NULL (Display Name)                          |
| program_level | VARCHAR(50)   | NOT NULL (e.g., 'Secondary', 'Higher Secondary') |
| program_type  | VARCHAR(50)   | NOT NULL (e.g., 'Science', 'Arts', 'Commerce')   |
| description   | NVARCHAR(MAX) | Nullable, detailed overview                      |
| created_at    | DATETIME2(7)  | DEFAULT GETDATE()                                |
| updated_at    | DATETIME2(7)  | DEFAULT GETDATE()                                |
| status        | NVARCHAR(50)  | DEFAULT 'Active' (Validated vs Configs)          |

### Fees

Manages the pricing structure for specific programs.

| Header         | Type          | Constraints / Role                            |
| :------------- | :------------ | :-------------------------------------------- |
| **id**         | INT           | **PK**, Identity(1,1)                         |
| **program_id** | INT           | **FK** (Ref: Programs.id)                     |
| amount         | DECIMAL(15,2) | **CHECK (amount >= 0)**                       |
| currency       | CHAR(3)       | DEFAULT 'PKR'                                 |
| effective_date | DATE          | NOT NULL (When the price point begins)        |
| notes          | NVARCHAR(500) | Nullable, billing details                     |
| fee_type       | NVARCHAR(50)  | DEFAULT 'Monthly' (e.g., 'Admission', 'Exam') |
| status         | NVARCHAR(50)  | DEFAULT 'Active'                              |

---

## Logic Summary for Backend Integration:

1.  **Validation Handshake:** Before an `INSERT` or `UPDATE` on `Programs.status` or `Fees.fee_type`, the application layer must verify the value exists within the corresponding `Configurations.config_values` JSON array.
2.  **Audit Control:** The `created_at` and `updated_at` timestamps in the `Programs` table provide record-level auditing separate from the `EmployeeLogin` creation history.
3.  **Financial Precision:** The use of `DECIMAL(15,2)` ensures zero rounding errors for financial reporting within the management system.
