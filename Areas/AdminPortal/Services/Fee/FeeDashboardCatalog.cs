using VEMS.Areas.AdminPortal.Models.Fee;

namespace VEMS.Areas.AdminPortal.Services.Fee;

public static class FeeDashboardCatalog
{
    public static IReadOnlyList<FeeDashboardTile> Tiles { get; } =
    [
        new()
        {
            Title = "Fee Heads",
            Description = "Define tuition, exam, and other fee head codes.",
            Url = "/adminportal/fee/fee-heads",
            IconClass = "bi-tags-fill",
            AccentClass = "accent-red"
        },
        new()
        {
            Title = "Fee Structures",
            Description = "Program, semester, and academic year fee packages.",
            Url = "/adminportal/fee/fee-structures",
            IconClass = "bi-layers-fill",
            AccentClass = "accent-orange"
        },
        new()
        {
            Title = "Structure Details",
            Description = "Add fee heads and amounts to each structure.",
            Url = "/adminportal/fee/fee-structures",
            IconClass = "bi-list-columns",
            AccentClass = "accent-orange"
        },
        new()
        {
            Title = "Generate Challans",
            Description = "Create student challans from a fee structure.",
            Url = "/adminportal/fee/challans/create",
            IconClass = "bi-receipt-cutoff",
            AccentClass = "accent-teal"
        },
        new()
        {
            Title = "Challan List",
            Description = "View, search, and manage issued challans.",
            Url = "/adminportal/fee/challans",
            IconClass = "bi-receipt",
            AccentClass = "accent-cyan"
        },
        new()
        {
            Title = "Payments",
            Description = "Record partial or full payments against challans.",
            Url = "/adminportal/fee/payments",
            IconClass = "bi-cash-stack",
            AccentClass = "accent-green"
        },
        new()
        {
            Title = "Receipts",
            Description = "Payment receipts and printable vouchers.",
            Url = "/adminportal/fee/payment-receipts",
            IconClass = "bi-file-earmark-text",
            AccentClass = "accent-indigo"
        },
        new()
        {
            Title = "Concessions",
            Description = "Student discounts by percentage or fixed amount.",
            Url = "/adminportal/fee/concessions",
            IconClass = "bi-percent",
            AccentClass = "accent-purple"
        }
    ];
}
