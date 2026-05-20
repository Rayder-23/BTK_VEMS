using VEMS.Areas.AdminPortal.Models.Examination;

namespace VEMS.Areas.AdminPortal.Services.Examination;

public static class ExaminationDashboardCatalog
{
    public static IReadOnlyList<ExaminationDashboardTile> Tiles { get; } =
    [
        new()
        {
            Title = "Exam Setup",
            Description = "Exam types, grading scales, and grade remark rules.",
            Url = "/adminportal/examination/exam-types",
            IconClass = "bi-tags-fill",
            AccentClass = "accent-teal"
        },
        new()
        {
            Title = "Marking & Scheduling",
            Description = "Course marking schemes, exam papers, and exam schedule.",
            Url = "/adminportal/examination/marking-schemes",
            IconClass = "bi-journal-text",
            AccentClass = "accent-orange"
        },
        new()
        {
            Title = "Marks & Submissions",
            Description = "Enter student marks and track assignment submissions.",
            Url = "/adminportal/examination/student-marks",
            IconClass = "bi-pencil-square",
            AccentClass = "accent-cyan"
        },
        new()
        {
            Title = "Results & Grades",
            Description = "Course grades, semester results, and official result lines.",
            Url = "/adminportal/examination/student-grades",
            IconClass = "bi-award-fill",
            AccentClass = "accent-indigo"
        }
    ];
}
