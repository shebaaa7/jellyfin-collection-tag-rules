using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.TagRules.Configuration;

/// <summary>
/// One admin-defined rule: items lacking every marker tag get <see cref="ExcludeTag"/> applied;
/// items carrying a marker tag get it removed again if present.
/// </summary>
public class TagRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TagRule"/> class.
    /// </summary>
    public TagRule()
    {
        MarkerTags = new Collection<string>();
        ItemTypes = new Collection<string>();
    }

    /// <summary>
    /// Gets or sets a value indicating whether this rule is evaluated by the scheduled task.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the tag applied to items that carry none of <see cref="MarkerTags"/>.
    /// </summary>
    public string ExcludeTag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tags that mark an item as belonging to the group. An item with any one
    /// of these is left alone (or has <see cref="ExcludeTag"/> removed if present).
    /// </summary>
    [SuppressMessage(
        "Usage",
        "CA2227:Collection properties should be read only",
        Justification = "Jellyfin's plugin configuration serializers require a public setter.")]
    public Collection<string> MarkerTags { get; set; }

    /// <summary>
    /// Gets or sets the item kinds this rule scans, as <see cref="ItemTypeOptions"/> keys.
    /// </summary>
    [SuppressMessage(
        "Usage",
        "CA2227:Collection properties should be read only",
        Justification = "Jellyfin's plugin configuration serializers require a public setter.")]
    public Collection<string> ItemTypes { get; set; }
}
