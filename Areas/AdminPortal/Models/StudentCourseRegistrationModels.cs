using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentCourseRegistrationListItem
{
    public int Uid { get; init; }
    public string RegistrationNo { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string CourseSectionDisplay { get; init; } = string.Empty;
    public DateTime RegistrationDate { get; init; }
}

public sealed class StudentCourseRegistrationLookups
{
    public IReadOnlyList<StudentLookupItem> Students { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> CourseSections { get; init; } = [];
}

public sealed class StudentCourseRegistrationFormPageViewModel
{
    public StudentCourseRegistrationFormModel Form { get; set; } = new();
    public StudentCourseRegistrationLookups Lookups { get; set; } = new();
}

public sealed class StudentCourseRegistrationFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Student is required.")]
    [Display(Name = "Student")]
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Course section is required.")]
    [Display(Name = "Course section")]
    [Range(1, int.MaxValue)]
    public int CourseSectionId { get; set; }

    [Required(ErrorMessage = "Registration date is required.")]
    [Display(Name = "Registration date")]
    [DataType(DataType.Date)]
    public DateTime RegistrationDate { get; set; } = DateTime.Today;
}
