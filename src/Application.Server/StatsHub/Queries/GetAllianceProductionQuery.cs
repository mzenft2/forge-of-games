using Ingweland.Fog.Application.Core.CityPlanner.Abstractions;
using Ingweland.Fog.Application.Core.CityPlanner.Stats;
using Ingweland.Fog.Application.Core.Repository.Abstractions;
using Ingweland.Fog.Application.Server.Interfaces;
using Ingweland.Fog.Application.Server.PlayerCity.Abstractions;
using Ingweland.Fog.Application.Server.StatsHub.AllianceProduction;
using Ingweland.Fog.Application.Server.StatsHub.Factories;
using Ingweland.Fog.Dtos.Hoh.Stats;
using Ingweland.Fog.Models.Fog.Entities;
using Ingweland.Fog.Models.Fog.Enums;
using Ingweland.Fog.Models.Hoh.Entities.City;
using Ingweland.Fog.Models.Hoh.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ingweland.Fog.Application.Server.StatsHub.Queries;

public record GetAllianceProductionQuery : IRequest<AllianceProductionDto?>, ICacheableRequest
{
    public required int AllianceId { get; init; }
    public TimeSpan? Duration => TimeSpan.FromHours(3);
    public DateTimeOffset? Expiration { get; }
}

public class GetAllianceProductionQueryHandler(
    IFogDbContext context,
    IHohCityCreationService cityCreationService,
    ICityStatsCalculator cityStatsCalculator,
    ICityPlannerDataService cityPlannerDataService,
    IHohCoreDataRepository coreDataRepository,
    IAllianceProductionDtoFactory allianceProductionDtoFactory,
    ILogger<GetAllianceProductionQueryHandler> logger)
    : IRequestHandler<GetAllianceProductionQuery, AllianceProductionDto?>
{
    private const int RECENT_WORKSHOP_AGES = 3;

    public async Task<AllianceProductionDto?> Handle(GetAllianceProductionQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting alliance production: {AllianceId}", request.AllianceId);
        var members = await context.Alliances
            .AsNoTracking()
            .Where(x => x.Id == request.AllianceId)
            .SelectMany(x => x.Members)
            .Where(x => x.Player.Status == InGameEntityStatus.Active)
            .Select(x => x.Player)
            .ToListAsync(cancellationToken);
        if (members.Count == 0)
        {
            logger.LogInformation("Alliance with ID {AllianceId} has no active members", request.AllianceId);
            return null;
        }

        var playerIds = members.Select(x => x.Id).ToHashSet();
        var latestSnapshotIds = await context.PlayerCitySnapshots
            .Where(x => playerIds.Contains(x.PlayerId) && x.CityId == CityId.Capital)
            .GroupBy(x => x.PlayerId)
            .Select(g => g.OrderByDescending(x => x.CollectedAt).Select(x => x.Id).First())
            .ToListAsync(cancellationToken);
        var snapshots = await context.PlayerCitySnapshots
            .AsNoTracking()
            .Include(x => x.Data)
            .Where(x => latestSnapshotIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.PlayerId, cancellationToken);

        var buildings = (await coreDataRepository.GetBuildingsAsync(CityId.Capital)).ToDictionary(x => x.Id);
        var ageIndexes = (await coreDataRepository.GetAges()).ToDictionary(x => x.Id, x => x.Index);
        var cityPlannerData = await cityPlannerDataService.GetCityPlannerDataAsync(CityId.Capital);
        var expansionSize = cityPlannerData.City.InitConfigs.Grid.ExpansionSize;
        var expansionArea = expansionSize * expansionSize;

        var rows = new List<(Player Player, PlayerCitySnapshot Snapshot, AllianceMemberCityMeasures Measures,
            AllianceProductionValues Values)>();
        var rates = new Dictionary<string, double>();
        var missing = new List<AllianceProductionMissingMemberDto>();
        foreach (var player in members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!snapshots.TryGetValue(player.Id, out var snapshot))
            {
                missing.Add(new AllianceProductionMissingMemberDto {PlayerId = player.Id, Name = player.Name});
                continue;
            }

            if (!rates.TryGetValue(snapshot.AgeId, out var rate))
            {
                rate = AllianceProductionCalculator.CalculateGoodsToFoodRate(buildings.Values, snapshot.AgeId) ?? 0;
                rates.Add(snapshot.AgeId, rate);
            }

            AllianceMemberCityMeasures measures;
            try
            {
                var city = await cityCreationService.Create(snapshot, player.Name);
                var stats = await cityStatsCalculator.Calculate(city);
                measures = Measure(city, stats, buildings, ageIndexes);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to calculate city stats for snapshot {SnapshotId}", snapshot.Id);
                missing.Add(new AllianceProductionMissingMemberDto {PlayerId = player.Id, Name = player.Name});
                continue;
            }

            var values = AllianceProductionCalculator.Calculate(new AllianceProductionInput
            {
                FoodPerHour = snapshot.Food1H,
                RuralFarmFoodPerHour = measures.RuralFarmFoodPerHour,
                GoodsPerHour = snapshot.Goods1H,
                CoinsPerHour = snapshot.Coins1H,
                GoodsToFoodRate = rate,
                PremiumExpansionCount = snapshot.PremiumExpansionCount,
                ExpansionArea = expansionArea,
                LuxuriousFarms = measures.LuxuriousFarms,
                LuxuriousHomes = measures.LuxuriousHomes,
                LuxuriousCultureSites = measures.LuxuriousCultureSites,
                TotalArea = measures.TotalArea,
            });
            rows.Add((player, snapshot, measures, values));
        }

        var ages = rows
            .GroupBy(x => x.Snapshot.AgeId)
            .OrderByDescending(g => ageIndexes.GetValueOrDefault(g.Key))
            .Select(g =>
            {
                var median = AllianceProductionCalculator.Median(g.Select(x => x.Values.DailyValue).ToList());
                return new AllianceProductionAgeDto
                {
                    AgeId = g.Key,
                    GoodsToFoodRate = rates[g.Key],
                    MedianDailyValue = median,
                    Members = g.OrderByDescending(x => x.Values.DailyValue)
                        .Select(x => allianceProductionDtoFactory.CreateMember(x.Player, x.Snapshot, x.Measures,
                            x.Values,
                            AllianceProductionCalculator.DeviationFromMedianPercent(x.Values.DailyValue, median)))
                        .ToList(),
                };
            })
            .ToList();

        return new AllianceProductionDto
        {
            MemberCount = members.Count,
            CoinsPerGood = AllianceProductionCalculator.COINS_PER_GOOD,
            DailyGoodsFurnaceLimit = AllianceProductionCalculator.DAILY_GOODS_FURNACE_LIMIT,
            ExpansionArea = expansionArea,
            FreePremiumExpansions = AllianceProductionCalculator.FREE_PREMIUM_EXPANSIONS,
            Ages = ages,
            MembersWithoutCity = missing.OrderBy(x => x.Name).ToList(),
        };
    }

    private static AllianceMemberCityMeasures Measure(HohCity city, CityStats stats,
        IReadOnlyDictionary<string, Building> buildings, IReadOnlyDictionary<string, int> ageIndexes)
    {
        var cityAgeIndex = ageIndexes.GetValueOrDefault(city.AgeId);
        int farms = 0, farmArea = 0, homeArea = 0, cultureArea = 0, barracksArea = 0;
        int currentWorkshops = 0, recentWorkshops = 0, olderWorkshops = 0;
        int currentWorkshopsArea = 0, nonCurrentWorkshopsArea = 0;
        int luxuriousFarms = 0, luxuriousHomes = 0, luxuriousCultureSites = 0;
        foreach (var entity in city.Entities)
        {
            if (entity.IsLocked || !buildings.TryGetValue(entity.CityEntityId, out var building))
            {
                continue;
            }

            var area = building.Width * building.Length;
            switch (building.Type)
            {
                case BuildingType.Farm:
                    farms++;
                    farmArea += area;
                    break;
                case BuildingType.Home:
                    homeArea += area;
                    break;
                case BuildingType.CultureSite:
                    cultureArea += area;
                    break;
                case BuildingType.Barracks:
                    barracksArea += area;
                    break;
                case BuildingType.Workshop:
                {
                    var agesBack = cityAgeIndex - (building.Age != null ? building.Age.Index : cityAgeIndex);
                    if (agesBack <= 0)
                    {
                        currentWorkshops++;
                        currentWorkshopsArea += area;
                    }
                    else
                    {
                        if (agesBack <= RECENT_WORKSHOP_AGES)
                        {
                            recentWorkshops++;
                        }
                        else
                        {
                            olderWorkshops++;
                        }

                        nonCurrentWorkshopsArea += area;
                    }

                    break;
                }
            }

            switch (building.Group)
            {
                case BuildingGroup.PremiumFarm:
                    luxuriousFarms++;
                    break;
                case BuildingGroup.PremiumHome:
                    luxuriousHomes++;
                    break;
                case BuildingGroup.PremiumCulture:
                    luxuriousCultureSites++;
                    break;
            }
        }

        var occupiedArea = stats.AreasByType.Values.Sum();
        var categorizedArea = farmArea + homeArea + cultureArea + barracksArea + currentWorkshopsArea +
            nonCurrentWorkshopsArea;
        var providedWorkers = stats.ProvidedWorkers.Values.Sum();
        var requiredWorkers = stats.RequiredWorkers.Values.Sum();
        var ruralFarmFood = stats.ProductsByGroup.TryGetValue(BuildingGroup.RuralFarm, out var ruralProducts) &&
            ruralProducts.TryGetValue(AllianceProductionCalculator.FOOD_RESOURCE_ID, out var ruralFood)
                ? ruralFood.OneHour
                : 0;

        return new AllianceMemberCityMeasures
        {
            Farms = farms,
            FarmArea = farmArea,
            HomeArea = homeArea,
            CultureArea = cultureArea,
            BarracksArea = barracksArea,
            CurrentWorkshops = currentWorkshops,
            RecentWorkshops = recentWorkshops,
            OlderWorkshops = olderWorkshops,
            CurrentWorkshopsArea = currentWorkshopsArea,
            NonCurrentWorkshopsArea = nonCurrentWorkshopsArea,
            AdministrationArea = Math.Max(0, occupiedArea - categorizedArea),
            TotalArea = stats.TotalArea,
            EmptyArea = Math.Max(0, stats.TotalArea - occupiedArea),
            FreeWorkers = providedWorkers - requiredWorkers,
            TotalWorkers = providedWorkers,
            CultureUsageRatio = stats.HappinessUsageRatio,
            LuxuriousFarms = luxuriousFarms,
            LuxuriousHomes = luxuriousHomes,
            LuxuriousCultureSites = luxuriousCultureSites,
            RuralFarmFoodPerHour = ruralFarmFood,
        };
    }
}
