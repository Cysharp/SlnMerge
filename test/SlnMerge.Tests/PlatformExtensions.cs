using System.Text.RegularExpressions;

namespace SlnMerge.Tests;

public static class PlatformExtensions
{
    public static string ReplacePathSeparators(this string value)
    {
        if (Path.DirectorySeparatorChar == '\\') return value; // on Windows: no-op

        return value.Replace('\\', '/');
    }

    public static string ToCurrentPlatformPathForm(this string path)
    {
        if (Path.DirectorySeparatorChar == '\\') return path; // on Windows: no-op

        return Regex.Replace(path, "^([a-zA-Z]):", "/mnt/$1").Replace('\\', '/');
    }
}
