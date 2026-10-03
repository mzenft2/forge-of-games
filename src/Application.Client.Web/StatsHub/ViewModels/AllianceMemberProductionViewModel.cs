using Ingweland.Fog.Dtos.Hoh.Stats;

namespace Ingweland.Fog.Application.Client.Web.StatsHub.ViewModels;

public class AllianceMemberProductionViewModel
{
    public required IReadOnlyList<AllianceProductionCityBarSegment> CityBarSegments { get; init; }
    public required string CoinsAsFoodFormatted { get; init; }
    public required AllianceMemberProductionDto Data { get; init; }
    public required string DailyValueFormatted { get; init; }
    public required string FoodPerDayFormatted { get; init; }
    public required string FoodPerDayNonStopFormatted { get; init; }
    public required string GoodsAsFoodFormatted { get; init; }
    public required string GoodsToFoodRateFormatted { get; init; }
    public string? MedianDailyValueFormatted { get; init; }
    public required string SnapshotDateFormatted { get; init; }
    public required string WithoutDiamondsFactorFormatted { get; init; }
    public required string WithoutDiamondsFormatted { get; init; }
    public int WithoutDiamondsSharePercent { get; init; }
}
