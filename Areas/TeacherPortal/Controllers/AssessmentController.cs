using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class AssessmentController : TeacherPortalBaseController
{
    public IActionResult Assignments() =>
        Placeholder("Assignments", "Create, distribute, and grade student assignments.");

    public IActionResult Exams() =>
        Placeholder("Exams", "Schedule and manage examinations.");

    public IActionResult GradeBook() =>
        Placeholder("Grade Book", "Enter and review consolidated student grades.");
}
