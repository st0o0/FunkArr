using Xunit;

namespace FunkArr.MatchMagic.Tests;

public sealed class FilterTests
{
    private static readonly MediaItem _item = TestData.CreateItem(
        topic: "Tatort",
        title: "Tatort: Die goldene Zeit",
        channel: "ARD",
        durationMinutes: 90,
        timestamp: 1719331200);

    [Fact]
    public void Duration_greaterThan_passes() =>
        Assert.True(new Filter("duration", FilterOp.GreaterThan, "60").Evaluate(_item));

    [Fact]
    public void Duration_greaterThan_fails() =>
        Assert.False(new Filter("duration", FilterOp.GreaterThan, "120").Evaluate(_item));

    [Fact]
    public void Duration_lessThan_passes() =>
        Assert.True(new Filter("duration", FilterOp.LessThan, "120").Evaluate(_item));

    [Fact]
    public void Duration_lessThan_fails() =>
        Assert.False(new Filter("duration", FilterOp.LessThan, "60").Evaluate(_item));

    [Fact]
    public void Title_contains_passes() =>
        Assert.True(new Filter("title", FilterOp.Contains, "goldene").Evaluate(_item));

    [Fact]
    public void Title_contains_case_insensitive() =>
        Assert.True(new Filter("title", FilterOp.Contains, "GOLDENE").Evaluate(_item));

    [Fact]
    public void Title_contains_fails() =>
        Assert.False(new Filter("title", FilterOp.Contains, "Schwarzer").Evaluate(_item));

    [Fact]
    public void Channel_eq_passes() =>
        Assert.True(new Filter("channel", FilterOp.Eq, "ARD").Evaluate(_item));

    [Fact]
    public void Channel_eq_case_insensitive() =>
        Assert.True(new Filter("channel", FilterOp.Eq, "ard").Evaluate(_item));

    [Fact]
    public void Channel_eq_fails() =>
        Assert.False(new Filter("channel", FilterOp.Eq, "ZDF").Evaluate(_item));

    [Fact]
    public void Regex_passes() =>
        Assert.True(new Filter("title", FilterOp.Regex, "^Tatort").Evaluate(_item));

    [Fact]
    public void Regex_fails() =>
        Assert.False(new Filter("title", FilterOp.Regex, "^heute").Evaluate(_item));

    [Fact]
    public void NotContains_passes() =>
        Assert.True(new Filter("title", FilterOp.NotContains, "Trailer").Evaluate(_item));

    [Fact]
    public void NotContains_fails() =>
        Assert.False(new Filter("title", FilterOp.NotContains, "goldene").Evaluate(_item));

    [Fact]
    public void Timestamp_greaterThan() =>
        Assert.True(new Filter("timestamp", FilterOp.GreaterThan, "1719244800").Evaluate(_item));

    [Fact]
    public void Unknown_field_returns_false() =>
        Assert.False(new Filter("unknown", FilterOp.Eq, "x").Evaluate(_item));

    [Fact]
    public void Regex_timeout_returns_false()
    {
        var evilPattern = @"^(a+)+$";
        var item = TestData.CreateItem(title: new string('a', 30) + "!");
        Assert.False(new Filter("title", FilterOp.Regex, evilPattern).Evaluate(item));
    }
}

public sealed class FilterGroupTests
{
    private static readonly MediaItem _item = TestData.CreateItem(
        channel: "ARD", durationMinutes: 90, title: "Tatort: Die goldene Zeit");

    [Fact]
    public void Empty_group_passes_everything() =>
        Assert.True(new FilterGroup().Evaluate(_item));

    [Fact]
    public void All_group_all_pass()
    {
        var group = new FilterGroup(All:
        [
            new FilterNode.Leaf(new Filter("duration", FilterOp.GreaterThan, "30")),
            new FilterNode.Leaf(new Filter("duration", FilterOp.LessThan, "120")),
        ]);

        Assert.True(group.Evaluate(_item));
    }

    [Fact]
    public void All_group_one_fails()
    {
        var group = new FilterGroup(All:
        [
            new FilterNode.Leaf(new Filter("duration", FilterOp.GreaterThan, "30")),
            new FilterNode.Leaf(new Filter("duration", FilterOp.LessThan, "60")),
        ]);

        Assert.False(group.Evaluate(_item));
    }

    [Fact]
    public void Any_group_one_passes()
    {
        var group = new FilterGroup(Any:
        [
            new FilterNode.Leaf(new Filter("channel", FilterOp.Eq, "ZDF")),
            new FilterNode.Leaf(new Filter("channel", FilterOp.Eq, "ARD")),
        ]);

        Assert.True(group.Evaluate(_item));
    }

    [Fact]
    public void Any_group_none_pass()
    {
        var group = new FilterGroup(Any:
        [
            new FilterNode.Leaf(new Filter("channel", FilterOp.Eq, "ZDF")),
            new FilterNode.Leaf(new Filter("channel", FilterOp.Eq, "BR")),
        ]);

        Assert.False(group.Evaluate(_item));
    }

    [Fact]
    public void Not_group_blocks_matching()
    {
        var group = new FilterGroup(Not:
        [
            new FilterNode.Leaf(new Filter("title", FilterOp.Contains, "Audiodeskription")),
        ]);

        Assert.True(group.Evaluate(_item));

        var adItem = TestData.CreateItem(title: "Tatort (Audiodeskription)");
        Assert.False(group.Evaluate(adItem));
    }

    [Fact]
    public void Combined_all_and_not()
    {
        var group = new FilterGroup(
            All: [new FilterNode.Leaf(new Filter("duration", FilterOp.GreaterThan, "30"))],
            Not: [new FilterNode.Leaf(new Filter("title", FilterOp.Contains, "Trailer"))]);

        Assert.True(group.Evaluate(_item));

        var trailerItem = TestData.CreateItem(title: "Tatort: Trailer", durationMinutes: 90);
        Assert.False(group.Evaluate(trailerItem));
    }

    [Fact]
    public void Nested_group_inside_all()
    {
        var group = new FilterGroup(All:
        [
            new FilterNode.Leaf(new Filter("duration", FilterOp.GreaterThan, "30")),
            new FilterNode.Group(new FilterGroup(Any:
            [
                new FilterNode.Leaf(new Filter("channel", FilterOp.Eq, "ARD")),
                new FilterNode.Leaf(new Filter("channel", FilterOp.Eq, "ZDF")),
            ])),
        ]);

        Assert.True(group.Evaluate(_item));

        var brItem = TestData.CreateItem(channel: "BR", durationMinutes: 90);
        Assert.False(group.Evaluate(brItem));
    }
}
