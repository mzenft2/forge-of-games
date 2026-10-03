using Ingweland.Fog.Application.Client.Web.StatsHub.Abstractions;
using Ingweland.Fog.Application.Client.Web.StatsHub.ViewModels;
using Ingweland.Fog.Dtos.Hoh;
using Ingweland.Fog.Dtos.Hoh.Stats;

namespace Ingweland.Fog.Application.Client.Web.StatsHub.Factories;

public class AllianceProductionViewModelFactory : IAllianceProductionViewModelFactory
{
    public AllianceProductionViewModel Create(AllianceProductionDto dto, IReadOnlyDictionary<string, AgeDto> ages)
    {
        return new AllianceProductionViewModel
        {
            MemberCount = dto.MemberCount,
            CoinsPerGood = dto.CoinsPerGood,
            DailyGoodsFurnaceLimit = dto.DailyGoodsFurnaceLimit,
            ExpansionArea = dto.ExpansionArea,
            FreePremiumExpansions = dto.FreePremiumExpansions,
            MembersWithCityCount = dto.Ages.Sum(x => x.Members.Count),
            MembersWithoutCity = dto.MembersWithoutCity.Select(x => x.Name).ToList(),
            Ages = dto.Ages.Select(x => CreateAge(x, ages)).ToList(),
        };
    }

    private static AllianceProductionAgeViewModel CreateAge(AllianceProductionAgeDto dto,
        IReadOnlyDictionary<string, AgeDto> ages)
    {
        var rate = dto.GoodsToFoodRate > 0 ? dto.GoodsToFoodRate.ToString("N0") : "—";
        var median = dto.MedianDailyValue.HasValue ? ToMillions(dto.MedianDailyValue.Value) : null;
        return new AllianceProductionAgeViewModel
        {
            AgeName = ages.TryGetValue(dto.AgeId, out var age) ? age.Name : dto.AgeId,
            GoodsToFoodRateFormatted = rate,
            MedianDailyValueFormatted = median,
            Members = dto.Members.Select(x => CreateMember(x, rate, median)).ToList(),
        };
    }

    private static AllianceMemberProductionViewModel CreateMember(AllianceMemberProductionDto dto,
        string goodsToFoodRate, string? medianDailyValue)
    {
        var diamondArea = dto.DiamondExpansionArea + dto.LuxuriousAdvantageArea;
        var withoutDiamondsFactor = dto.TotalArea > 0 ? Math.Max(0, 1 - (double) diamondArea / dto.TotalArea) : 1;
        return new AllianceMemberProductionViewModel
        {
            Data = dto,
            DailyValueFormatted = ToMillions(dto.DailyValue),
            FoodPerDayFormatted = ToMillions(dto.FoodPerDay),
            FoodPerDayNonStopFormatted = ToMillions(dto.FoodPerHour * 24.0),
            GoodsAsFoodFormatted = ToMillions(dto.GoodsAsFoodPerDay),
            CoinsAsFoodFormatted = ToMillions(dto.CoinsAsFoodPerDay),
            WithoutDiamondsFormatted = ToMillions(dto.DailyValueWithoutDiamonds),
            WithoutDiamondsFactorFormatted = withoutDiamondsFactor.ToString("N2"),
            WithoutDiamondsSharePercent = (int) Math.Round(100 - withoutDiamondsFactor * 100),
            GoodsToFoodRateFormatted = goodsToFoodRate,
            MedianDailyValueFormatted = medianDailyValue,
            SnapshotDateFormatted = dto.SnapshotDate.ToString("d"),
            CityBarSegments = CreateCityBar(dto),
        };
    }

    // Order in which a city fills up: administration, barracks, current workshops, farms, old workshops, homes,
    // culture. Whatever is left is empty.
    private static IReadOnlyList<AllianceProductionCityBarSegment> CreateCityBar(AllianceMemberProductionDto dto)
    {
        if (dto.TotalArea <= 0)
        {
            return [];
        }

        return new (AllianceProductionCityBarSegmentType Type, int Area)[]
            {
                (AllianceProductionCityBarSegmentType.Administration, dto.AdministrationArea),
                (AllianceProductionCityBarSegmentType.Barracks, dto.BarracksArea),
                (AllianceProductionCityBarSegmentType.CurrentWorkshops, dto.CurrentWorkshopsArea),
                (AllianceProductionCityBarSegmentType.Farms, dto.FarmArea),
                (AllianceProductionCityBarSegmentType.OldWorkshops, dto.NonCurrentWorkshopsArea),
                (AllianceProductionCityBarSegmentType.Homes, dto.HomeArea),
                (AllianceProductionCityBarSegmentType.Culture, dto.CultureArea),
            }
            .Where(x => x.Area > 0)
            .Select(x => new AllianceProductionCityBarSegment(x.Type, x.Area * 100.0 / dto.TotalArea))
            .ToList();
    }

    private static string ToMillions(double value)
    {
        return (value / 1_000_000).ToString("N2");
    }
}
