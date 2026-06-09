using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

[Area("TeacherPortal")]
[Authorize(AuthenticationSchemes = TeacherPortalAuth.Scheme)]
public abstract class TeacherPortalBaseController : Controller
{
}
