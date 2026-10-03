using Ingweland.Fog.Application.Core.Constants;
using Ingweland.Fog.Application.Server.StatsHub.AllianceProduction;
using Ingweland.Fog.Dtos.Hoh.Stats;
using Ingweland.Fog.Models.Fog.Entities;

namespace Ingweland.Fog.Application.Server.StatsHub.Factories;

public class AllianceProductionDtoFactory : IAllianceProductionDtoFactory
{
    public AllianceMemberProductionDto CreateMember(Player player, PlayerCitySnapshot snapshot,
        AllianceMemberCityMeasures measures, AllianceProductionValues values, int? deviationFromMedianPercent)
    {
        var expansionCosts = HohConstants.CapitalPremiumExpansionCost;
        return new AllianceMemberProductionDto
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            SnapshotDate = snapshot.CollectedAt,
            FoodPerHour = snapshot.Food1H,
            FoodCollectionFactor = values.FoodCollectionFactor,
            FoodPerDay = values.FoodPerDay,
            GoodsPerHour = snapshot.Goods1H,
            GoodsAsFoodPerDay = values.GoodsAsFoodPerDay,
            FurnaceLimitPercent = values.FurnaceLimitPercent,
            CoinsPerHour = snapshot.Coins1H,
            CoinsAsFoodPerDay = values.CoinsAsFoodPerDay,
            DailyValue = values.DailyValue,
            DeviationFromMedianPercent = deviationFromMedianPercent,
            DailyValueWithoutDiamonds = values.DailyValueWithoutDiamonds,
            DiamondExpansionArea = values.DiamondExpansionArea,
            LuxuriousAdvantageArea = values.LuxuriousAdvantageArea,
            Farms = measures.Farms,
            FarmArea = measures.FarmArea,
            CurrentWorkshops = measures.CurrentWorkshops,
            RecentWorkshops = measures.RecentWorkshops,
            OlderWorkshops = measures.OlderWorkshops,
            FreeWorkers = measures.FreeWorkers,
            TotalWorkers = measures.TotalWorkers,
            CultureUsageRatio = measures.CultureUsageRatio,
            TotalArea = measures.TotalArea,
            EmptyArea = measures.EmptyArea,
            AdministrationArea = measures.AdministrationArea,
            BarracksArea = measures.BarracksArea,
            CurrentWorkshopsArea = measures.CurrentWorkshopsArea,
            NonCurrentWorkshopsArea = measures.NonCurrentWorkshopsArea,
            HomeArea = measures.HomeArea,
            CultureArea = measures.CultureArea,
            LuxuriousFarms = measures.LuxuriousFarms,
            LuxuriousHomes = measures.LuxuriousHomes,
            LuxuriousCultureSites = measures.LuxuriousCultureSites,
            PremiumExpansionCount = snapshot.PremiumExpansionCount,
            PremiumExpansionCost = expansionCosts.Take(snapshot.PremiumExpansionCount).Sum(),
            NextPremiumExpansionCost = snapshot.PremiumExpansionCount < expansionCosts.Length
                ? expansionCosts[snapshot.PremiumExpansionCount]
                : null,
        };
    }
}
