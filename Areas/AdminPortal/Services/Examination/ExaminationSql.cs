using Microsoft.Data.SqlClient;

namespace VEMS.Areas.AdminPortal.Services.Examination;

internal static class ExaminationSql
{
    public static int ToInt32(SqlDataReader reader, string column) =>
        Convert.ToInt32(reader.GetValue(reader.GetOrdinal(column)));

    public static short ToInt16(SqlDataReader reader, string column) =>
        Convert.ToInt16(reader.GetValue(reader.GetOrdinal(column)));

    public static decimal ToDecimal(SqlDataReader reader, string column) =>
        Convert.ToDecimal(reader.GetValue(reader.GetOrdinal(column)));

    public static bool ToBoolean(SqlDataReader reader, string column) =>
        Convert.ToBoolean(reader.GetValue(reader.GetOrdinal(column)));

    public static string Cell(SqlDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? "" : Convert.ToString(r[col]) ?? "";

    public static string Cell(SqlDataReader r, int ordinal) =>
        r.IsDBNull(ordinal) ? "" : Convert.ToString(r.GetValue(ordinal)) ?? "";

    public static string FmtDecimal(SqlDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? "" : Convert.ToDecimal(r[col]).ToString("0.##");

    public static string FmtDate(SqlDataReader r, string col)
    {
        if (r.IsDBNull(r.GetOrdinal(col))) return "";
        var v = r.GetFieldType(r.GetOrdinal(col))?.Name;
        if (v == "DateTime")
            return DateTime.Parse(r[col]!.ToString()!).ToString("yyyy-MM-dd");
        return r[col]?.ToString() ?? "";
    }

    public static string FmtDateTime(SqlDataReader r, string col) =>
        r.IsDBNull(r.GetOrdinal(col)) ? "" : ((DateTime)r[col]).ToString("u");

    public static string FmtTime(SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return "";
        var o = r.GetValue(ord);
        return o switch
        {
            TimeSpan ts => ts.ToString(@"hh\:mm"),
            DateTime dt => dt.TimeOfDay.ToString(@"hh\:mm"),
            _ => o.ToString() ?? ""
        };
    }
}
