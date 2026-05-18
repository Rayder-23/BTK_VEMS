using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class FeeMgmtSubNavItem
{
    public required string Name { get; init; }

    public required string Action { get; init; }

    public string BuildUrl(string segment) =>
        string.Equals(Action, "Index", StringComparison.OrdinalIgnoreCase)
            ? $"/adminportal/fee/{segment}"
            : $"/adminportal/fee/{segment}/{Action.ToLowerInvariant()}";
}

public sealed class FeeMgmtModule
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Controller { get; init; }

    public required string Segment { get; init; }

    public required string IconClass { get; init; }

    public required string Description { get; init; }

    public required string AccentClass { get; init; }

    public required IReadOnlyList<FeeMgmtSubNavItem> SubNav { get; init; }

    public string Url => $"/adminportal/fee/{Segment}";
}

public static class FeeMgmtModuleCatalog
{
    public static IReadOnlyList<FeeMgmtModule> GridModules { get; } =
    [
        Module("RefFeeHeads", "FeeRefFeeHeads", "fee-heads", "bi-tags-fill", "Fee head definitions (ref_FeeHeads)", "accent-red",
            Sub("Add Fee Head", "Create"), Sub("All Fee Heads", "Index")),
        Module("FeeStructures", "FeeStructures", "fee-structures", "bi-layers-fill", "Program and term fee structures", "accent-orange",
            Sub("Add Structure", "Create"), Sub("All Structures", "Index")),
        Module("FeeStructureDetails", "FeeStructureDetails", "fee-structure-details", "bi-list-columns", "Line items per fee structure", "accent-orange",
            Sub("Add Detail", "Create"), Sub("All Details", "Index")),
        Module("Challans", "FeeChallans", "challans", "bi-receipt-cutoff", "Student fee challans", "accent-teal",
            Sub("Generate Challan", "Create"), Sub("All Challans", "Index")),
        Module("ChallanDetails", "FeeChallanDetails", "challan-details", "bi-receipt", "Challan line items", "accent-cyan",
            Sub("Add Line", "Create"), Sub("All Lines", "Index")),
        Module("Payments", "FeePayments", "payments", "bi-cash-stack", "Fee payment records", "accent-green",
            Sub("Record Payment", "Create"), Sub("All Payments", "Index")),
        Module("PaymentReceipts", "FeePaymentReceipts", "payment-receipts", "bi-file-earmark-text", "Payment receipts and vouchers", "accent-indigo",
            Sub("Add Receipt", "Create"), Sub("All Receipts", "Index")),
        Module("Concessions", "FeeConcessions", "concessions", "bi-percent", "Discounts and fee concessions", "accent-purple",
            Sub("Add Concession", "Create"), Sub("All Concessions", "Index"))
    ];

