/*
    VEMS — Bulk challan generation
    Run against the VEMS database before using bulk generation.
*/
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.usp_BulkGenerateChallans', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_BulkGenerateChallans;
GO

CREATE PROCEDURE dbo.usp_BulkGenerateChallans
    @ProgramID      INT,
    @StructureID    INT,
    @Semester       NVARCHAR(20),
    @AcademicYear   SMALLINT,
    @IssueDate      DATE,
    @DueDate        DATE,
    @CreatedBy      INT,
    @StudentIDs     NVARCHAR(MAX) = NULL  -- NULL = whole class; comma-separated Student Uids for selected students
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;

    DECLARE @Results TABLE
    (
        StudentID       INT             NOT NULL,
        RegistrationNo  NVARCHAR(30)    NULL,
        StudentName     NVARCHAR(200)   NULL,
        ChallanNo       NVARCHAR(30)    NULL,
        NetPayable      DECIMAL(10, 2)  NULL,
        Status          NVARCHAR(100)   NOT NULL
    );

    IF @ProgramID IS NULL OR @ProgramID <= 0
    BEGIN
        RAISERROR(N'Program is required.', 16, 1);
        RETURN;
    END;

    IF @StructureID IS NULL OR @StructureID <= 0
    BEGIN
        RAISERROR(N'Fee structure is required.', 16, 1);
        RETURN;
    END;

    IF @Semester IS NULL OR LTRIM(RTRIM(@Semester)) = N''
    BEGIN
        RAISERROR(N'Semester is required.', 16, 1);
        RETURN;
    END;

    IF @AcademicYear < 1900 OR @AcademicYear > 9999
    BEGIN
        RAISERROR(N'Academic year must be a valid 4-digit year.', 16, 1);
        RETURN;
    END;

    IF @IssueDate > @DueDate
    BEGIN
        RAISERROR(N'Issue date must be on or before due date.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.FeeStructures fs
        WHERE fs.Uid = @StructureID
          AND fs.ProgramID = @ProgramID
          AND fs.Semester = @Semester
          AND fs.AcademicYear = @AcademicYear
          AND fs.IsActive = 1
    )
    BEGIN
        RAISERROR(N'Fee structure does not belong to the selected program, semester, and academic year.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.FeeStructureDetails fsd
        WHERE fsd.StructureID = @StructureID
    )
    BEGIN
        RAISERROR(N'Fee structure has no line items. Add structure details first.', 16, 1);
        RETURN;
    END;

    DECLARE @Students TABLE
    (
        StudentID       INT NOT NULL PRIMARY KEY,
        RegistrationNo  NVARCHAR(30) NOT NULL,
        StudentName     NVARCHAR(200) NOT NULL,
        IsActive        BIT NOT NULL
    );

    IF @StudentIDs IS NULL OR LTRIM(RTRIM(@StudentIDs)) = N''
    BEGIN
        INSERT INTO @Students (StudentID, RegistrationNo, StudentName, IsActive)
        SELECT
            s.Uid,
            s.RegistrationNo,
            LTRIM(RTRIM(s.FirstName + ISNULL(N' ' + s.MiddleName, N'') + N' ' + s.LastName)),
            s.IsActive
        FROM dbo.Students s
        WHERE s.ProgramID = @ProgramID
          AND s.IsActive = 1;
    END
    ELSE
    BEGIN
        INSERT INTO @Students (StudentID, RegistrationNo, StudentName, IsActive)
        SELECT
            s.Uid,
            s.RegistrationNo,
            LTRIM(RTRIM(s.FirstName + ISNULL(N' ' + s.MiddleName, N'') + N' ' + s.LastName)),
            s.IsActive
        FROM dbo.Students s
        INNER JOIN
        (
            SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS StudentID
            FROM STRING_SPLIT(@StudentIDs, N',')
            WHERE LTRIM(RTRIM(value)) <> N''
        ) ids ON ids.StudentID = s.Uid
        WHERE s.ProgramID = @ProgramID;
    END;

    DECLARE
        @StudentID          INT,
        @RegistrationNo     NVARCHAR(30),
        @StudentName        NVARCHAR(200),
        @IsActive           BIT,
        @ChallanNo          NVARCHAR(30),
        @TotalAmount        DECIMAL(10, 2),
        @LineDiscountTotal  DECIMAL(10, 2),
        @OverallDiscount    DECIMAL(10, 2),
        @DiscountAmount     DECIMAL(10, 2),
        @NetPayable         DECIMAL(10, 2),
        @ChallanID          INT,
        @RetryCount         INT,
        @Prefix             NVARCHAR(30),
        @Seq                INT,
        @ErrMsg             NVARCHAR(4000);

    DECLARE student_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT StudentID, RegistrationNo, StudentName, IsActive
        FROM @Students
        ORDER BY RegistrationNo;

    OPEN student_cursor;
    FETCH NEXT FROM student_cursor INTO @StudentID, @RegistrationNo, @StudentName, @IsActive;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @IsActive = 0
        BEGIN
            INSERT INTO @Results (StudentID, RegistrationNo, StudentName, ChallanNo, NetPayable, Status)
            VALUES (@StudentID, @RegistrationNo, @StudentName, NULL, NULL, N'Skipped - Inactive');
        END
        ELSE IF EXISTS
        (
            SELECT 1
            FROM dbo.Challans c
            WHERE c.StudentID = @StudentID
              AND c.Semester = @Semester
              AND c.AcademicYear = @AcademicYear
              AND c.IsActive = 1
        )
        BEGIN
            INSERT INTO @Results (StudentID, RegistrationNo, StudentName, ChallanNo, NetPayable, Status)
            VALUES (@StudentID, @RegistrationNo, @StudentName, NULL, NULL, N'Skipped - Already Exists');
        END
        ELSE
        BEGIN
            BEGIN TRY
                BEGIN TRANSACTION;

                SET @TotalAmount = 0;
                SET @LineDiscountTotal = 0;
                SET @OverallDiscount = 0;

                DECLARE @LineItems TABLE
                (
                    FeeHeadID       SMALLINT        NOT NULL,
                    Amount          DECIMAL(10, 2)  NOT NULL,
                    DiscountAmount  DECIMAL(10, 2)  NOT NULL,
                    NetAmount       DECIMAL(10, 2)  NOT NULL
                );

                DELETE FROM @LineItems;

                ;WITH LineBase AS
                (
                    SELECT fsd.FeeHeadID, fsd.Amount
                    FROM dbo.FeeStructureDetails fsd
                    WHERE fsd.StructureID = @StructureID
                ),
                LineDiscounts AS
                (
                    SELECT
                        lb.FeeHeadID,
                        lb.Amount,
                        ISNULL(SUM(
                            CASE
                                WHEN c.DiscountPercent > 0 THEN ROUND(lb.Amount * c.DiscountPercent / 100.0, 2)
                                WHEN c.DiscountAmount > 0 THEN c.DiscountAmount
                                ELSE 0
                            END
                        ), 0) AS RawDiscount
                    FROM LineBase lb
                    LEFT JOIN dbo.Concessions c
                        ON c.StudentID = @StudentID
                       AND c.FeeHeadID = lb.FeeHeadID
                       AND c.IsActive = 1
                       AND c.ValidFrom <= @IssueDate
                       AND (c.ValidTo IS NULL OR c.ValidTo >= @IssueDate)
                    GROUP BY lb.FeeHeadID, lb.Amount
                )
                INSERT INTO @LineItems (FeeHeadID, Amount, DiscountAmount, NetAmount)
                SELECT
                    ld.FeeHeadID,
                    ld.Amount,
                    CASE WHEN ld.RawDiscount > ld.Amount THEN ld.Amount ELSE ld.RawDiscount END,
                    ld.Amount - CASE WHEN ld.RawDiscount > ld.Amount THEN ld.Amount ELSE ld.RawDiscount END
                FROM LineDiscounts ld;

                SELECT
                    @TotalAmount = ISNULL(SUM(Amount), 0),
                    @LineDiscountTotal = ISNULL(SUM(DiscountAmount), 0)
                FROM @LineItems;

                SELECT @OverallDiscount = ISNULL(SUM(
                    CASE
                        WHEN c.DiscountPercent > 0 THEN ROUND((@TotalAmount - @LineDiscountTotal) * c.DiscountPercent / 100.0, 2)
                        WHEN c.DiscountAmount > 0 THEN c.DiscountAmount
                        ELSE 0
                    END
                ), 0)
                FROM dbo.Concessions c
                WHERE c.StudentID = @StudentID
                  AND c.FeeHeadID IS NULL
                  AND c.IsActive = 1
                  AND c.ValidFrom <= @IssueDate
                  AND (c.ValidTo IS NULL OR c.ValidTo >= @IssueDate);

                IF @OverallDiscount > (@TotalAmount - @LineDiscountTotal)
                    SET @OverallDiscount = @TotalAmount - @LineDiscountTotal;

                SET @DiscountAmount = @LineDiscountTotal + @OverallDiscount;
                SET @NetPayable = @TotalAmount - @DiscountAmount;
                IF @NetPayable < 0
                    SET @NetPayable = 0;

                SET @Prefix = N'VEMS-' + CAST(@AcademicYear AS NVARCHAR(4)) + N'-';
                SET @RetryCount = 0;
                SET @ChallanNo = NULL;

                WHILE @RetryCount < 5 AND @ChallanNo IS NULL
                BEGIN
                    SELECT @Seq = ISNULL(MAX(TRY_CAST(RIGHT(c.ChallanNo, 4) AS INT)), 0) + 1
                    FROM dbo.Challans c WITH (UPDLOCK, HOLDLOCK)
                    WHERE c.ChallanNo LIKE @Prefix + N'%';

                    SET @ChallanNo = @Prefix + RIGHT(N'0000' + CAST(@Seq AS NVARCHAR(4)), 4);

                    IF EXISTS (SELECT 1 FROM dbo.Challans WHERE ChallanNo = @ChallanNo)
                    BEGIN
                        SET @ChallanNo = NULL;
                        SET @RetryCount += 1;
                    END
                END;

                IF @ChallanNo IS NULL
                    THROW 50001, N'Unable to allocate a unique challan number.', 1;

                INSERT INTO dbo.Challans
                (
                    ChallanNo, StudentID, StructureID, Semester, AcademicYear, IssueDate, DueDate,
                    TotalAmount, DiscountAmount, LateFineAmount, NetPayable, AmountPaid, Status, Remarks,
                    IsActive, CreatedBy, CreatedAt
                )
                VALUES
                (
                    @ChallanNo, @StudentID, @StructureID, @Semester, @AcademicYear, @IssueDate, @DueDate,
                    @TotalAmount, @DiscountAmount, 0, @NetPayable, 0, N'Unpaid', NULL,
                    1, @CreatedBy, SYSUTCDATETIME()
                );

                SET @ChallanID = CAST(SCOPE_IDENTITY() AS INT);

                INSERT INTO dbo.ChallanDetails
                (
                    ChallanID, FeeHeadID, Amount, DiscountAmount, LateFine, NetAmount, CreatedBy, CreatedAt
                )
                SELECT
                    @ChallanID,
                    li.FeeHeadID,
                    li.Amount,
                    li.DiscountAmount,
                    0,
                    li.NetAmount,
                    @CreatedBy,
                    SYSUTCDATETIME()
                FROM @LineItems li;

                COMMIT TRANSACTION;

                INSERT INTO @Results (StudentID, RegistrationNo, StudentName, ChallanNo, NetPayable, Status)
                VALUES (@StudentID, @RegistrationNo, @StudentName, @ChallanNo, @NetPayable, N'Generated');
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0
                    ROLLBACK TRANSACTION;

                SET @ErrMsg = ERROR_MESSAGE();
                INSERT INTO @Results (StudentID, RegistrationNo, StudentName, ChallanNo, NetPayable, Status)
                VALUES (@StudentID, @RegistrationNo, @StudentName, NULL, NULL, N'Error - ' + @ErrMsg);
            END CATCH
        END

        FETCH NEXT FROM student_cursor INTO @StudentID, @RegistrationNo, @StudentName, @IsActive;
    END

    CLOSE student_cursor;
    DEALLOCATE student_cursor;

    SELECT
        StudentID,
        RegistrationNo,
        StudentName,
        ChallanNo,
        NetPayable,
        Status
    FROM @Results
    ORDER BY RegistrationNo;
END
GO
