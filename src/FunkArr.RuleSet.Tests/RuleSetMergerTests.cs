using FunkArr.Messages.MetadataResolver;
using FunkArr.Messages.Scoring;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetMergerTests
{
    private const string _communityJson = """
        {
          "topic": "Test Show",
          "aliases": ["Test Alias"],
          "confidence": 0.9,
          "rules": [
            {
              "id": "airdate",
              "priority": 0,
              "strategy": "itemTitleEqualsAirdate",
              "filters": {
                "all": [
                  { "field": "duration", "op": "greaterThan", "value": "30" }
                ]
              }
            },
            {
              "id": "season-ep",
              "priority": 1,
              "strategy": "seasonAndEpisodeNumber",
              "seasonRegex": "S(\\d+)",
              "episodeRegex": "E(\\d+)",
              "filters": {
                "all": [
                  { "field": "duration", "op": "greaterThan", "value": "20" }
                ]
              }
            }
          ]
        }
        """;

    [Fact]
    public void Build_community_only_produces_valid_config()
    {
        var config = RuleSetMerger.Build("test-show", _communityJson, null);

        Assert.NotNull(config);
        Assert.Equal("test-show", config.RuleSetId);
        Assert.Equal(0.9f, config.DefaultConfidence);
        Assert.Equal(2, config.Rules.Length);
    }

    [Fact]
    public void Build_local_only_produces_valid_config()
    {
        var localJson = """
            {
              "topic": "Local Show",
              "confidence": 0.8,
              "rules": [
                {
                  "id": "title-rule",
                  "priority": 0,
                  "strategy": "itemTitleExact",
                  "titleRules": [
                    { "type": "regex", "field": "title", "pattern": "(.*)" }
                  ]
                }
              ]
            }
            """;

        var config = RuleSetMerger.Build("local-show", null, localJson);

        Assert.NotNull(config);
        Assert.Equal("local-show", config.RuleSetId);
        Assert.Single(config.Rules);
    }

    [Fact]
    public void Build_merge_local_overrides_rule_by_id()
    {
        var localJson = """
            {
              "topic": "Test Show",
              "rules": [
                {
                  "id": "airdate",
                  "priority": 0,
                  "confidence": 0.5,
                  "strategy": "itemTitleEqualsAirdate"
                }
              ]
            }
            """;

        var config = RuleSetMerger.Build("test-show", _communityJson, localJson);

        Assert.NotNull(config);
        Assert.Equal(2, config.Rules.Length);
        var airdateRule = config.Rules.First(r => r.Id == "airdate");
        Assert.Equal(0.5f, airdateRule.Confidence);
    }

    [Fact]
    public void Build_standalone_local_ignores_community()
    {
        var localJson = """
            {
              "topic": "Standalone Show",
              "standalone": true,
              "confidence": 0.7,
              "rules": [
                {
                  "id": "local-only",
                  "priority": 0,
                  "strategy": "itemTitleEqualsAirdate"
                }
              ]
            }
            """;

        var config = RuleSetMerger.Build("test-show", _communityJson, localJson);

        Assert.NotNull(config);
        Assert.Single(config.Rules);
        Assert.Equal("local-only", config.Rules[0].Id);
        Assert.Equal(0.7f, config.DefaultConfidence);
    }

    [Fact]
    public void Build_disable_excludes_community_rules()
    {
        var localJson = """
            {
              "topic": "Test Show",
              "disable": ["airdate"],
              "rules": []
            }
            """;

        var config = RuleSetMerger.Build("test-show", _communityJson, localJson);

        Assert.NotNull(config);
        Assert.Single(config.Rules);
        Assert.Equal("season-ep", config.Rules[0].Id);
    }

    [Fact]
    public void Build_maps_seasonAndEpisodeNumber_to_RegexCapture()
    {
        var config = RuleSetMerger.Build("test", _communityJson, null);

        Assert.NotNull(config);
        var rule = config.Rules.First(r => r.Id == "season-ep");
        Assert.Equal(IdentificationStrategy.RegexCapture, rule.Identification.Strategy);
        Assert.Equal("S(\\d+)", rule.Identification.SeasonPattern);
        Assert.Equal("E(\\d+)", rule.Identification.EpisodePattern);
    }

    [Fact]
    public void Build_maps_byAbsoluteEpisodeNumber_to_RegexCapture_without_season()
    {
        var json = """
            {
              "topic": "Test",
              "rules": [
                {
                  "id": "abs",
                  "priority": 0,
                  "strategy": "byAbsoluteEpisodeNumber",
                  "episodeRegex": "\\((\\d+)\\)"
                }
              ]
            }
            """;

        var config = RuleSetMerger.Build("test", json, null);

        Assert.NotNull(config);
        Assert.Single(config.Rules);
        Assert.Equal(IdentificationStrategy.RegexCapture, config.Rules[0].Identification.Strategy);
        Assert.Null(config.Rules[0].Identification.SeasonPattern);
        Assert.NotNull(config.Rules[0].Identification.EpisodePattern);
    }

    [Fact]
    public void Build_maps_itemTitleExact_to_TitleConstruction_exact()
    {
        var json = """
            {
              "topic": "Test",
              "rules": [
                {
                  "id": "exact",
                  "priority": 0,
                  "strategy": "itemTitleExact",
                  "titleRules": [
                    { "type": "regex", "field": "title", "pattern": "(.*)" }
                  ]
                }
              ]
            }
            """;

        var config = RuleSetMerger.Build("test", json, null);

        Assert.NotNull(config);
        var rule = config.Rules[0];
        Assert.Equal(IdentificationStrategy.TitleConstruction, rule.Identification.Strategy);
        Assert.Equal(TitleMatchMode.Exact, rule.Identification.MatchMode);
        Assert.NotNull(rule.Identification.TitleParts);
    }

    [Fact]
    public void Build_maps_itemTitleIncludes_to_TitleConstruction_contains()
    {
        var json = """
            {
              "topic": "Test",
              "rules": [
                {
                  "id": "includes",
                  "priority": 0,
                  "strategy": "itemTitleIncludes",
                  "titleRules": [
                    { "type": "regex", "field": "title", "pattern": "(.*)" }
                  ]
                }
              ]
            }
            """;

        var config = RuleSetMerger.Build("test", json, null);

        Assert.NotNull(config);
        Assert.Equal(TitleMatchMode.Contains, config.Rules[0].Identification.MatchMode);
    }

    [Fact]
    public void Build_maps_itemTitleEqualsAirdate_to_AirdateExtraction()
    {
        var config = RuleSetMerger.Build("test", _communityJson, null);

        Assert.NotNull(config);
        var rule = config.Rules.First(r => r.Id == "airdate");
        Assert.Equal(IdentificationStrategy.AirdateExtraction, rule.Identification.Strategy);
    }

    [Fact]
    public void Build_transforms_filter_field_and_op_to_enums()
    {
        var config = RuleSetMerger.Build("test", _communityJson, null);

        Assert.NotNull(config);
        var rule = config.Rules.First(r => r.Id == "airdate");
        Assert.NotNull(rule.Filters);
        Assert.NotNull(rule.Filters.All);

        var condition = Assert.IsType<FilterNode.ConditionNode>(rule.Filters.All[0]);
        Assert.Equal(FilterField.Duration, condition.Condition.Field);
        Assert.Equal(FilterOp.GreaterThan, condition.Condition.Op);
        Assert.Equal("30", condition.Condition.Value);
    }

    [Fact]
    public void Build_transforms_title_rules_to_enums()
    {
        var json = """
            {
              "topic": "Test",
              "rules": [
                {
                  "id": "title",
                  "priority": 0,
                  "strategy": "itemTitleExact",
                  "titleRules": [
                    { "type": "static", "value": "Folge " },
                    { "type": "regex", "field": "title", "pattern": "\\((\\d+)\\)" }
                  ]
                }
              ]
            }
            """;

        var config = RuleSetMerger.Build("test", json, null);

        Assert.NotNull(config);
        var parts = config.Rules[0].Identification.TitleParts;
        Assert.NotNull(parts);
        Assert.Equal(2, parts.Length);
        Assert.Equal(TitlePartType.Static, parts[0].Type);
        Assert.Equal("Folge ", parts[0].Value);
        Assert.Equal(TitlePartType.Regex, parts[1].Type);
        Assert.Equal(FilterField.Title, parts[1].Field);
    }

    [Fact]
    public void Build_skips_rule_with_invalid_strategy()
    {
        var json = """
            {
              "topic": "Test",
              "rules": [
                {
                  "id": "valid",
                  "priority": 0,
                  "strategy": "itemTitleEqualsAirdate"
                },
                {
                  "id": "invalid",
                  "priority": 1,
                  "strategy": "nonExistentStrategy"
                }
              ]
            }
            """;

        var config = RuleSetMerger.Build("test", json, null);

        Assert.NotNull(config);
        Assert.Single(config.Rules);
        Assert.Equal("valid", config.Rules[0].Id);
    }

    [Fact]
    public void ExtractIdentity_returns_topic_and_aliases()
    {
        var identity = RuleSetMerger.ExtractIdentity(_communityJson, null);

        Assert.NotNull(identity);
        Assert.Equal("Test Show", identity.Value.Topic);
        Assert.Single(identity.Value.Aliases);
        Assert.Equal("Test Alias", identity.Value.Aliases[0]);
    }

    [Fact]
    public void ExtractIdentity_returns_null_for_null_inputs()
    {
        var identity = RuleSetMerger.ExtractIdentity(null, null);

        Assert.Null(identity);
    }

    [Fact]
    public void Build_returns_null_for_null_inputs()
    {
        var config = RuleSetMerger.Build("test", null, null);

        Assert.Null(config);
    }

    [Fact]
    public void ExtractIdentity_returns_media_ids_from_community()
    {
        var json = """
            {
              "topic": "Tatort",
              "aliases": [],
              "media": {
                "tvdbId": 83214,
                "imdbId": "tt0806910",
                "tmdbId": 2116
              },
              "rules": []
            }
            """;

        var identity = RuleSetMerger.ExtractIdentity(json, null);

        Assert.NotNull(identity);
        Assert.Equal(83214, identity.Value.TvdbId);
        Assert.Equal("tt0806910", identity.Value.ImdbId);
        Assert.Equal(2116, identity.Value.TmdbId);
    }

    [Fact]
    public void ExtractIdentity_local_overrides_community_media_ids()
    {
        var communityJson = """
            {
              "topic": "Tatort",
              "media": { "tvdbId": 83214, "imdbId": "tt0806910" },
              "rules": []
            }
            """;
        var localJson = """
            {
              "topic": "Tatort",
              "media": { "tvdbId": 99999 },
              "rules": []
            }
            """;

        var identity = RuleSetMerger.ExtractIdentity(communityJson, localJson);

        Assert.NotNull(identity);
        Assert.Equal(99999, identity.Value.TvdbId);
        Assert.Equal("tt0806910", identity.Value.ImdbId);
    }

    [Fact]
    public void ExtractIdentity_standalone_local_uses_local_media()
    {
        var communityJson = """
            {
              "topic": "Tatort",
              "media": { "tvdbId": 83214 },
              "rules": []
            }
            """;
        var localJson = """
            {
              "topic": "Custom",
              "standalone": true,
              "media": { "imdbId": "tt1234567" },
              "rules": []
            }
            """;

        var identity = RuleSetMerger.ExtractIdentity(communityJson, localJson);

        Assert.NotNull(identity);
        Assert.Null(identity.Value.TvdbId);
        Assert.Equal("tt1234567", identity.Value.ImdbId);
    }

    [Fact]
    public void ExtractIdentity_no_media_block_returns_null_ids()
    {
        var identity = RuleSetMerger.ExtractIdentity(_communityJson, null);

        Assert.NotNull(identity);
        Assert.Null(identity.Value.TvdbId);
        Assert.Null(identity.Value.ImdbId);
        Assert.Null(identity.Value.TmdbId);
    }

    [Fact]
    public void Build_with_resolution_config_produces_resolution()
    {
        var json = """
            {
              "topic": "Tatort",
              "confidence": 1.0,
              "resolution": {
                "strategy": "fuzzy",
                "threshold": 0.7,
                "airdateTolerance": 7
              },
              "rules": [
                {
                  "id": "title",
                  "priority": 0,
                  "strategy": "itemTitleEqualsAirdate"
                }
              ]
            }
            """;

        var config = RuleSetMerger.Build("tatort", json, null);

        Assert.NotNull(config);
        Assert.NotNull(config.Resolution);
        Assert.Equal("fuzzy", config.Resolution.Strategy);
        Assert.Equal(0.7f, config.Resolution.Threshold);
        Assert.Equal(7, config.Resolution.AirdateTolerance);
    }

    [Fact]
    public void Build_without_resolution_config_has_null_resolution()
    {
        var config = RuleSetMerger.Build("test", _communityJson, null);

        Assert.NotNull(config);
        Assert.Null(config.Resolution);
    }

    [Fact]
    public void Build_merge_local_resolution_overrides_community()
    {
        var communityJson = """
            {
              "topic": "Tatort",
              "resolution": {
                "strategy": "fuzzy",
                "threshold": 0.7,
                "airdateTolerance": 7
              },
              "rules": [
                { "id": "r1", "priority": 0, "strategy": "itemTitleEqualsAirdate" }
              ]
            }
            """;
        var localJson = """
            {
              "topic": "Tatort",
              "resolution": {
                "strategy": "strict",
                "threshold": 0.95
              },
              "rules": []
            }
            """;

        var config = RuleSetMerger.Build("tatort", communityJson, localJson);

        Assert.NotNull(config);
        Assert.NotNull(config.Resolution);
        Assert.Equal("strict", config.Resolution.Strategy);
        Assert.Equal(0.95f, config.Resolution.Threshold);
        Assert.Equal(7, config.Resolution.AirdateTolerance);
    }

    [Fact]
    public void Build_merge_community_only_resolution_preserved()
    {
        var communityJson = """
            {
              "topic": "Tatort",
              "resolution": {
                "strategy": "fuzzy",
                "threshold": 0.8
              },
              "rules": [
                { "id": "r1", "priority": 0, "strategy": "itemTitleEqualsAirdate" }
              ]
            }
            """;
        var localJson = """
            {
              "topic": "Tatort",
              "rules": []
            }
            """;

        var config = RuleSetMerger.Build("tatort", communityJson, localJson);

        Assert.NotNull(config);
        Assert.NotNull(config.Resolution);
        Assert.Equal("fuzzy", config.Resolution.Strategy);
        Assert.Equal(0.8f, config.Resolution.Threshold);
    }

    [Fact]
    public void BuildFromJson_includes_resolution_config()
    {
        var json = """
            {
              "topic": "Tatort",
              "confidence": 1.0,
              "resolution": {
                "strategy": "strict",
                "threshold": 0.95,
                "airdateTolerance": 3
              },
              "rules": []
            }
            """;

        var config = RuleSetMerger.BuildFromJson("tatort", json);

        Assert.NotNull(config);
        Assert.NotNull(config.Resolution);
        Assert.Equal("strict", config.Resolution.Strategy);
        Assert.Equal(0.95f, config.Resolution.Threshold);
        Assert.Equal(3, config.Resolution.AirdateTolerance);
    }

    [Fact]
    public void Build_handles_nested_filter_groups()
    {
        var json = """
            {
              "topic": "Test",
              "rules": [
                {
                  "id": "nested",
                  "priority": 0,
                  "strategy": "itemTitleEqualsAirdate",
                  "filters": {
                    "all": [
                      { "field": "duration", "op": "greaterThan", "value": "30" },
                      {
                        "any": [
                          { "field": "channel", "op": "eq", "value": "ZDF" },
                          { "field": "channel", "op": "eq", "value": "ARD" }
                        ]
                      }
                    ]
                  }
                }
              ]
            }
            """;

        var config = RuleSetMerger.Build("test", json, null);

        Assert.NotNull(config);
        var filters = config.Rules[0].Filters;
        Assert.NotNull(filters?.All);
        Assert.Equal(2, filters.All.Length);
        Assert.IsType<FilterNode.ConditionNode>(filters.All[0]);
        var group = Assert.IsType<FilterNode.GroupNode>(filters.All[1]);
        Assert.NotNull(group.Group.Any);
        Assert.Equal(2, group.Group.Any.Length);
    }
}
