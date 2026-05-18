using VEMS.Areas.AdminPortal.Models.Fee;

namespace VEMS.Areas.AdminPortal.Services.Fee;

public interface IFeeLookupRepository
{
    Task<IReadOnlyList<FeeLookupItem>> GetProgramsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeLookupItem>> GetActiveStudentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeLookupItem>> GetActiveFeeHeadsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeLookupItem>> GetActiveStructuresAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeLookupItem>> GetUnpaidChallansAsync(CancellationToken cancellationToken = default);
}

public interface IFeeHeadRepository
{
    Task<IReadOnlyList<FeeHeadListItem>> ListAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default);
    Task<FeeHeadFormModel?> GetAsync(short uid, CancellationToken cancellationToken = default);
    Task<bool> HeadCodeExistsAsync(string headCode, short? excludeUid, CancellationToken cancellationToken = default);
    Task<short> InsertAsync(FeeHeadFormModel model, int createdBy, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(FeeHeadFormModel model, int? updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(short uid, int? updatedBy, CancellationToken cancellationToken = default);
}

public interface IFeeStructureRepository
{
    Task<IReadOnlyList<FeeStructureListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<FeeStructureFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(short programId, string semester, short academicYear, int? excludeUid, CancellationToken cancellationToken = default);
    Task<int> InsertAsync(FeeStructureFormModel model, int createdBy, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(FeeStructureFormModel model, int? updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default);
    Task<FeeStructureDetailsPageModel?> GetDetailsPageAsync(int structureId, CancellationToken cancellationToken = default);
    Task<bool> DetailExistsAsync(int structureId, short feeHeadId, int? excludeUid, CancellationToken cancellationToken = default);
    Task<int> AddDetailAsync(FeeStructureDetailFormModel model, int createdBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteDetailAsync(int detailUid, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeStructureDetailLine>> GetDetailsForStructureAsync(int structureId, CancellationToken cancellationToken = default);
}

public interface IFeeChallanRepository
{
    Task<IReadOnlyList<ChallanListItem>> ListAsync(string? search, CancellationToken cancellationToken = default);
    Task<ChallanDetailsPageModel?> GetDetailsAsync(int challanId, CancellationToken cancellationToken = default);
    Task<int> GenerateChallanAsync(ChallanGenerateFormModel model, int createdBy, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(int challanId, int? updatedBy, CancellationToken cancellationToken = default);
    Task RecalculateStatusAsync(int challanId, CancellationToken cancellationToken = default);
}

public interface IFeePaymentRepository
{
    Task<IReadOnlyList<PaymentListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentListItem>> ListReceiptsAsync(CancellationToken cancellationToken = default);
    Task<PaymentFormModel?> GetChallanForPaymentAsync(int challanId, CancellationToken cancellationToken = default);
    Task<int> RecordPaymentAsync(PaymentFormModel model, int createdBy, CancellationToken cancellationToken = default);
    Task<PaymentReceiptViewModel?> GetReceiptAsync(int paymentId, CancellationToken cancellationToken = default);
    Task IncrementPrintCountAsync(int paymentId, CancellationToken cancellationToken = default);
}

public interface IFeeConcessionRepository
{
    Task<IReadOnlyList<ConcessionListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<ConcessionFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);
    Task<int> InsertAsync(ConcessionFormModel model, int createdBy, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ConcessionFormModel model, int? updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default);
    Task<decimal> GetApplicableDiscountForHeadAsync(int studentId, short feeHeadId, decimal amount, DateOnly asOf, CancellationToken cancellationToken = default);
}
