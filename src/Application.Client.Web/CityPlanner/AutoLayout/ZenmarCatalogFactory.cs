using Ingweland.Fog.Application.Client.Web.CityPlanner.Abstractions;
using Ingweland.Fog.Dtos.Hoh.City;
using Ingweland.Fog.Dtos.Hoh.CityPlanner;
using Ingweland.Fog.Models.Hoh.Constants;
using Ingweland.Fog.Models.Hoh.Entities.City;
using Ingweland.Fog.Models.Hoh.Entities.Rewards;
using Ingweland.Fog.Models.Hoh.Enums;

namespace Ingweland.Fog.Application.Client.Web.CityPlanner.AutoLayout;

/// <summary>
///     Builds the Zenmar Strategy engine catalog from Forge core data.
///     Port of scripts/read-fog.cjs + scripts/build-data.cjs of the engine repository.
/// </summary>
public class ZenmarCatalogFactory : IZenmarCatalogFactory
{
    public const string EARLY_GOTHIC_ERA_ID = "early-gothic";
    public const string FOUNTAIN_ID = "fountain";
    public const string TOWER_ID = "collectableMinoanWatchtowerV2";

    private static readonly string[] MandatoryIds =
    [
        "cityHall", "furnace", "infantryBarracks", "rangedBarracks", "cavalryBarracks", "heavyInfantryBarracks",
        "siegeBarracks",
    ];

    // Forge age id -> engine era id
    public static readonly IReadOnlyDictionary<string, string> EraIds = new Dictionary<string, string>
    {
        {AgeIds.STONE_AGE, "stone-age"},
        {AgeIds.BRONZE_AGE, "bronze-age"},
        {AgeIds.MINOAN_ERA, "minoan"},
        {AgeIds.CLASSIC_GREECE, "classic-greece"},
        {AgeIds.EARLY_ROME, "early-rome"},
        {AgeIds.ROMAN_EMPIRE, "roman-empire"},
        {AgeIds.BYZANTINE_ERA, "byzantine"},
        {AgeIds.AGE_OF_THE_FRANKS, "franks"},
        {AgeIds.FEUDAL_AGE, "feudal"},
        {AgeIds.IBERIAN_ERA, "iberian"},
        {AgeIds.KINGDOM_OF_SICILY, "sicily"},
        {AgeIds.HIGH_MIDDLE_AGE, "high-middle-ages"},
        {AgeIds.EARLY_GOTHIC_ERA, EARLY_GOTHIC_ERA_ID},
        {AgeIds.LATE_GOTHIC_ERA, "late-gothic"},
    };

    // Forge building group -> engine building id
    public static readonly IReadOnlyDictionary<BuildingGroup, string> BuildingIds =
        new Dictionary<BuildingGroup, string>
        {
            {BuildingGroup.Alchemist, "alchemist"},
            {BuildingGroup.Artisan, "artisan"},
            {BuildingGroup.AverageHome, "averageHome"},
            {BuildingGroup.Carpenter, "carpenter"},
            {BuildingGroup.CavalryBarracks, "cavalryBarracks"},
            {BuildingGroup.CityHall, "cityHall"},
            {BuildingGroup.CollectableAmphitheatre, "collectableAmphitheatre"},
            {BuildingGroup.CollectableArchitectsStudioV2, "collectableArchitectsStudioV2"},
            {BuildingGroup.CollectableMinoanWatchtowerV2, TOWER_ID},
            {BuildingGroup.CollectableSchoolV2, "collectableSchoolV2"},
            {BuildingGroup.CompactCulture, "compactCulture"},
            {BuildingGroup.DomesticFarm, "domesticFarm"},
            {BuildingGroup.EvolvingFountainOfYouth, FOUNTAIN_ID},
            {BuildingGroup.Furnace, "furnace"},
            {BuildingGroup.Glassblower, "glassblower"},
            {BuildingGroup.HeavyInfantryBarracks, "heavyInfantryBarracks"},
            {BuildingGroup.HeroAcademy, "heroAcademy"},
            {BuildingGroup.InfantryBarracks, "infantryBarracks"},
            {BuildingGroup.Jeweler, "jeweler"},
            {BuildingGroup.LargeCulture, "largeCulture"},
            {BuildingGroup.LittleCulture, "littleCulture"},
            {BuildingGroup.ModerateCulture, "moderateCulture"},
            {BuildingGroup.PremiumCulture, "premiumCulture"},
            {BuildingGroup.PremiumFarm, "premiumFarm"},
            {BuildingGroup.PremiumHome, "premiumHome"},
            {BuildingGroup.RangedBarracks, "rangedBarracks"},
            {BuildingGroup.RuralFarm, "ruralFarm"},
            {BuildingGroup.Scribe, "scribe"},
            {BuildingGroup.SiegeBarracks, "siegeBarracks"},
            {BuildingGroup.SmallHome, "smallHome"},
            {BuildingGroup.SpiceMerchant, "spiceMerchant"},
            {BuildingGroup.StoneMason, "stoneMason"},
            {BuildingGroup.Tailor, "tailor"},
        };

