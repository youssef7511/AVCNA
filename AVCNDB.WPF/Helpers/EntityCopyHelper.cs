using System.Reflection;

namespace AVCNDB.WPF.Helpers;

public static class EntityCopyHelper
{
    public static void CopyWritableProperties<T>(T source, T destination, params string[] excludedPropertyNames)
        where T : class
    {
        var excluded = new HashSet<string>(excludedPropertyNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !excluded.Contains(p.Name));

        foreach (var property in properties)
        {
            var value = property.GetValue(source);
            property.SetValue(destination, value);
        }
    }
}
