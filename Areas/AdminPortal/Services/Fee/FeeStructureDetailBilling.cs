using VEMS.Areas.AdminPortal.Models.Fee;

namespace VEMS.Areas.AdminPortal.Services.Fee;

internal static class FeeStructureDetailBilling
{
    public static bool AppliesToBillingPeriod(FeeStructureDetailLine line, DateOnly billingPeriod)
    {
        var frequency = line.Frequency?.Trim() ?? string.Empty;

        if (string.Equals(frequency, "Monthly", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(frequency, "OneTime", StringComparison.Ordinal))
        {
            return line.ApplicableMonth.HasValue
                && line.ApplicableYear.HasValue
                && line.ApplicableMonth.Value == billingPeriod.Month
                && line.ApplicableYear.Value == billingPeriod.Year;
        }

        return false;
    }

    public static List<FeeStructureDetailLine> FilterForBillingPeriod(
        IEnumerable<FeeStructureDetailLine> lines,
        DateOnly billingPeriod) =>
        lines.Where(line => AppliesToBillingPeriod(line, billingPeriod)).ToList();
}
