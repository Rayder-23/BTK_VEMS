using VEMS.Models;

namespace VEMS.Services;

public interface IStudentApplicationPageFactory
{
    Task<StudentApplicationPageViewModel> CreateAsync(
        StudentApplicationFormModel? form = null,
        CancellationToken cancellationToken = default);
}
