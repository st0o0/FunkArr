namespace FunkArr.ArrApi.Newznab.Models;

public sealed record NewznabCategory
{
    public static readonly NewznabCategory Movie = new(2000, "Movies", "2040", "2030");
    public static readonly NewznabCategory Tv = new(5000, "TV", "5040", "5030");

    private NewznabCategory(int parentId, string label, string hdId, string sdId)
    {
        ParentId = parentId;
        Label = label;
        HdId = hdId;
        SdId = sdId;
    }

    public int ParentId { get; }
    public string Label { get; }
    public string HdId { get; }
    public string SdId { get; }

    public string CategoryId(int quality) => quality >= 720 ? HdId : SdId;

    public string DisplayName(int quality) => quality >= 720
        ? $"{Label} > HD"
        : $"{Label} > SD";

    public static NewznabCategory? FromCat(int? cat) => cat switch
    {
        >= 2000 and < 3000 => Movie,
        _ => null,
    };
}
