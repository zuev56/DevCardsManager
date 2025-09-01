using System.Text.Json;

namespace DevCardsManager.Extensions;

public static class ObjectExtensions
{
    public static TObject? Duplicate<TObject>(this TObject? source)
        where TObject : class
    {
        if (source == null)
            return null;

        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<TObject>(json);
    }
}