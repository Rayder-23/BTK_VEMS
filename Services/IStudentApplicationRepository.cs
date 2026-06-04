using VEMS.Models;

namespace VEMS.Services;

public interface IStudentApplicationRepository
{
    Task<IReadOnlyList<StudentApplicationProgramOption>> GetActiveProgramsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetActiveCountryNamesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetActiveProvinceNamesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetActiveCityNamesAsync(CancellationToken cancellationToken = default);

    Task<string> InsertAsync(StudentApplicationFormModel model, CancellationToken cancellationToken = default);
}
