# Database Workflow Documentation: Virtual Education Management System

- *VERSION: 1.1*
- *UPDATED AT: 2026-05-16 12:23pm*

---

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

---

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

---

## Fee & Challan Workflows

### Core Concepts
- A **Challan** is a payment voucher generated for a student covering one billing period.
- A **ChallanItem** is a line item on a challan, one row per fee applicable to that student.
- A **FeePayment** is a recorded payment transaction against a challan. One challan can have many payments.

---

### Workflow 1: Challan Generation

| Step | Table                  | Action                                                                                          |
| :--- | :--------------------- | :---------------------------------------------------------------------------------------------- |
| 1    | StudentEnrollments     | Query active enrollments for the student to get their Programs                                  |
| 2    | FeeStructures          | For each Program, fetch active FeeStructures matching the current AcademicYear                  |
| 3    | FeeTypes               | Pulled via FeeStructures — provides FeeName, Category, Frequency                               |
| 4    | StudentFeeAllocations  | Check for any discount allocations linked to the student and FeeStructure                       |
| 5    | Challans               | Insert one Challan record with TotalAmount, IssueDate, DueDate, Status = `'Unpaid'`            |
| 6    | ChallanItems           | Insert one row per applicable fee, snapshotting FeeName, Amount, Discount and NetAmount         |

### Notes
- `ChallanItems.FeeName` is a snapshot — it stores the name at generation time so historical vouchers are unaffected by future FeeType edits.
- `Challans.TotalAmount` must equal the sum of all `ChallanItems.NetAmount`.
- `Challans.PaidAmount` starts at 0.00 on creation.

---

### Workflow 2: Full Payment

| Step | Table       | Action                                                                                  |
| :--- | :---------- | :-------------------------------------------------------------------------------------- |
| 1    | FeePayments | Insert payment record with full AmountPaid, PaymentMode, ReceiptNo, CollectedBy        |
| 2    | Challans    | Update PaidAmount = TotalAmount, Status = `'Paid'`, PaidDate = today, PaymentMode      |

---

### Workflow 3: Partial Payment

| Step | Table       | Action                                                                                           |
| :--- | :---------- | :----------------------------------------------------------------------------------------------- |
| 1    | FeePayments | Insert payment record with partial AmountPaid                                                    |
| 2    | Challans    | Update PaidAmount = PaidAmount + new payment, Status = `'Partial'`                               |
| 3    | FeePayments | On subsequent payments, repeat Step 1 — multiple FeePayments rows per ChallanId are valid        |
| 4    | Challans    | On final payment, Update PaidAmount = TotalAmount, Status = `'Paid'`, PaidDate = today           |

### Notes
- The voucher re-generation reads `Challans.Balance` (computed: `TotalAmount - PaidAmount`) to display the remaining amount due.
- `PaidAmount` must be updated by the application on every FeePayments insert — there is no trigger enforcing this automatically.

---

### Workflow 4: Due Date Extension

| Step | Table    | Action                                                                                              |
| :--- | :------- | :-------------------------------------------------------------------------------------------------- |
| 1    | Challans | Append current DueDate to DueDateHistory (CSV audit trail), then update DueDate to new date         |

### Notes
- Only an authorised staff member via EmployeeLogin should be permitted to perform this action at the application layer.

---

### Workflow 5: Late Fee on Voucher

| Step | Table         | Action                                                                                          |
| :--- | :------------ | :---------------------------------------------------------------------------------------------- |
| 1    | Challans      | At voucher generation time, check if current date > DueDate                                     |
| 2    | FeeStructures | If overdue, read LateFeeAmount and LateFeeDays for each linked FeeStructure                     |
| 3    | Challans      | If days overdue exceed LateFeeDays grace period, calculate and display surcharge on the voucher  |
| 4    | ChallanItems  | Optionally insert a late fee line item to formally record the surcharge against the challan      |

### Notes
- Late fee calculation: if `GETDATE() > DueDate` by more than `LateFeeDays`, apply `LateFeeAmount` per applicable FeeStructure.
- Whether the surcharge is display-only or written as a ChallanItem is an application-layer decision. Writing it as a ChallanItem and updating `Challans.TotalAmount` is recommended for accurate record keeping.
- Challans.Status should be updated to `'Overdue'` when past due date and unpaid.