using System.Text.Json.Serialization;

namespace FunkArr.ArrApi.Sabnzbd.Models;

public sealed record SabnzbdConfigResponse(
    [property: JsonPropertyName("config")] SabnzbdConfig Config);

public sealed record SabnzbdConfig(
    [property: JsonPropertyName("misc")] SabnzbdMiscConfig Misc,
    [property: JsonPropertyName("categories")] IReadOnlyList<SabnzbdCategoryConfig> Categories,
    [property: JsonPropertyName("sorters")] IReadOnlyList<object> Sorters);

public sealed record SabnzbdMiscConfig(
    [property: JsonPropertyName("complete_dir")] string CompleteDir,
    [property: JsonPropertyName("enable_tv_sorting")] bool EnableTvSorting,
    [property: JsonPropertyName("enable_movie_sorting")] bool EnableMovieSorting,
    [property: JsonPropertyName("enable_date_sorting")] bool EnableDateSorting,
    [property: JsonPropertyName("pre_check")] bool PreCheck,
    [property: JsonPropertyName("history_retention")] string HistoryRetention,
    [property: JsonPropertyName("history_retention_option")] string HistoryRetentionOption,
    [property: JsonPropertyName("history_retention_number")] int HistoryRetentionNumber,
    [property: JsonPropertyName("tv_categories")] IReadOnlyList<string> TvCategories,
    [property: JsonPropertyName("movie_categories")] IReadOnlyList<string> MovieCategories,
    [property: JsonPropertyName("date_categories")] IReadOnlyList<string> DateCategories);

public sealed record SabnzbdCategoryConfig(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("dir")] string Dir,
    [property: JsonPropertyName("newzbin")] string Newzbin,
    [property: JsonPropertyName("priority")] int Priority);
