using System.Text.RegularExpressions;

namespace SlnMerge.Tests;

public static class PlatformExtensions
{
    public static string ReplacePathSeparators(this string value)
    {
        if (Path.DirectorySeparatorChar == '\\') return value.Replace('/', '\\'); // on Windows

        return value.Replace('\\', '/');
    }

    public static string ToCurrentPlatformPathForm(this string path)
    {
        if (Path.DirectorySeparatorChar == '\\') return path.Replace('/', '\\'); // on Windows

        return Regex.Replace(path, "^([a-zA-Z]):", "/mnt/$1").Replace('\\', '/');
    }
}
