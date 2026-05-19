# VEMS — Virtual Education Management System

A web platform for managing students, staff, academic programs, and fee billing at Bahria Town Virtual Schooling. Built as an ASP.NET Core MVC application with area-based portals and direct SQL Server access via ADO.NET.

---

## Project Overview

VEMS centralizes school operations in one system:

- **Student lifecycle** — registration, guardians, contacts, siblings, enrollments, and portal logins
- **Staff & HR** — employee profiles, roles, and login accounts
- **Fee & billing** — fee heads, structures, challans, payments, receipts, concessions, and reminders
- **Configuration** — system-wide dropdowns and status values driven by the `Configurations` table

The application is organized into **areas** (portals), each with its own routes and UI. The public site handles marketing and admissions information; authenticated areas serve admins, students, and teachers.

---

## Technology Stack

| Category | Technology |
|----------|------------|
| Framework | ASP.NET Core 8 (MVC) |
| Language | C# |
| UI | Razor Views, Bootstrap 5, Bootstrap Icons |
| Database | Microsoft SQL Server |
| Data access | ADO.NET (`Microsoft.Data.SqlClient`) — repository pattern, parameterized queries |
| API docs (Development) | Swagger + [Scalar](https://scalar.com/) |

**NuGet packages:** `Microsoft.Data.SqlClient`, `Swashbuckle.AspNetCore`, `Scalar.AspNetCore`

---

## Portals & Routes

| Portal | Base route | Purpose |
|--------|------------|---------|
| Public site | `/` | Home, admissions, and public content |
| Admin Portal | `/adminportal` | Staff dashboard, student/HR/fee/settings modules |
| Student Portal | `/studentportal` | Student login, dashboard, profile |
| Teacher Portal | `/teacherportal` | Teacher dashboard (scaffold) |
| Management Portal | `/ManagementPortal` | Legacy management route (preserved for existing links) |

### Admin modules

| Module | Status | Highlights |
|--------|--------|------------|
| Students Management | Active | Students CRUD, student logins, enrollment-related screens |
| Fee Management | Active | Fee heads, structures, challans, payments, receipts, concessions |
| HR Management | Active | Employees (live); payroll, leaves, attendance, tax (placeholders) |
| Settings | Active | `Configurations` key/value management |
| Accounts | Active | UI scaffold |
| Examination, Library, Transport, Hostel, Notifications, Reports, Admissions, Timetable | Planned | Shown on dashboard; not yet implemented |

---

## Repository Layout

```
VEMS/
├── Areas/
│   ├── AdminPortal/       # Admin UI, controllers, services, models
│   ├── StudentPortal/     # Student login, dashboard, profile
│   ├── TeacherPortal/     # Teacher portal scaffold
│   └── ManagementPortal/  # Legacy management area
├── Controllers/           # Public site controllers
├── Views/                 # Public Razor views
├── wwwroot/               # Static assets (CSS, JS, Bootstrap)
├── Docs/
│   ├── Database.md        # Full schema reference (v1.8)
│   ├── db.txt             # Table/column listing for quick lookup
│   └── DBworkflows.md     # Insert/update workflow notes
├── Scripts/               # SQL maintenance scripts
├── Properties/            # Launch profiles
├── Program.cs             # App startup, DI, routing, auth
└── VEMS.csproj
```

**Data layer:** Repositories live under `Areas/*/Services/` (e.g. `EmployeeRepository`, `FeeChallanRepository`). They read `IConfiguration` for database access at runtime — never hardcode credentials in source.

**Conventions:** See `.cursor/rules/database-rules.mdc` and `Docs/Database.md` for naming (`Uid` primary keys, CamelCase columns, `Configurations` as source of truth for status strings, `DECIMAL` for money).

---

## Database

SQL Server database **`VEMS`** with **25 application tables**, including:

| Domain | Tables |
|--------|--------|
| Config | `Configurations` |
| Auth | `EmployeeLogin`, `StudentsLogin` |
| Profiles | `Employee`, `Students`, `StudentParents`, `StudentContacts`, `StudentSiblings` |
| Reference | `ref_Countries`, `ref_Provinces`, `ref_Cities`, `ref_InstitutionTypes`, `ref_Programs`, `ref_FeeHeads` |
| Academic | `Sections`, `StudentEnrollments`, `TeacherSections` |
| Fees | `FeeStructures`, `FeeStructureDetails`, `Concessions` |
| Billing | `Challans`, `ChallanDetails`, `Payments`, `PaymentReceipts`, `ChallanReminders` |

Billing flow:

```
StudentEnrollments → ref_Programs → FeeStructures → FeeStructureDetails → ref_FeeHeads
                                              ↓
                                          Challans → ChallanDetails → Payments → PaymentReceipts
```

Full schema, foreign keys, and constraints: **[Docs/Database.md](Docs/Database.md)**  
Column-only reference: **[Docs/db.txt](Docs/db.txt)**

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (Express or full instance) with the `VEMS` database deployed
- IDE: Visual Studio 2022, VS Code, or Rider

---

## Quickstart

### 1. Clone and restore

```bash
git clone <repository-url>
cd VEMS
dotnet restore
```

### 2. Local configuration

`appsettings.Development.json` is gitignored. Create it from the template:

```bash
copy appsettings.Development.example.json appsettings.Development.json
```

Edit `appsettings.Development.json` and set your **local SQL Server** details so the app can reach the `VEMS` database. Do not commit this file.

Alternatively, use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (`UserSecretsId` is configured in `VEMS.csproj`) for machine-local overrides.

### 3. Run

```bash
dotnet run
```

### Access URLs (default HTTP profile)

| Resource | URL |
|----------|-----|
| Public site | http://localhost:5265 |
| Admin Portal | http://localhost:5265/adminportal |
| Student Portal | http://localhost:5265/studentportal |
| API docs (Development only) | http://localhost:5265/scalar |

HTTPS profile also binds **https://localhost:7102** when using the `https` launch profile.

---

## Authentication

| Portal | Mechanism |
|--------|-----------|
| Student Portal | Cookie authentication (`VEMS.StudentPortal.Auth`), 8-hour sliding session |
| Admin Portal | Server session (`VEMS.AdminPortal.Session`) for admin UI state |

Student accounts are created by staff via Admin Portal; students do not self-register.

---

## Documentation

| Document | Description |
|----------|-------------|
| [Docs/Database.md](Docs/Database.md) | Authoritative schema documentation |
| [Docs/db.txt](Docs/db.txt) | Flat table/column list synced with the live database |
| [Docs/DBworkflows.md](Docs/DBworkflows.md) | Employee/student creation and fee workflows |
| `.cursor/rules/database-rules.mdc` | AI/editor rules for database-related code |

When the database schema changes, update **both** `Docs/Database.md` and `Docs/db.txt` together.

---

## Developed by

- **Rayder-23**
- **Coditium Solutions**
