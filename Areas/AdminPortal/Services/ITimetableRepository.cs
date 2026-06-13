using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ITimetableRepository
{
    Task<TimetableLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TimetableSlotListItem>> ListAsync(
        int? classId,
        int? teacherId,
        string semester,
        short academicYear,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
}
