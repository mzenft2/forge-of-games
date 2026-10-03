using Ingweland.Fog.Models.Hoh.Entities.City;
using Ingweland.Fog.Models.Hoh.Entities.Rewards;
using Ingweland.Fog.Models.Hoh.Enums;

namespace Ingweland.Fog.Application.Server.StatsHub.AllianceProduction;

/// <summary>
///     Turns a member's city numbers into a single daily value counted in food, so that cities of the same age can
///     be compared. The method follows the alliance production table by Marek Zenft.
/// </summary>
public static class AllianceProductionCalculator
{
    public const string FOOD_RESOURCE_ID = "resource.food";
    public const string COINS_RESOURCE_ID = "resource.coins";

    // Not available in game data. Spark conversion screen: 84,000 coins = 170 sparks, 70 goods = 95 sparks,
    // so one good is worth (84,000 / 170) / (70 / 95) ≈ 671 coins.
    public const double COINS_PER_GOOD = 671;

    // Not available in game data. Daily limit of goods that can be turned into sparks.
    public const int DAILY_GOODS_FURNACE_LIMIT = 7000;

    // Assumption. Rural farms are ready every 3 hours, so a player realistically collects them about 18 hours a day.
    public const double RURAL_FARM_COLLECTION_HOURS = 18;

    // Assumption. Domestic and luxurious farms (and any other food source) are collected about 22.5 hours a day.
    public const double OTHER_FOOD_COLLECTION_HOURS = 22.5;

    // Assumption. The diamonds for the first premium expansions can be earned in the game, so they are not counted
    // as a bought advantage.
    public const int FREE_PREMIUM_EXPANSIONS = 7;

    // Assumption. How many tiles of regular buildings a luxurious building is worth, minus its own area:
    // farm 20,040 food/h on 9 tiles vs ~940 per tile of a rural farm (≈21.3 tiles), home 5,285 coins/h on 6 tiles
    // vs ~547 per tile of a small home (≈9.7 tiles), culture site estimated.
    public const double LUXURIOUS_FARM_ADVANTAGE_AREA = 12.3;
    public const double LUXURIOUS_HOME_ADVANTAGE_AREA = 3.7;
    public const double LUXURIOUS_CULTURE_ADVANTAGE_AREA = 3;

    public const int MIN_PLAYERS_FOR_MEDIAN = 3;

    /// <summary>
    ///     Share of a day the city's food is realistically collected: rural farms at 18 h, everything else at 22.5 h,
    ///     weighted by how much food each produces.
    /// </summary>
    public static double CalculateFoodCollectionFactor(int ruralFarmFoodPerHour, int totalFoodPerHour)
    {
        if (totalFoodPerHour <= 0)
        {
            return 0;
        }

        var rural = Math.Clamp(ruralFarmFoodPerHour, 0, totalFoodPerHour);
        var other = totalFoodPerHour - rural;
        return (rural * RURAL_FARM_COLLECTION_HOURS + other * OTHER_FOOD_COLLECTION_HOURS) /
            (24.0 * totalFoodPerHour);
    }

