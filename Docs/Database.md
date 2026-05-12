Virtual Education Management System - Database Schema
Generated: 2026-05-12 10:40

### 1. Configuration (The Source of Truth)
| Header      | Type         | Constraints                |
|:------------|:-------------|:---------------------------|
| **uid**     | INT          | **PK**, Identity(1,1)      |
| ConfigKey   | VARCHAR(50)  | NOT NULL, Unique Group Name|
| ConfigValue | VARCHAR(MAX) | NOT NULL, Comma-Separated  |
| is_active   | BIT          | DEFAULT 1 (Soft Removal)   |

---

### 2. Programs
| Header            | Type         | Constraints                               |
|:------------------|:-------------|:------------------------------------------|
| **id**            | INT          | **PK**, Identity(1,1)                     |
| program_code      | VARCHAR(20)  | UNIQUE, NOT NULL                          |
| name              | VARCHAR(100) | NOT NULL                                  |
| program_level     | VARCHAR(50)  | e.g., 'Grade 1'                           |
| program_type      | VARCHAR(50)  | e.g., 'Academic'                          |
| status            | VARCHAR(20)  | Value must exist in Config 'ProgramStatus'|
| description       | NVARCHAR(MAX)| Nullable                                  |
| created_at        | DATETIME2    | DEFAULT GETDATE()                         |
| updated_at        | DATETIME2    | DEFAULT GETDATE()                         |

---

### 3. Fees
| Header            | Type         | Constraints                               |
|:------------------|:-------------|:------------------------------------------|
| **id**            | INT          | **PK**, Identity(1,1)                     |
| **program_id**    | INT          | **FK** (Ref: Programs.id)                 |
| fee_type          | VARCHAR(50)  | Value must exist in Config 'FeeType'      |
| amount            | DECIMAL(15,2)| CHECK (amount >= 0)                       |
| currency          | CHAR(3)      | DEFAULT 'PKR'                             |
| effective_date    | DATE         | NOT NULL                                  |
| status            | VARCHAR(20)  | Value must exist in Config 'ProgramStatus'|

---

### 4. EmployeeLogin (Admins & Teachers)
| Header            | Type         | Constraints                               |
|:------------------|:-------------|:------------------------------------------|
| **uid**           | INT          | **PK**, Identity(1,1)                     |
| EmployeeId        | VARCHAR(20)  | UNIQUE identifier                         |
| Username          | VARCHAR(50)  | UNIQUE, Indexed for Login                 |
| PasswordHash      | VARCHAR(MAX) | Hashed Secret                             |
| Role              | INT          | 1=Teacher, 2=Admin, 3=Both (Bitmask/Logic)|
| status            | VARCHAR(20)  | Value must exist in Config 'EmployeeStatus'|
| CreatedBy         | INT          | ID of creator                             |
| CreatedOn         | DATETIME2    | DEFAULT GETDATE()                         |
| History           | NVARCHAR(MAX)| JSON/Text Log of changes                  |

---

### 5. StudentsLogin
| Header            | Type         | Constraints                               |
|:------------------|:-------------|:------------------------------------------|
| **uid**           | INT          | **PK**, Identity(1,1)                     |
| StudentId         | VARCHAR(20)  | UNIQUE identifier                         |
| Username          | VARCHAR(50)  | UNIQUE, Indexed for Login                 |
| PasswordHash      | VARCHAR(MAX) | Hashed Secret                             |
| Role              | INT          | Default Student Role                      |
| status            | VARCHAR(20)  | Value must exist in Config 'StudentStatus' |
| CreatedBy         | INT          | ID of creator                             |
| CreatedOn         | DATETIME2    | DEFAULT GETDATE()                         |
| History           | NVARCHAR(MAX)| JSON/Text Log of changes                  |