namespace Ingweland.Fog.Dtos.Hoh.Stats;

public class AllianceMemberProductionDto
{
    public int AdministrationArea { get; init; }
    public int BarracksArea { get; init; }
    public double CoinsAsFoodPerDay { get; init; }
    public int CoinsPerHour { get; init; }
    public float CultureUsageRatio { get; init; }
    public int CurrentWorkshops { get; init; }
    public int CurrentWorkshopsArea { get; init; }
    public int CultureArea { get; init; }
    public double DailyValue { get; init; }
    public double DailyValueWithoutDiamonds { get; init; }
    public int? DeviationFromMedianPercent { get; init; }
    public int DiamondExpansionArea { get; init; }
    public int EmptyArea { get; init; }
    public int FarmArea { get; init; }
    public int Farms { get; init; }
    public double FoodCollectionFactor { get; init; }
    public int FoodPerHour { get; init; }
    public double FoodPerDay { get; init; }
    public int FreeWorkers { get; init; }
    public int FurnaceLimitPercent { get; init; }
    public double GoodsAsFoodPerDay { get; init; }
    public int GoodsPerHour { get; init; }
    public int HomeArea { get; init; }
    public int LuxuriousAdvantageArea { get; init; }
    public int LuxuriousCultureSites { get; init; }
    public int LuxuriousFarms { get; init; }
    public int LuxuriousHomes { get; init; }
    public int? NextPremiumExpansionCost { get; init; }
    public int OlderWorkshops { get; init; }
    public int NonCurrentWorkshopsArea { get; init; }
    public required string PlayerName { get; init; }
    public int PlayerId { get; init; }
    public int PremiumExpansionCost { get; init; }
    public int PremiumExpansionCount { get; init; }
    public int RecentWorkshops { get; init; }
    public required DateOnly SnapshotDate { get; init; }
    public int TotalArea { get; init; }
    public int TotalWorkers { get; init; }
}