    public static AllianceProductionValues Calculate(AllianceProductionInput input)
    {
        var foodCollectionFactor = CalculateFoodCollectionFactor(input.RuralFarmFoodPerHour, input.FoodPerHour);
        var foodPerDay = Math.Round(input.FoodPerHour * foodCollectionFactor * 24);
        var goodsAsFood = Math.Round(input.GoodsPerHour * input.GoodsToFoodRate * 24);
        var coinsAsFood = Math.Round(input.CoinsPerHour * 24 * input.GoodsToFoodRate / COINS_PER_GOOD);
        var dailyValue = foodPerDay + goodsAsFood + coinsAsFood;

        var diamondExpansionArea = Math.Max(0, input.PremiumExpansionCount - FREE_PREMIUM_EXPANSIONS) *
            input.ExpansionArea;
        var luxuriousAdvantageArea = (int) Math.Round(input.LuxuriousFarms * LUXURIOUS_FARM_ADVANTAGE_AREA +
            input.LuxuriousHomes * LUXURIOUS_HOME_ADVANTAGE_AREA +
            input.LuxuriousCultureSites * LUXURIOUS_CULTURE_ADVANTAGE_AREA);
        var diamondShare = input.TotalArea > 0
            ? Math.Min(1, (double) (diamondExpansionArea + luxuriousAdvantageArea) / input.TotalArea)
            : 0;

        return new AllianceProductionValues
        {
            FoodCollectionFactor = foodCollectionFactor,
            FoodPerDay = foodPerDay,
            GoodsAsFoodPerDay = goodsAsFood,
            CoinsAsFoodPerDay = coinsAsFood,
            DailyValue = dailyValue,
            FurnaceLimitPercent = (int) Math.Round(input.GoodsPerHour * 24 * 100.0 / DAILY_GOODS_FURNACE_LIMIT,
                MidpointRounding.AwayFromZero),
            DiamondExpansionArea = diamondExpansionArea,
            LuxuriousAdvantageArea = luxuriousAdvantageArea,
            DailyValueWithoutDiamonds = Math.Round(dailyValue * (1 - diamondShare)),
        };
    }

    public static double? Median(IReadOnlyCollection<double> values)
    {
        if (values.Count < MIN_PLAYERS_FOR_MEDIAN)
        {
            return null;
        }

        var sorted = values.Order().ToList();
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : Math.Round((sorted[middle - 1] + sorted[middle]) / 2);
    }

    public static int? DeviationFromMedianPercent(double value, double? median)
    {
        if (median is not > 0)
        {
            return null;
        }

        return (int) Math.Round((value / median.Value - 1) * 100, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    ///     How much food one good is worth in the given age: the food a rural farm of that age (highest level, full
    ///     culture bonus) makes on the tiles a workshop of the same age takes, divided by the workshop's goods per hour.
    /// </summary>
    public static double? CalculateGoodsToFoodRate(IEnumerable<Building> buildings, string ageId)
    {
        var ageBuildings = buildings.Where(b => b.Age?.Id == ageId).ToList();
        var farm = ageBuildings.Where(b => b.Group == BuildingGroup.RuralFarm).MaxBy(b => b.Level);
        if (farm == null)
        {
            return null;
        }

        var farmFoodPerHour = GetFullyBuffedHourlyProduction(farm, r => r == FOOD_RESOURCE_ID);
        var farmArea = farm.Width * farm.Length;
        if (farmFoodPerHour is not > 0 || farmArea == 0)
        {
            return null;
        }

        var workshops = ageBuildings
            .Where(b => b.Type == BuildingType.Workshop && b.Group != BuildingGroup.PremiumWorkshop)
            .GroupBy(b => b.Group)
            .Select(g => g.MaxBy(b => b.Level)!)
            .Select(b => (Area: b.Width * b.Length,
                GoodsPerHour: GetFullyBuffedHourlyProduction(b,
                    r => r != FOOD_RESOURCE_ID && r != COINS_RESOURCE_ID)))
            .Where(x => x is {GoodsPerHour: > 0, Area: > 0})
            .ToList();
        if (workshops.Count == 0)
        {
            return null;
        }

        var foodPerTile = farmFoodPerHour.Value / farmArea;
        return workshops.Average(x => foodPerTile * x.Area / x.GoodsPerHour!.Value);
    }

    private static double? GetFullyBuffedHourlyProduction(Building building, Func<string, bool> resourceFilter)
    {
        foreach (var production in building.Components.OfType<ProductionComponent>())
        {
            var reward = production.Products.OfType<ResourceReward>()
                .FirstOrDefault(r => resourceFilter(r.ResourceId));
            if (reward == null || production.ProductionTime <= 0)
            {
                continue;
            }

            var hours = production.ProductionTime / 3600.0;
            double bonus = 0;
            if (building.BuffDetails != null)
            {
                var factor = building.BuffDetails.Resources?.FirstOrDefault(r => r.ResourceId == reward.ResourceId)
                    ?.Factor ?? building.BuffDetails.Factor;
                bonus = Math.Floor(building.BuffDetails.Value * factor);
            }

            return reward.Amount / hours + bonus;
        }

        return null;
    }
}
