using Jellyfin.Plugin.TagRules.Configuration;
using Jellyfin.Plugin.TagRules.ScheduledTasks;
using Xunit;

namespace Jellyfin.Plugin.TagRules.Tests;

public sealed class TagRuleEvaluatorTests
{
    private static TagRule CreateRule(string excludeTag, params string[] markerTags)
    {
        var rule = new TagRule { ExcludeTag = excludeTag };
        foreach (var tag in markerTags)
        {
            rule.MarkerTags.Add(tag);
        }

        return rule;
    }

    [Fact]
    public void Evaluate_AddsExcludeTag_WhenNoMarkerTagPresent()
    {
        var rule = CreateRule("Not Kids Content", "Kids", "Family");

        var action = TagRuleEvaluator.Evaluate(["Action"], rule);

        Assert.Equal(TagRuleAction.AddExcludeTag, action);
    }

    [Fact]
    public void Evaluate_NoChange_WhenMarkerTagAlreadyPresentAndNoExcludeTag()
    {
        var rule = CreateRule("Not Kids Content", "Kids", "Family");

        var action = TagRuleEvaluator.Evaluate(["Kids"], rule);

        Assert.Equal(TagRuleAction.NoChange, action);
    }

    [Fact]
    public void Evaluate_NoChange_WhenExcludeTagAlreadyPresentAndNoMarker()
    {
        var rule = CreateRule("Not Kids Content", "Kids", "Family");

        var action = TagRuleEvaluator.Evaluate(["Not Kids Content"], rule);

        Assert.Equal(TagRuleAction.NoChange, action);
    }

    [Fact]
    public void Evaluate_RemovesExcludeTag_WhenMarkerTagLaterAdded()
    {
        var rule = CreateRule("Not Kids Content", "Kids", "Family");

        var action = TagRuleEvaluator.Evaluate(["Not Kids Content", "Family"], rule);

        Assert.Equal(TagRuleAction.RemoveExcludeTag, action);
    }

    [Fact]
    public void Evaluate_MarkerAndExcludeMatching_IsCaseInsensitive()
    {
        var rule = CreateRule("Not Kids Content", "kids");

        var action = TagRuleEvaluator.Evaluate(["KIDS"], rule);

        Assert.Equal(TagRuleAction.NoChange, action);
    }

    [Fact]
    public void ApplyAction_AddExcludeTag_AppendsTag()
    {
        var tags = TagRuleEvaluator.ApplyAction(["Action"], TagRuleAction.AddExcludeTag, "Not Kids Content");

        Assert.Equal(["Action", "Not Kids Content"], tags);
    }

    [Fact]
    public void ApplyAction_RemoveExcludeTag_RemovesTagCaseInsensitively()
    {
        var tags = TagRuleEvaluator.ApplyAction(["not kids content", "Family"], TagRuleAction.RemoveExcludeTag, "Not Kids Content");

        Assert.Equal(["Family"], tags);
    }

    [Fact]
    public void ApplyAction_NoChange_ReturnsTagsUnmodified()
    {
        var tags = TagRuleEvaluator.ApplyAction(["Action"], TagRuleAction.NoChange, "Not Kids Content");

        Assert.Equal(["Action"], tags);
    }
}