    public static FeeMgmtModule Get(string key) =>
        GridModules.First(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));

    public static FeeMgmtModule? TryGetByController(string controller) =>
        GridModules.FirstOrDefault(m =>
            string.Equals(m.Controller, controller, StringComparison.OrdinalIgnoreCase));

    public static AdminModulePageViewModel CreateListPage(string key)
    {
        var module = Get(key);
        return key switch
        {
            "RefFeeHeads" => CreatePage(module, "Add Fee Head",
                ["Code", "Name", "Type", "Status"],
                [
                    ["TUITION", "Tuition Fee", "Recurring", "Active"],
                    ["LAB", "Laboratory Fee", "Recurring", "Active"],
                    ["ADM", "Admission Fee", "One-time", "Active"]
                ]),
            "FeeStructures" => CreatePage(module, "Add Structure",
                ["Structure", "Program", "Term", "Status"],
                [
                    ["FS-2026-S1", "BSCS", "Spring 2026", "Active"],
                    ["FS-2026-S1", "BBA", "Spring 2026", "Active"],
                    ["FS-2025-F1", "Intermediate", "Fall 2025", "Draft"]
                ]),
            "FeeStructureDetails" => CreatePage(module, "Add Detail",
                ["Structure", "Fee Head", "Amount", "Status"],
                [
                    ["FS-2026-S1", "Tuition Fee", "45,000", "Active"],
                    ["FS-2026-S1", "Laboratory Fee", "5,000", "Active"],
                    ["FS-2026-S1", "Admission Fee", "10,000", "Active"]
                ]),
            "Challans" => CreatePage(module, "Generate Challan",
                ["Challan No", "Student", "Due Date", "Amount", "Status"],
                [
                    ["CH-2026-001", "Ayesha Khan", "2026-06-15", "50,000", "Pending"],
                    ["CH-2026-002", "Hamza Ali", "2026-06-15", "48,000", "Paid"],
                    ["CH-2026-003", "Fatima Noor", "2026-06-01", "42,000", "Overdue"]
                ]),
            "ChallanDetails" => CreatePage(module, "Add Line",
                ["Challan", "Fee Head", "Amount", "Status"],
                [
                    ["CH-2026-001", "Tuition Fee", "45,000", "Open"],
                    ["CH-2026-001", "Laboratory Fee", "5,000", "Open"],
                    ["CH-2026-002", "Tuition Fee", "42,000", "Paid"]
                ]),
            "Payments" => CreatePage(module, "Record Payment",
                ["Receipt", "Challan", "Amount", "Mode", "Date"],
                [
                    ["RCP-1001", "CH-2026-002", "48,000", "Cash", "2026-05-10"],
                    ["RCP-1002", "CH-2026-001", "25,000", "Bank", "2026-05-12"],
                    ["RCP-1003", "CH-2026-003", "20,000", "Online", "2026-05-14"]
                ]),
            "PaymentReceipts" => CreatePage(module, "Add Receipt",
                ["Receipt No", "Student", "Amount", "Issued", "Status"],
                [
                    ["RCP-1001", "Hamza Ali", "48,000", "2026-05-10", "Posted"],
                    ["RCP-1002", "Ayesha Khan", "25,000", "2026-05-12", "Posted"],
                    ["RCP-1003", "Fatima Noor", "20,000", "2026-05-14", "Draft"]
                ]),
            "Concessions" => CreatePage(module, "Add Concession",
                ["Student", "Fee Head", "Type", "Value", "Status"],
                [
                    ["Ayesha Khan", "Tuition Fee", "Percent", "10%", "Active"],
                    ["Hamza Ali", "Laboratory Fee", "Fixed", "2,000", "Active"],
                    ["Fatima Noor", "Tuition Fee", "Percent", "25%", "Pending"]
                ]),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown fee management module.")
        };
    }

    public static FeeMgmtFormPlaceholderViewModel CreateFormPage(string key, int? id = null)
    {
        var module = Get(key);
        return new FeeMgmtFormPlaceholderViewModel
        {
            ModuleName = module.Name,
            ModuleKey = module.Key,
            Segment = module.Segment,
            IconClass = module.IconClass,
            AccentClass = module.AccentClass,
            Description = module.Description,
            Id = id
        };
    }

    private static FeeMgmtModule Module(
        string key,
        string controller,
        string segment,
        string icon,
        string description,
        string accent,
        params FeeMgmtSubNavItem[] subNav) =>
        new()
        {
            Key = key,
            Name = key switch
            {
                "RefFeeHeads" => "ref_FeeHeads",
                "FeeStructures" => "FeeStructures",
                "FeeStructureDetails" => "FeeStructureDetails",
                "Challans" => "Challans",
                "ChallanDetails" => "ChallanDetails",
                "Payments" => "Payments",
                "PaymentReceipts" => "PaymentReceipts",
                _ => key
            },
            Controller = controller,
            Segment = segment,
            IconClass = icon,
            Description = description,
            AccentClass = accent,
            SubNav = subNav
        };

    private static FeeMgmtSubNavItem Sub(string name, string action) =>
        new() { Name = name, Action = action };

    private static AdminModulePageViewModel CreatePage(
        FeeMgmtModule module,
        string addButtonText,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        return new AdminModulePageViewModel
        {
            Title = module.Name,
            Description = module.Description,
            IconClass = module.IconClass,
            AccentClass = module.AccentClass,
            AddButtonText = addButtonText,
            TableHeaders = headers,
            TableRows = rows,
            ModuleSegment = module.Segment
        };
    }
}