    private static string? GetKind(BuildingType type)
    {
        return type switch
        {
            BuildingType.Barracks => "barracks",
            BuildingType.CityHall or BuildingType.Collectable or BuildingType.Evolving or BuildingType.Special =>
                "special",
            BuildingType.CultureSite => "happiness",
            BuildingType.Farm => "farm",
            BuildingType.Home => "home",
            BuildingType.Workshop => "workshop",
            _ => null,
        };
    }

    public ZenmarCatalog Create(CityPlannerDataDto cityPlannerData)
    {
        var ageRanks = cityPlannerData.Ages.ToDictionary(a => a.Id, a => a.Index);
        var all = new List<(ZenmarBuilding Building, BuildingDto Source)>();
        BuildingDto? fountain = null;
        foreach (var building in cityPlannerData.Buildings)
        {
            if (!building.CityIds.Contains(CityId.Capital) ||
                !BuildingIds.TryGetValue(building.Group, out var id))
            {
                continue;
            }

            var kind = GetKind(building.Type);
            if (kind == null)
            {
                continue;
            }

            if (id == FOUNTAIN_ID)
            {
                fountain ??= building;
                continue;
            }

            all.Add((CreateBuilding(building, id, kind), building));
        }

        var eras = new List<ZenmarEra>();
        foreach (var (ageId, eraId) in EraIds)
        {
            if (!ageRanks.TryGetValue(ageId, out var max))
            {
                continue;
            }

            var selected = new Dictionary<string, (ZenmarBuilding Building, BuildingDto Source)>();
            foreach (var item in all)
            {
                if (item.Building.Age == null || !ageRanks.TryGetValue(item.Building.Age, out var rank) || rank > max)
                {
                    continue;
                }

                if (!selected.TryGetValue(item.Building.Id, out var prev))
                {
                    selected[item.Building.Id] = item;
                    continue;
                }

                var prevRank = ageRanks[prev.Building.Age!];
                if (rank > prevRank || rank == prevRank && item.Building.Level > prev.Building.Level)
                {
                    selected[item.Building.Id] = item;
                }
            }

            var current = selected.Values
                .Where(b => b.Building.Kind == "workshop" && b.Building.Age == ageId)
                .Select(b => b.Building.Id)
                .Order(StringComparer.Ordinal)
                .ToList();
            // The engine keeps the Gothic workshop order of its original form
            if (current.Contains("jeweler"))
            {
                current = ["jeweler", "glassblower", "alchemist"];
            }

            var buildings = selected.Values
                .Select(b => b.Building.With(b.Building.Kind == "workshop" && !current.Contains(b.Building.Id)))
                .ToList();
            var tower = buildings.FirstOrDefault(b => b.Id == TOWER_ID);
            if (tower != null)
            {
                tower.Levels = all.Where(b => b.Building.Id == TOWER_ID)
                    .OrderBy(b => b.Building.Level)
                    .Select(b => new ZenmarBuildingLevel(b.Building.Level, b.Building.Points, b.Building.Range,
                        b.Building.Workers, b.Building.SourceId))
                    .ToList();
            }

            var sources = selected.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Source);
            if (tower != null)
            {
                foreach (var towerLevel in all.Where(b => b.Building.Id == TOWER_ID))
                {
                    sources[$"{TOWER_ID}:{towerLevel.Building.Level}"] = towerLevel.Source;
                }
            }

            if (fountain != null)
            {
                sources[FOUNTAIN_ID] = fountain;
            }

            eras.Add(new ZenmarEra
            {
                Id = eraId,
                Name = cityPlannerData.Ages.First(a => a.Id == ageId).Name,
                SourceAge = ageId,
                Index = max,
                Current = current,
                Mandatory = MandatoryIds.Where(selected.ContainsKey).ToList(),
                OldWorkshop = buildings.FirstOrDefault(b => b.Id == "stoneMason" && b.OldWorkshop)?.Id,
                Buildings = buildings,
                FountainLevels = CreateFountainLevels(fountain, ageId),
                SourceBuildings = sources,
            });
        }

