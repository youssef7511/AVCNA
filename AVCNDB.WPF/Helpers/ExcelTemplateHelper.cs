using System.Reflection;

namespace AVCNDB.WPF.Helpers;

public static class ExcelTemplateHelper
{
    public static IReadOnlyList<string> GetStrictColumns<T>() where T : class
    {
        return typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => p.CanWrite)
            .Select(p => p.Name)
            .ToList();
    }

    public static List<string> GetUnexpectedColumns(IEnumerable<string> foundColumns, IEnumerable<string> expectedColumns)
    {
        var expected = expectedColumns.ToList();
        return foundColumns
            .Where(f => !expected.Any(e => e.Equals(f, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public static List<string> GetDuplicateColumns(IEnumerable<string> foundColumns)
    {
        return foundColumns
            .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
    }
}
