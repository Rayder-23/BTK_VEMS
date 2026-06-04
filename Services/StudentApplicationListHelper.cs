namespace VEMS.Services;

public static class StudentApplicationListHelper
{
    public static IReadOnlyList<string> WithSelectedValue(
        IReadOnlyList<string> options,
        string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected))
        {
            return options;
        }

        var value = selected.Trim();
        if (options.Any(o => string.Equals(o, value, StringComparison.OrdinalIgnoreCase)))
        {
            return options;
        }

        var list = options.ToList();
        list.Insert(0, value);
        return list;
    }
}
