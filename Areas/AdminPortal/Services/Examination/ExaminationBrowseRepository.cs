using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models.Examination;

namespace VEMS.Areas.AdminPortal.Services.Examination;

public sealed class ExaminationBrowseRepository : IExaminationBrowseRepository
{
    private readonly string _connectionString;

    public ExaminationBrowseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    private static ExaminationListPageViewModel Build(string moduleKey, string title, List<string> headers, List<List<string>> rows) =>
        new()
        {
            ModuleKey = moduleKey,
            PageTitle = title,
            Headers = headers,
            Rows = rows.ConvertAll(static x => (IReadOnlyList<string>)x)
        };

    public async Task<ExaminationListPageViewModel> ListExamTypesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 Uid, TypeCode, TypeName, Description, IsActive
            FROM dbo.ExamTypes
            ORDER BY TypeCode;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "TypeCode"),
                ExaminationSql.Cell(r, "TypeName"),
                ExaminationSql.Cell(r, "Description"),
                ExaminationSql.ToBoolean(r, "IsActive") ? "Active" : "Inactive"
            ]);
        }

        return Build("ExamTypes", "Exam Types",
            ["Uid", "Code", "Name", "Description", "Status"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListGradingScalesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 Uid, ScaleName, ScaleType, MaxGPA, PassingMarks, IsDefault, IsActive
            FROM dbo.GradingScales
            ORDER BY ScaleName;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "ScaleName"),
                ExaminationSql.Cell(r, "ScaleType"),
                r.IsDBNull(r.GetOrdinal("MaxGPA")) ? "" : ExaminationSql.FmtDecimal(r, "MaxGPA"),
                ExaminationSql.FmtDecimal(r, "PassingMarks"),
                ExaminationSql.ToBoolean(r, "IsDefault") ? "Yes" : "No",
                ExaminationSql.ToBoolean(r, "IsActive") ? "Active" : "Inactive"
            ]);
        }

        return Build("GradingScales", "Grading Scales",
            ["Uid", "Scale", "Type", "Max GPA", "Pass Marks", "Default", "Status"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListGradingScaleDetailsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 d.Uid, s.ScaleName, d.LetterGrade, d.MinMarks, d.MaxMarks, d.GradePoints, d.Remarks, d.IsPassing
            FROM dbo.GradingScaleDetails d
            INNER JOIN dbo.GradingScales s ON s.Uid = d.ScaleID
            ORDER BY s.ScaleName, d.MinMarks DESC;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "ScaleName"),
                ExaminationSql.Cell(r, "LetterGrade"),
                ExaminationSql.FmtDecimal(r, "MinMarks"),
                ExaminationSql.FmtDecimal(r, "MaxMarks"),
                ExaminationSql.FmtDecimal(r, "GradePoints"),
                ExaminationSql.Cell(r, "Remarks"),
                ExaminationSql.ToBoolean(r, "IsPassing") ? "Yes" : "No"
            ]);
        }

        return Build("GradingScaleDetails", "Grading Scale Details",
            ["Uid", "Scale", "Letter", "Min %", "Max %", "Points", "Remarks", "Pass"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListMarkingSchemesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 m.Uid, m.SchemeName, p.ProgramName, m.CourseID, m.Semester, m.AcademicYear,
                   t.TypeName, m.WeightPercent, m.TotalMarks, m.IsActive
            FROM dbo.MarkingScheme m
            INNER JOIN dbo.ref_Programs p ON p.Uid = m.ProgramID
            INNER JOIN dbo.ExamTypes t ON t.Uid = m.ExamTypeID
            ORDER BY m.SchemeName, m.AcademicYear DESC;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "SchemeName"),
                ExaminationSql.Cell(r, "ProgramName"),
                ExaminationSql.Cell(r, "CourseID"),
                ExaminationSql.Cell(r, "Semester"),
                ExaminationSql.Cell(r, "AcademicYear"),
                ExaminationSql.Cell(r, "TypeName"),
                ExaminationSql.FmtDecimal(r, "WeightPercent"),
                ExaminationSql.FmtDecimal(r, "TotalMarks"),
                ExaminationSql.ToBoolean(r, "IsActive") ? "Active" : "Inactive"
            ]);
        }

        return Build("MarkingSchemes", "Marking Schemes",
            ["Uid", "Scheme", "Program", "Course ID", "Semester", "Year", "Exam Type", "Weight %", "Total Marks", "Status"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListExamsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 e.Uid, e.ExamTitle, t.TypeName, p.ProgramName, e.CourseID, e.Semester, e.AcademicYear,
                   e.TotalMarks, e.PassingMarks, e.ExamNo, e.IsActive
            FROM dbo.Exams e
            INNER JOIN dbo.ExamTypes t ON t.Uid = e.ExamTypeID
            INNER JOIN dbo.ref_Programs p ON p.Uid = e.ProgramID
            ORDER BY e.AcademicYear DESC, e.ExamTitle;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "ExamTitle"),
                ExaminationSql.Cell(r, "TypeName"),
                ExaminationSql.Cell(r, "ProgramName"),
                ExaminationSql.Cell(r, "CourseID"),
                ExaminationSql.Cell(r, "Semester"),
                ExaminationSql.Cell(r, "AcademicYear"),
                ExaminationSql.FmtDecimal(r, "TotalMarks"),
                ExaminationSql.FmtDecimal(r, "PassingMarks"),
                ExaminationSql.Cell(r, "ExamNo"),
                ExaminationSql.ToBoolean(r, "IsActive") ? "Active" : "Inactive"
            ]);
        }

        return Build("Exams", "Exams",
            ["Uid", "Title", "Type", "Program", "Course ID", "Semester", "Year", "Total", "Pass", "No.", "Status"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListExamSchedulesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 s.Uid, e.ExamTitle, s.ExamDate, s.StartTime, s.EndTime, s.RoomNo, s.Building, s.IsActive
            FROM dbo.ExamSchedule s
            INNER JOIN dbo.Exams e ON e.Uid = s.ExamID
            ORDER BY s.ExamDate DESC, s.StartTime;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "ExamTitle"),
                ExaminationSql.FmtDate(r, "ExamDate"),
                ExaminationSql.FmtTime(r, "StartTime"),
                ExaminationSql.FmtTime(r, "EndTime"),
                ExaminationSql.Cell(r, "RoomNo"),
                ExaminationSql.Cell(r, "Building"),
                ExaminationSql.ToBoolean(r, "IsActive") ? "Active" : "Inactive"
            ]);
        }

        return Build("ExamSchedules", "Exam Schedule",
            ["Uid", "Exam", "Date", "Start", "End", "Room", "Building", "Status"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListStudentMarksAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 m.Uid, e.ExamTitle, st.RegistrationNo, m.MarksObtained, m.IsAbsent, m.IsExcused, m.EnteredAt
            FROM dbo.StudentMarks m
            INNER JOIN dbo.Exams e ON e.Uid = m.ExamID
            INNER JOIN dbo.Students st ON st.Uid = m.StudentID
            ORDER BY m.EnteredAt DESC;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "ExamTitle"),
                ExaminationSql.Cell(r, "RegistrationNo"),
                ExaminationSql.FmtDecimal(r, "MarksObtained"),
                ExaminationSql.ToBoolean(r, "IsAbsent") ? "Yes" : "No",
                ExaminationSql.ToBoolean(r, "IsExcused") ? "Yes" : "No",
                ExaminationSql.FmtDateTime(r, "EnteredAt")
            ]);
        }

        return Build("StudentMarks", "Student Marks",
            ["Uid", "Exam", "Registration", "Marks", "Absent", "Excused", "Entered"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListAssignmentSubmissionsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 a.Uid, e.ExamTitle, st.RegistrationNo, a.SubmissionDate, a.DueDate, a.IsLate, a.FileName
            FROM dbo.AssignmentSubmissions a
            INNER JOIN dbo.Exams e ON e.Uid = a.ExamID
            INNER JOIN dbo.Students st ON st.Uid = a.StudentID
            ORDER BY a.SubmissionDate DESC;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "ExamTitle"),
                ExaminationSql.Cell(r, "RegistrationNo"),
                ExaminationSql.FmtDateTime(r, "SubmissionDate"),
                ExaminationSql.FmtDateTime(r, "DueDate"),
                ExaminationSql.ToBoolean(r, "IsLate") ? "Yes" : "No",
                ExaminationSql.Cell(r, "FileName")
            ]);
        }

        return Build("AssignmentSubmissions", "Assignment Submissions",
            ["Uid", "Exam", "Registration", "Submitted", "Due", "Late", "File"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListStudentGradesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 g.Uid, st.RegistrationNo, g.CourseID, p.ProgramName, g.Semester, g.AcademicYear,
                   s.ScaleName, g.PercentageMarks, g.LetterGrade, g.GradePoints, g.IsPassing
            FROM dbo.StudentGrades g
            INNER JOIN dbo.Students st ON st.Uid = g.StudentID
            INNER JOIN dbo.ref_Programs p ON p.Uid = g.ProgramID
            INNER JOIN dbo.GradingScales s ON s.Uid = g.ScaleID
            ORDER BY g.AcademicYear DESC, st.RegistrationNo;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "RegistrationNo"),
                ExaminationSql.Cell(r, "CourseID"),
                ExaminationSql.Cell(r, "ProgramName"),
                ExaminationSql.Cell(r, "Semester"),
                ExaminationSql.Cell(r, "AcademicYear"),
                ExaminationSql.Cell(r, "ScaleName"),
                ExaminationSql.FmtDecimal(r, "PercentageMarks"),
                ExaminationSql.Cell(r, "LetterGrade"),
                ExaminationSql.FmtDecimal(r, "GradePoints"),
                ExaminationSql.ToBoolean(r, "IsPassing") ? "Yes" : "No"
            ]);
        }

        return Build("StudentGrades", "Student Grades",
            ["Uid", "Registration", "Course", "Program", "Semester", "Year", "Scale", "%", "Grade", "Points", "Pass"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListStudentResultsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 r.Uid, st.RegistrationNo, p.ProgramName, r.Semester, r.AcademicYear,
                   r.TotalCourses, r.CoursesPassed, r.CoursesFailed, r.SemesterGPA, r.CumulativeCGPA, r.ResultStatus, r.IsLocked
            FROM dbo.StudentResults r
            INNER JOIN dbo.Students st ON st.Uid = r.StudentID
            INNER JOIN dbo.ref_Programs p ON p.Uid = r.ProgramID
            ORDER BY r.AcademicYear DESC, st.RegistrationNo;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "RegistrationNo"),
                ExaminationSql.Cell(r, "ProgramName"),
                ExaminationSql.Cell(r, "Semester"),
                ExaminationSql.Cell(r, "AcademicYear"),
                ExaminationSql.Cell(r, "TotalCourses"),
                ExaminationSql.Cell(r, "CoursesPassed"),
                ExaminationSql.Cell(r, "CoursesFailed"),
                ExaminationSql.FmtDecimal(r, "SemesterGPA"),
                ExaminationSql.FmtDecimal(r, "CumulativeCGPA"),
                ExaminationSql.Cell(r, "ResultStatus"),
                ExaminationSql.ToBoolean(r, "IsLocked") ? "Yes" : "No"
            ]);
        }

        return Build("StudentResults", "Student Results",
            ["Uid", "Registration", "Program", "Semester", "Year", "Courses", "Passed", "Failed", "SGPA", "CGPA", "Status", "Locked"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListResultDetailsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 d.Uid, st.RegistrationNo, sr.Semester, sr.AcademicYear, d.CourseID, d.LetterGrade, d.GradePoints, d.QualityPoints, d.IsPassing
            FROM dbo.ResultDetails d
            INNER JOIN dbo.StudentResults sr ON sr.Uid = d.ResultID
            INNER JOIN dbo.Students st ON st.Uid = d.StudentID
            ORDER BY sr.AcademicYear DESC, st.RegistrationNo;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "RegistrationNo"),
                ExaminationSql.Cell(r, "Semester"),
                ExaminationSql.Cell(r, "AcademicYear"),
                ExaminationSql.Cell(r, "CourseID"),
                ExaminationSql.Cell(r, "LetterGrade"),
                ExaminationSql.FmtDecimal(r, "GradePoints"),
                ExaminationSql.FmtDecimal(r, "QualityPoints"),
                ExaminationSql.ToBoolean(r, "IsPassing") ? "Yes" : "No"
            ]);
        }

        return Build("ResultDetails", "Result Details",
            ["Uid", "Registration", "Semester", "Year", "Course", "Grade", "Points", "Quality", "Pass"], rows);
    }

    public async Task<ExaminationListPageViewModel> ListGradeRemarksAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP 500 g.Uid, g.RemarkCode, g.RemarkName, s.ScaleName, g.MinCGPA, g.MaxCGPA, g.MinPercentage, g.MaxPercentage, g.IsActive
            FROM dbo.GradeRemarks g
            INNER JOIN dbo.GradingScales s ON s.Uid = g.ScaleID
            ORDER BY g.RemarkCode;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        var rows = new List<List<string>>();
        while (await r.ReadAsync(cancellationToken))
        {
            rows.Add(
            [
                ExaminationSql.Cell(r, "Uid"),
                ExaminationSql.Cell(r, "RemarkCode"),
                ExaminationSql.Cell(r, "RemarkName"),
                ExaminationSql.Cell(r, "ScaleName"),
                r.IsDBNull(r.GetOrdinal("MinCGPA")) ? "" : ExaminationSql.FmtDecimal(r, "MinCGPA"),
                r.IsDBNull(r.GetOrdinal("MaxCGPA")) ? "" : ExaminationSql.FmtDecimal(r, "MaxCGPA"),
                r.IsDBNull(r.GetOrdinal("MinPercentage")) ? "" : ExaminationSql.FmtDecimal(r, "MinPercentage"),
                r.IsDBNull(r.GetOrdinal("MaxPercentage")) ? "" : ExaminationSql.FmtDecimal(r, "MaxPercentage"),
                ExaminationSql.ToBoolean(r, "IsActive") ? "Active" : "Inactive"
            ]);
        }

        return Build("GradeRemarks", "Grade Remarks",
            ["Uid", "Code", "Name", "Scale", "Min CGPA", "Max CGPA", "Min %", "Max %", "Status"], rows);
    }
}
