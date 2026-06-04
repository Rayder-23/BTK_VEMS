using VEMS.Areas.AdminPortal.Models.Admissions;

namespace VEMS.Areas.AdminPortal.Services.Admissions;

internal static class StudentConversionValidator
{
    public static void ValidateApplicationFields(
        StudentApplicationFormModel application,
        IReadOnlyList<string> allowedGenders)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(application.ProgramCode))
        {
            errors.Add("Program code is required on the application.");
        }

        if (!application.ApplicationDate.HasValue)
        {
            errors.Add("Application date is required.");
        }

        if (!application.DateOfBirth.HasValue)
        {
            errors.Add("Date of birth is required.");
        }

        if (string.IsNullOrWhiteSpace(application.FirstName))
        {
            errors.Add("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(application.LastName))
        {
            errors.Add("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(application.FatherName))
        {
            errors.Add("Father name is required.");
        }

        if (string.IsNullOrWhiteSpace(application.AddressLine1))
        {
            errors.Add("Address is required.");
        }

        if (string.IsNullOrWhiteSpace(application.Gender))
        {
            errors.Add("Gender is required (use M, F, or O).");
        }
        else
        {
            var genderCode = NormalizeGenderCode(application.Gender);
            if (allowedGenders.Count > 0
                && !allowedGenders.Any(g => string.Equals(g, genderCode, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(
                    $"Gender '{application.Gender.Trim()}' is not valid. Allowed values: {string.Join(", ", allowedGenders)}.");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }
    }

    public static string NormalizeGenderCode(string gender)
    {
        var trimmed = gender.Trim();
        if (trimmed.Length == 1)
        {
            return trimmed.ToUpperInvariant();
        }

        if (trimmed.StartsWith("Male", StringComparison.OrdinalIgnoreCase))
        {
            return "M";
        }

        if (trimmed.StartsWith("Female", StringComparison.OrdinalIgnoreCase))
        {
            return "F";
        }

        if (trimmed.StartsWith("Other", StringComparison.OrdinalIgnoreCase))
        {
            return "O";
        }

        throw new InvalidOperationException(
            $"Gender '{gender}' must be a single letter (M, F, or O) from Configurations → Genders.");
    }
}
