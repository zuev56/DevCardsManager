using System.IO;

namespace DevCardsManager;

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
}