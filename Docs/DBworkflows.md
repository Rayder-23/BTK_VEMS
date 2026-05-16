# Database Workflow Documentation: Virtual Education Management System

- *VERSION: 1.0*
- *UPDATED AT: 2026-05-16 11:05am*

## Employee Creation Workflow

When a new staff member is added, two records are created in sequence:

1. **Employee** record is inserted first — captures the person's identity and profile.
2. **EmployeeLogin** record is inserted second — links to the Employee and grants system access.

`CreatedBy` on both records stores the `Uid` of the **EmployeeLogin** record of the admin performing the action.

### Insert Order & Field Sources

| Step | Table         | Key Fields Populated                                                                                                                    |
| :--- | :------------ | :-------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | Employee      | EmployeeId (generated), FullName, Email, CNIC, Department, Designation, EmployeeType, Status, JoinedDate, CreatedBy (admin's Login Uid) |
| 2    | EmployeeLogin | EmployeeId (FK → Employee.Uid), Username, PasswordHash, Role, Status, CreatedBy (admin's Login Uid)                                     |

### Notes

- `Employee` must exist before `EmployeeLogin` can be created (FK constraint enforces this).
- `Role` and `Status` values must match a valid entry in the `Configurations` table before insert.
- `ModifiedBy` and `ModifiedAt` on `Employee` are only populated on subsequent edits, not on creation.
- Deleting an `Employee` record will cascade and automatically remove the linked `EmployeeLogin` record.
- A login account can be disabled (`Status = 'Inactive'`) independently without deleting the Employee record, for suspension scenarios.

## Student Creation Workflow

When a new student is added, two records are created in sequence:

1. **Students** record is inserted first — captures the student's identity and profile.
2. **StudentsLogin** record is inserted second — links to the Student and grants portal access.

`CreatedBy` on both records stores the `Uid` of the **EmployeeLogin** record of the staff member performing the action.

### Insert Order & Field Sources

| Step | Table         | Key Fields Populated                                                                                                       |
| :--- | :------------ | :------------------------------------------------------------------------------------------------------------------------- |
| 1    | Students      | FullName, Email, Phone, GuardianName, GuardianPhone, GradeLevel, City, Status, EnrolledDate, CreatedBy (staff's Login Uid) |
| 2    | StudentsLogin | StudentId (FK → Students.Uid), Username, PasswordHash, Status, CreatedBy (staff's Login Uid)                               |

### Notes

- `Students` must exist before `StudentsLogin` can be created (FK constraint enforces this).
- `Status` values must match a valid entry under the `StudentStatus` key in the `Configurations` table before insert.
- `GradeLevel` must match a valid entry under the `GradeLevels` key in the `Configurations` table.
- `ModifiedBy` and `ModifiedAt` on `Students` are only populated on subsequent edits, not on creation.
- Deleting a `Students` record will cascade and automatically remove the linked `StudentsLogin` record.
- A login account can be disabled (`Status = 'Inactive'`) independently without deleting the Student record, for suspension scenarios.
- Unlike employees, students do not self-register — all student accounts are created by a staff member.