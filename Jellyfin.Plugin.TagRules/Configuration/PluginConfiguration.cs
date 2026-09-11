using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TagRules.Configuration;

/// <summary>
/// Plugin configuration: the list of tag rules the scheduled task evaluates.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Rules = new Collection<TagRule>();
    }

    /// <summary>
    /// Gets or sets the configured tag rules.
    /// </summary>
    [SuppressMessage(
        "Usage",
        "CA2227:Collection properties should be read only",
        Justification = "Jellyfin's plugin configuration serializers require a public setter.")]
    public Collection<TagRule> Rules { get; set; }
}
