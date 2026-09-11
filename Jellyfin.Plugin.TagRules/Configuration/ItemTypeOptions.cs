using System.Collections.ObjectModel;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.TagRules.Configuration;

/// <summary>
/// The curated, admin-facing set of item kinds a <see cref="TagRule"/> can scan. Kept to a short,
/// clearly-labeled list rather than exposing every <see cref="BaseItemKind"/> value.
/// </summary>
public static class ItemTypeOptions
{
    /// <summary>
    /// Gets the supported item type keys mapped to the underlying <see cref="BaseItemKind"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, BaseItemKind> Kinds { get; } = new ReadOnlyDictionary<string, BaseItemKind>(
        new Dictionary<string, BaseItemKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["BoxSet"] = BaseItemKind.BoxSet,
            ["Series"] = BaseItemKind.Series,
            ["Movie"] = BaseItemKind.Movie,
            ["MusicAlbum"] = BaseItemKind.MusicAlbum,
            ["Playlist"] = BaseItemKind.Playlist,
        });
}
