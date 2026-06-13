using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IProgramCourseRepository
{
    Task<IReadOnlyList<ProgramCourseListItem>> ListAsync(string? search, CancellationToken cancellationToken = default);

    Task<ProgramCourseFormModel?> GetAsync(int programCourseId, CancellationToken cancellationToken = default);

    Task<ProgramCourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int programId, int courseId, int? excludeProgramCourseId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ProgramCourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ProgramCourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int programCourseId, CancellationToken cancellationToken = default);
}
