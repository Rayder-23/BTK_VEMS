namespace VEMS.Areas.AdminPortal.Services.Fee;

internal static class FeeStatusHelper
{
    public static string ComputeChallanStatus(decimal netPayable, decimal amountPaid, DateOnly dueDate, string storedStatus)
    {
        if (string.Equals(storedStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return "Cancelled";
        }

        if (amountPaid <= 0)
        {
            return dueDate < DateOnly.FromDateTime(DateTime.Today) ? "Overdue" : "Unpaid";
        }

        if (amountPaid >= netPayable)
        {
            return "Paid";
        }

        return dueDate < DateOnly.FromDateTime(DateTime.Today) ? "Overdue" : "Partial";
    }

    public static string ResolveStoredStatus(decimal netPayable, decimal amountPaid, DateOnly dueDate, string? currentStatus)
    {
        if (string.Equals(currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return "Cancelled";
        }

        if (amountPaid >= netPayable && netPayable > 0)
        {
            return "Paid";
        }

        if (amountPaid > 0)
        {
            return "Partial";
        }

        return dueDate < DateOnly.FromDateTime(DateTime.Today) ? "Overdue" : "Unpaid";
    }
}
