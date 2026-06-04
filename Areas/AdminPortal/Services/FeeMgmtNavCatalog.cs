namespace VEMS.Areas.AdminPortal.Services;

public sealed class FeeMgmtNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public static class FeeMgmtNavCatalog
{
    public static IReadOnlyList<FeeMgmtNavItem> SidebarNav { get; } =
    [
        new() { Key = "dashboard", Name = "Dashboard", Url = "/adminportal/fee", IconClass = "fa-gauge-high" },
        new() { Key = "fee-heads", Name = "Fee Heads", Url = "/adminportal/fee/fee-heads", IconClass = "fa-tags" },
        new() { Key = "fee-structures", Name = "Fee Structures", Url = "/adminportal/fee/fee-structures", IconClass = "fa-layer-group" },
        new() { Key = "challans", Name = "Challans", Url = "/adminportal/fee/challans", IconClass = "fa-file-invoice-dollar" },
        new() { Key = "payments", Name = "Payments", Url = "/adminportal/fee/payments", IconClass = "fa-wallet" },
        new() { Key = "receipts", Name = "Receipts", Url = "/adminportal/fee/payment-receipts", IconClass = "fa-receipt" },
        new() { Key = "concessions", Name = "Concessions", Url = "/adminportal/fee/concessions", IconClass = "fa-percent" }
    ];

    private static readonly HashSet<string> FeeMgmtControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "FeeMgmt",
        "FeeRefFeeHeads",
        "FeeStructures",
        "FeeStructureDetails",
        "FeeChallans",
        "FeeChallanDetails",
        "FeePayments",
        "FeePaymentReceipts",
        "FeeConcessions"
    };

    public static bool IsFeeMgmtController(string controller) =>
        FeeMgmtControllers.Contains(controller)
        || FeeMgmtModuleCatalog.TryGetByController(controller) is not null;

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        action = action.ToLowerInvariant();

        if (path.Contains("/fee-heads", StringComparison.Ordinal))
        {
            return "fee-heads";
        }

        if (path.Contains("/fee-structures", StringComparison.Ordinal) || path.Contains("/fee-structure-details", StringComparison.Ordinal))
        {
            return "fee-structures";
        }

        if (path.Contains("/challans", StringComparison.Ordinal) || path.Contains("/challan-details", StringComparison.Ordinal))
        {
            return "challans";
        }

        if (path.Contains("/payment-receipts", StringComparison.Ordinal))
        {
            return "receipts";
        }

        if (path.Contains("/payments", StringComparison.Ordinal))
        {
            return "payments";
        }

        if (path.Contains("/concessions", StringComparison.Ordinal))
        {
            return "concessions";
        }

        if (path.EndsWith("/fee", StringComparison.Ordinal) || path.EndsWith("/fee/index", StringComparison.Ordinal))
        {
            return "dashboard";
        }

        return "dashboard";
    }
}
