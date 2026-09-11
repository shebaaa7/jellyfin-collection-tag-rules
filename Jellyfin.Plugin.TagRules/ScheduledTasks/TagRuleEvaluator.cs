using Jellyfin.Plugin.TagRules.Configuration;

namespace Jellyfin.Plugin.TagRules.ScheduledTasks;

/// <summary>
/// The change a <see cref="TagRule"/> requires for a given item's current tags.
/// </summary>
public enum TagRuleAction
{
    /// <summary>
    /// No tag change is required.
    /// </summary>
    NoChange,

    /// <summary>
    /// The rule's exclude tag should be added.
    /// </summary>
    AddExcludeTag,

    /// <summary>
    /// The rule's exclude tag should be removed.
    /// </summary>
    RemoveExcludeTag,
}

/// <summary>
/// Pure, testable tag-rule matching logic, kept independent of any Jellyfin server types so it can
/// be unit tested without a running server.
/// </summary>
public static class TagRuleEvaluator
{
    /// <summary>
    /// Determines what, if anything, should change about an item's tags for a given rule.
    /// </summary>
    /// <param name="currentTags">The item's current tags.</param>
    /// <param name="rule">The rule to evaluate.</param>
    /// <returns>The required action.</returns>
    public static TagRuleAction Evaluate(IEnumerable<string> currentTags, TagRule rule)
    {
        ArgumentNullException.ThrowIfNull(currentTags);
        ArgumentNullException.ThrowIfNull(rule);

        var tags = currentTags as ICollection<string> ?? currentTags.ToList();
        var hasMarkerTag = rule.MarkerTags.Any(marker => tags.Contains(marker, StringComparer.OrdinalIgnoreCase));
        var hasExcludeTag = tags.Contains(rule.ExcludeTag, StringComparer.OrdinalIgnoreCase);

        if (hasMarkerTag)
        {
            return hasExcludeTag ? TagRuleAction.RemoveExcludeTag : TagRuleAction.NoChange;
        }

        return hasExcludeTag ? TagRuleAction.NoChange : TagRuleAction.AddExcludeTag;
    }

    /// <summary>
    /// Produces the tag set that results from applying <paramref name="action"/>.
    /// </summary>
    /// <param name="currentTags">The item's current tags.</param>
    /// <param name="action">The action to apply.</param>
    /// <param name="excludeTag">The rule's exclude tag.</param>
    /// <returns>The resulting tag array.</returns>
    public static string[] ApplyAction(IEnumerable<string> currentTags, TagRuleAction action, string excludeTag)
    {
        ArgumentNullException.ThrowIfNull(currentTags);

        var tags = currentTags.ToList();

        switch (action)
        {
            case TagRuleAction.AddExcludeTag:
                tags.Add(excludeTag);
                break;
            case TagRuleAction.RemoveExcludeTag:
                tags.RemoveAll(tag => string.Equals(tag, excludeTag, StringComparison.OrdinalIgnoreCase));
                break;
            case TagRuleAction.NoChange:
            default:
                break;
        }

        return tags.ToArray();
    }
}
