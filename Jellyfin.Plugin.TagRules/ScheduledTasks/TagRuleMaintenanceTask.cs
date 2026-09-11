using System.Collections.ObjectModel;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.TagRules.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TagRules.ScheduledTasks;

/// <summary>
/// Scheduled task that applies every enabled <see cref="TagRule"/> to the library.
/// </summary>
public class TagRuleMaintenanceTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<TagRuleMaintenanceTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagRuleMaintenanceTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TagRuleMaintenanceTask}"/> interface.</param>
    public TagRuleMaintenanceTask(ILibraryManager libraryManager, ILogger<TagRuleMaintenanceTask> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Apply collection tag rules";

    /// <inheritdoc />
    public string Key => "TagRulesMaintenance";

    /// <inheritdoc />
    public string Description =>
        "For each configured tag rule, adds the rule's exclude tag to items missing every marker " +
        "tag, and removes it again from items that now carry a marker tag.";

    /// <inheritdoc />
    public string Category => "Library";

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.DailyTrigger,
            TimeOfDayTicks = TimeSpan.FromHours(3).Ticks,
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        var rules = (Plugin.Instance?.Configuration.Rules ?? new Collection<TagRule>())
            .Where(rule => rule.Enabled
                && !string.IsNullOrWhiteSpace(rule.ExcludeTag)
                && rule.MarkerTags.Count > 0
                && rule.ItemTypes.Count > 0)
            .ToList();

        if (rules.Count == 0)
        {
            _logger.LogInformation("No enabled tag rules are configured; nothing to do.");
            progress.Report(100);
            return;
        }

        for (var index = 0; index < rules.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ApplyRuleAsync(rules[index], cancellationToken).ConfigureAwait(false);
            progress.Report(100.0 * (index + 1) / rules.Count);
        }
    }

    private async Task ApplyRuleAsync(TagRule rule, CancellationToken cancellationToken)
    {
        var itemTypes = rule.ItemTypes
            .Select(type => ItemTypeOptions.Kinds.TryGetValue(type, out var kind) ? (BaseItemKind?)kind : null)
            .Where(kind => kind.HasValue)
            .Select(kind => kind!.Value)
            .Distinct()
            .ToArray();

        if (itemTypes.Length == 0)
        {
            _logger.LogWarning("Tag rule '{ExcludeTag}' has no recognized item types configured; skipping.", rule.ExcludeTag);
            return;
        }

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = itemTypes,
            Recursive = true,
        };

        var items = _libraryManager.GetItemList(query);
        var added = 0;
        var removed = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentTags = item.Tags ?? Array.Empty<string>();
            var action = TagRuleEvaluator.Evaluate(currentTags, rule);
            if (action == TagRuleAction.NoChange)
            {
                continue;
            }

            item.Tags = TagRuleEvaluator.ApplyAction(currentTags, action, rule.ExcludeTag);
            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

            if (action == TagRuleAction.AddExcludeTag)
            {
                added++;
            }
            else
            {
                removed++;
            }
        }

        _logger.LogInformation(
            "Tag rule '{ExcludeTag}': scanned {Total} item(s), added to {Added}, removed from {Removed}.",
            rule.ExcludeTag,
            items.Count,
            added,
            removed);
    }
}
