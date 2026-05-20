namespace VEMS.Areas.AdminPortal.Services.Examination;

public sealed class ExaminationModuleInfo
{
    public required string Controller { get; init; }
    public required string Segment { get; init; }
    public required string Name { get; init; }
    public required string IconClass { get; init; }
}

/// <summary>Sidebar + layout discovery for examination sub-modules.</summary>
public static class ExaminationModuleCatalog
{
    public static IReadOnlyList<ExaminationModuleInfo> SidebarModules { get; } =
    [
        new() { Controller = "ExamTypes", Segment = "exam-types", Name = "Exam Types", IconClass = "fa-tags" },
        new() { Controller = "GradingScales", Segment = "grading-scales", Name = "Grading Scales", IconClass = "fa-scale-balanced" },
        new() { Controller = "GradingScaleDetails", Segment = "grading-scale-details", Name = "Scale Details", IconClass = "fa-list-ol" },
        new() { Controller = "MarkingSchemes", Segment = "marking-schemes", Name = "Marking Schemes", IconClass = "fa-percent" },
        new() { Controller = "Exams", Segment = "exams", Name = "Exams", IconClass = "fa-file-lines" },
        new() { Controller = "ExamSchedules", Segment = "exam-schedules", Name = "Exam Schedule", IconClass = "fa-calendar-days" },
        new() { Controller = "StudentMarks", Segment = "student-marks", Name = "Student Marks", IconClass = "fa-pen-to-square" },
        new() { Controller = "AssignmentSubmissions", Segment = "assignment-submissions", Name = "Assignment Submissions", IconClass = "fa-paperclip" },
        new() { Controller = "StudentGrades", Segment = "student-grades", Name = "Student Grades", IconClass = "fa-graduation-cap" },
        new() { Controller = "StudentResults", Segment = "student-results", Name = "Student Results", IconClass = "fa-chart-line" },
        new() { Controller = "ResultDetails", Segment = "result-details", Name = "Result Details", IconClass = "fa-table-list" },
        new() { Controller = "GradeRemarks", Segment = "grade-remarks", Name = "Grade Remarks", IconClass = "fa-comment-dots" }
    ];

    public static ExaminationModuleInfo? TryGetByController(string controller) =>
        SidebarModules.FirstOrDefault(m =>
            string.Equals(m.Controller, controller, StringComparison.OrdinalIgnoreCase));

    public static string Url(ExaminationModuleInfo m) => $"/adminportal/examination/{m.Segment}";
}