        return new ZenmarCatalog
        {
            Eras = eras,
            FountainSourceId = fountain != null ? $"building.{fountain.Id}" : null,
        };
    }

    private static ZenmarBuilding CreateBuilding(BuildingDto building, string id, string kind)
    {
        var productions = building.Components.OfType<ProductionComponent>().ToList();
        var production = productions.FirstOrDefault();
        var culture = building.Components.OfType<CultureComponent>().FirstOrDefault();
        var grant = building.Components.OfType<GrantWorkerComponent>().FirstOrDefault();
        return new ZenmarBuilding
        {
            Id = id,
            SourceId = $"building.{building.Id}",
            Name = building.Name,
            Kind = kind,
            W = building.Width,
            H = building.Length,
            Level = building.Level,
            Age = building.Age?.Id,
            Workers = grant?.WorkerCount ?? 0,
            Needs = Math.Max(0, productions.Select(p => p.WorkerBehaviour?.Amount ?? 0).DefaultIfEmpty(0).Max()),
            Hours = (production?.ProductionTime ?? 0) / 3600.0,
            Rewards = production?.Products.OfType<ResourceReward>()
                .Select(r => new ZenmarReward(StripResourcePrefix(r.ResourceId), r.Amount))
                .ToList() ?? [],
            HappyMax = building.BuffDetails?.Value ?? 0,
            Factors = building.BuffDetails?.Resources?
                .GroupBy(r => StripResourcePrefix(r.ResourceId))
                .ToDictionary(g => g.Key, g => g.First().Factor) ?? new Dictionary<string, double>(),
            Factor = building.BuffDetails?.Factor ?? 0,
            Range = culture?.Range ?? 0,
            Points = culture?.Value ?? 0,
            Premium = id.StartsWith("premium", StringComparison.Ordinal),
        };
    }

    private static IReadOnlyCollection<ZenmarFountainLevel> CreateFountainLevels(BuildingDto? fountain, string ageId)
    {
        var culture = fountain?.CultureComponent;
        if (culture == null || !culture.Values.TryGetValue(ageId, out var points) || points.Count == 0)
        {
            return [];
        }

        var lastLevel = points.Keys.Max();
        return culture.Ranges.Keys.Order()
            .Select(level => new ZenmarFountainLevel(level,
                points.TryGetValue(Math.Min(level, lastLevel), out var p) ? p : 0, culture.Ranges[level]))
            .ToList();
    }

    private static string StripResourcePrefix(string resourceId)
    {
        return resourceId.StartsWith("resource.", StringComparison.Ordinal) ? resourceId[9..] : resourceId;
    }
}
