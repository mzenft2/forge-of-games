namespace Ingweland.Fog.Application.Server.StatsHub.AllianceProduction;

public record AllianceProductionValues
{
    public double CoinsAsFoodPerDay { get; init; }
    public double DailyValue { get; init; }
    public double DailyValueWithoutDiamonds { get; init; }
    public int DiamondExpansionArea { get; init; }
    public double FoodCollectionFactor { get; init; }
    public double FoodPerDay { get; init; }
    public int FurnaceLimitPercent { get; init; }
    public double GoodsAsFoodPerDay { get; init; }
    public int LuxuriousAdvantageArea { get; init; }
}
