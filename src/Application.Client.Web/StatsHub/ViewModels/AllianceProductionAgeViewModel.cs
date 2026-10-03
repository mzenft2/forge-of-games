namespace Ingweland.Fog.Application.Client.Web.StatsHub.ViewModels;

public class AllianceProductionAgeViewModel
{
    public required string AgeName { get; init; }
    public required string GoodsToFoodRateFormatted { get; init; }
    public string? MedianDailyValueFormatted { get; init; }
    public IReadOnlyCollection<AllianceMemberProductionViewModel> Members { get; init; } = [];
}
