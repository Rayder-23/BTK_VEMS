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

    public static IEnumerable<DateOnly> EnumerateBillingMonths(DateOnly fromPeriod, DateOnly toPeriod)
    {
        var start = new DateOnly(fromPeriod.Year, fromPeriod.Month, 1);
        var end = new DateOnly(toPeriod.Year, toPeriod.Month, 1);
        for (var current = start; current <= end; current = current.AddMonths(1))
        {
            yield return current;
        }
    }

    public static DateOnly NormalizeBillingMonth(DateOnly value) =>
        new(value.Year, value.Month, 1);

    public static bool TryParseMonthInput(string? value, out DateOnly period)
    {
        period = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return DateOnly.TryParse($"{value.Trim()}-01", out period);
    }

    public static string FormatBillingRange(DateOnly fromPeriod, DateOnly toPeriod)
    {
        var from = NormalizeBillingMonth(fromPeriod);
        var to = NormalizeBillingMonth(toPeriod);
        return from == to
            ? from.ToString("MMMM yyyy")
            : $"{from:MMMM yyyy} - {to:MMMM yyyy}";
    }

    public static void ValidateBillingRange(DateOnly fromPeriod, DateOnly toPeriod)
    {
        var from = NormalizeBillingMonth(fromPeriod);
        var to = NormalizeBillingMonth(toPeriod);
        if (to < from)
        {
            throw new InvalidOperationException("To month must be on or after From month.");
        }
    }
}
