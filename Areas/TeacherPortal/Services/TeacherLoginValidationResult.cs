namespace VEMS.Areas.TeacherPortal.Services;

public sealed class TeacherLoginValidationResult
{
    public TeacherLoginUser? User { get; init; }

    public TeacherLoginFailureReason? FailureReason { get; init; }

    public static TeacherLoginValidationResult Success(TeacherLoginUser user) =>
        new() { User = user };

    public static TeacherLoginValidationResult Failure(TeacherLoginFailureReason reason) =>
        new() { FailureReason = reason };
}
