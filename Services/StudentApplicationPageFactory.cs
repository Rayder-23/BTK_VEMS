using VEMS.Models;

namespace VEMS.Services;

public sealed class StudentApplicationPageFactory : IStudentApplicationPageFactory
{
    private readonly IStudentApplicationRepository _applications;
    private readonly IConfigurationValuesProvider _configValues;

    public StudentApplicationPageFactory(
        IStudentApplicationRepository applications,
        IConfigurationValuesProvider configValues)
    {
        _applications = applications;
        _configValues = configValues;
    }

    public async Task<StudentApplicationPageViewModel> CreateAsync(
        StudentApplicationFormModel? form = null,
        CancellationToken cancellationToken = default)
    {
        var programs = await _applications.GetActiveProgramsAsync(cancellationToken);
        var countries = await _applications.GetActiveCountryNamesAsync(cancellationToken);
        var provinces = await _applications.GetActiveProvinceNamesAsync(cancellationToken);
        var cities = await _applications.GetActiveCityNamesAsync(cancellationToken);
        var genders = await _configValues.GetValuesAsync("Genders", cancellationToken);
        var model = form ?? CreateDefaultForm();

        if (model.DesiredYear == 0)
        {
            model.DesiredYear = (short)DateTime.UtcNow.Year;
        }

        if (model.DesiredGradeOrSemester == 0)
        {
            model.DesiredGradeOrSemester = 1;
        }

        if (string.IsNullOrWhiteSpace(model.Gender) && genders.Count > 0)
        {
            model.Gender = genders[0];
        }

        if (string.IsNullOrWhiteSpace(model.Country))
        {
            model.Country = "Pakistan";
        }

        return new StudentApplicationPageViewModel
        {
            Form = model,
            Programs = programs,
            Genders = genders,
            Countries = countries,
            Provinces = provinces,
            Cities = cities
        };
    }

    private static StudentApplicationFormModel CreateDefaultForm() =>
        new()
        {
            DesiredYear = (short)DateTime.UtcNow.Year,
            DesiredGradeOrSemester = 1,
            Country = "Pakistan"
        };
}
