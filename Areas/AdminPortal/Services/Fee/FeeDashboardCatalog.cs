using VEMS.Areas.AdminPortal.Models.Fee;

namespace VEMS.Areas.AdminPortal.Services.Fee;

public static class FeeDashboardCatalog
{
    public static IReadOnlyList<FeeDashboardTile> Tiles { get; } =
    [
        new()
        {
            Sequence = 1,
            Title = "Fee Structures",
            Description = "Program, semester, and academic year fee packages.",
            Url = "/adminportal/fee/fee-structures",
            IconClass = "bi-layers-fill",
            AccentClass = "accent-orange"
        },
        new()
        {
            Sequence = 2,
            Title = "Challan Management",
            Description = "View, search, generate, and manage issued challans.",
            Url = "/adminportal/fee/challans",
            IconClass = "bi-receipt",
            AccentClass = "accent-cyan"
        },
        new()
        {
            Sequence = 3,
            Title = "Payments and Receipts",
            Description = "Record payments, view the ledger, and open printable receipts.",
            Url = "/adminportal/fee/payments",
            IconClass = "bi-cash-stack",
            AccentClass = "accent-green"
        }
    ];
}
