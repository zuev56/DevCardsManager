using System.IO;

namespace DevCardsManager.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Меняет направление символа Slash
    /// </summary>
    /// <remarks>
    /// Т.к. приложение кроссплатформенное, чтобы не путаться при переносе конфигурации на другую ОС,
    /// решил в конфиге пути прописывать с forward slash, а в Windows это мешает при использовании Path.Combine
    /// </remarks>
    internal static string ToOsSpecificDirectorySeparatorChar(this string path)
        => path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

    public static bool IsValidDirectoryPath(this string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            var fullPath = Path.GetFullPath(path);

            return !string.IsNullOrEmpty(Path.GetDirectoryName(fullPath));
        }
        catch
        {
            return false;
        }
    }
}