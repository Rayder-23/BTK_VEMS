using Microsoft.Data.SqlClient;

namespace VEMS.Areas.AdminPortal.Services.Fee;

internal static class FeeSql
{
    public static int ToInt32(SqlDataReader reader, string column) =>
        Convert.ToInt32(reader.GetValue(reader.GetOrdinal(column)));

    public static int ToInt32(SqlDataReader reader, int ordinal) =>
        Convert.ToInt32(reader.GetValue(ordinal));

    public static short ToInt16(SqlDataReader reader, string column) =>
        Convert.ToInt16(reader.GetValue(reader.GetOrdinal(column)));

    public static decimal ToDecimal(SqlDataReader reader, string column) =>
        Convert.ToDecimal(reader.GetValue(reader.GetOrdinal(column)));

    public static bool ToBoolean(SqlDataReader reader, string column) =>
        Convert.ToBoolean(reader.GetValue(reader.GetOrdinal(column)));

    public static DateTime? ToNullableDateTime(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    public static DateOnly ToDateOnly(SqlDataReader reader, string column) =>
        DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal(column)));

    public static DateOnly? ToNullableDateOnly(SqlDataReader reader, string column)
    {
        var dt = ToNullableDateTime(reader, column);
        return dt.HasValue ? DateOnly.FromDateTime(dt.Value) : null;
    }
}
