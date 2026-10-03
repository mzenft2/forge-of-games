namespace Ingweland.Fog.Application.Server.StatsHub.AllianceProduction;

public record AllianceMemberCityMeasures
{
    public int AdministrationArea { get; init; }
    public int BarracksArea { get; init; }
    public float CultureUsageRatio { get; init; }
    public int CultureArea { get; init; }
    public int CurrentWorkshops { get; init; }
    public int CurrentWorkshopsArea { get; init; }
    public int EmptyArea { get; init; }
    public int FarmArea { get; init; }
    public int Farms { get; init; }
    public int FreeWorkers { get; init; }
    public int HomeArea { get; init; }
    public int LuxuriousCultureSites { get; init; }
    public int LuxuriousFarms { get; init; }
    public int LuxuriousHomes { get; init; }
    public int NonCurrentWorkshopsArea { get; init; }
    public int OlderWorkshops { get; init; }
    public int RecentWorkshops { get; init; }
    public int RuralFarmFoodPerHour { get; init; }
    public int TotalArea { get; init; }
    public int TotalWorkers { get; init; }
}
