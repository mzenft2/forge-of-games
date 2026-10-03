namespace Ingweland.Fog.Application.Client.Web.StatsHub.ViewModels;

public class AllianceProductionViewModel
{
    public IReadOnlyCollection<AllianceProductionAgeViewModel> Ages { get; init; } = [];
    public double CoinsPerGood { get; init; }
    public int DailyGoodsFurnaceLimit { get; init; }
    public int ExpansionArea { get; init; }
    public int FreePremiumExpansions { get; init; }
    public int MemberCount { get; init; }
    public int MembersWithCityCount { get; init; }
    public IReadOnlyCollection<string> MembersWithoutCity { get; init; } = [];
}
