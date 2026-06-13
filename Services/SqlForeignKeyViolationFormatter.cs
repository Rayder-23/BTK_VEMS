using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace VEMS.Services;

/// <summary>
/// Turns SQL Server FK violation (547) into admin-readable guidance.
/// </summary>
public static class SqlForeignKeyViolationFormatter
{
    private static readonly Dictionary<string, string> ConstraintHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FK_Students_Program"] =
            "Program: the application's Program code must match an active program in Reference Data (Programs). Edit the applicant's program or activate the program in the system.",
        ["FK_Students_Country"] =
            "Country: no matching active country was found in ref_Countries. Ensure reference countries exist and are active.",
        ["FK_Students_Province"] =
            "Province: the province on the application does not link to ref_Provinces. Correct the applicant's Province to an existing name (e.g. Punjab, Sindh) or add the province in reference data.",
        ["FK_Students_City"] =
            "City: the city on the application does not link to ref_Cities. Correct the applicant's City to an existing name (e.g. Karachi, Lahore) or add the city in reference data.",
        ["FK_Students_Section"] =
            "Section: an invalid section was specified. Leave section empty when converting from an application.",
        ["FK_Students_Blood"] =
            "Blood group reference is invalid.",
        ["FK_Students_Religion"] =
            "Religion reference is invalid."
    };

    public static string Describe(SqlException ex)
    {
        if (ex.Number != 547)
        {
            return ex.Message;
        }

        if (ex.Message.Contains("CHECK constraint", StringComparison.OrdinalIgnoreCase))
        {
            var check = TryExtractConstraintName(ex.Message);
            if (string.Equals(check, "CK_StudentApplications_Status", StringComparison.OrdinalIgnoreCase))
            {
                return "Could not create student — application status 'Converted As Student' is not allowed on the database. Run Scripts/Fix_StudentApplications_ConvertStatus.sql and Scripts/Seed_ApplicationConfigurations.sql.";
            }

            if (string.Equals(check, "CK_StudentApplications_Gender", StringComparison.OrdinalIgnoreCase))
            {
                return "Could not create student — gender must be M, F, or O.";
            }

            return $"Could not create student — a database rule blocked the update ({check ?? "CHECK constraint"}).";
        }

        var constraint = TryExtractConstraintName(ex.Message);
        if (!string.IsNullOrEmpty(constraint) && ConstraintHints.TryGetValue(constraint, out var hint))
        {
            return $"Could not create student — {hint}";
        }

        var referenced = TryExtractReferencedTable(ex.Message);
        if (!string.IsNullOrEmpty(referenced))
        {
            return $"Could not create student — a value does not exist in {referenced}. Check the applicant's program, city, province, and country against reference data.";
        }

        return "Could not create student — a related reference record is missing or invalid. Check program, city, province, and country on the application.";
    }

    private static string? TryExtractConstraintName(string message)
    {
        var match = Regex.Match(message, @"constraint\s+""([^""]+)""", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? TryExtractReferencedTable(string message)
    {
        var references = Regex.Match(message, @"references\s+table\s+""dbo\.([^""]+)""", RegexOptions.IgnoreCase);
        if (references.Success)
        {
            return references.Groups[1].Value;
        }

        return null;
    }
}
