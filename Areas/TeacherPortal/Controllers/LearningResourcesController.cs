using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class LearningResourcesController : TeacherPortalBaseController
{
    public IActionResult Notes() =>
        Placeholder("Notes", "Share lecture notes and study materials with students.");

    public IActionResult Videos() =>
        Placeholder("Videos", "Upload and manage instructional video content.");

    public IActionResult Materials() =>
        Placeholder("Materials", "Organize downloadable learning materials.");
}
