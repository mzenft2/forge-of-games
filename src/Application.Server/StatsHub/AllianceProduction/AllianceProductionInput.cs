namespace Ingweland.Fog.Application.Server.StatsHub.AllianceProduction;

public record AllianceProductionInput
{
    public int CoinsPerHour { get; init; }
    public int ExpansionArea { get; init; }
    public int FoodPerHour { get; init; }
    public int GoodsPerHour { get; init; }
    public double GoodsToFoodRate { get; init; }
    public int LuxuriousCultureSites { get; init; }
    public int LuxuriousFarms { get; init; }
    public int LuxuriousHomes { get; init; }
    public int PremiumExpansionCount { get; init; }
    public int RuralFarmFoodPerHour { get; init; }
    public int TotalArea { get; init; }
}
