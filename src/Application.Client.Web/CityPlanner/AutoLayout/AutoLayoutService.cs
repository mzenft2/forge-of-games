using Ingweland.Fog.Application.Client.Web.CityPlanner.Abstractions;
using Ingweland.Fog.Application.Core.CityPlanner.Abstractions;
using Ingweland.Fog.Application.Core.Constants;
using Ingweland.Fog.Dtos.Hoh.City;
using Ingweland.Fog.Dtos.Hoh.CityPlanner;
using Ingweland.Fog.Models.Fog.Entities;
using Ingweland.Fog.Models.Hoh.Entities.City;
using Ingweland.Fog.Models.Hoh.Enums;

namespace Ingweland.Fog.Application.Client.Web.CityPlanner.AutoLayout;

/// <summary>
///     Translates between a City Planner city and the Zenmar Strategy engine (input settings and result layout).
/// </summary>
public class AutoLayoutService(IHohCityFactory hohCityFactory) : IAutoLayoutService
{
    private const int DEFAULT_WORKSHOP_COUNT = 2;

    private static readonly HashSet<string> ProductionKinds = ["farm", "home", "workshop"];

    public AutoLayoutSettings CreateSettings(HohCity city, CityPlannerDataDto cityPlannerData, ZenmarCatalog catalog)
    {
        var expansionSize = cityPlannerData.City.InitConfigs.Grid.ExpansionSize;
        var expansions = cityPlannerData.Expansions.Where(e => e.CityId == city.InGameCityId).ToList();
        var minX = expansions.Min(e => e.X);
        var minY = expansions.Min(e => e.Y);
        var eraId = ZenmarCatalogFactory.EraIds.GetValueOrDefault(city.AgeId) ?? catalog.Eras.Last().Id;
        var era = catalog.Eras.First(e => e.Id == eraId);
        var settings = new AutoLayoutSettings
        {
            AgeId = era.SourceAge,
            EraId = era.Id,
            ExpansionSize = expansionSize,
            MapOriginX = minX,
            MapOriginY = minY,
        };

        foreach (var expansion in expansions)
        {
            var column = (expansion.X - minX) / expansionSize;
            var row = (expansion.Y - minY) / expansionSize;
            if (column >= AutoLayoutSettings.GRID_COLUMNS || row >= AutoLayoutSettings.GRID_ROWS)
            {
                continue;
            }

            var index = row * AutoLayoutSettings.GRID_COLUMNS + column;
            settings.ExpansionIds[index] = expansion.Id;
            settings.Tiles[index] = expansion.Type == ExpansionType.Undefined &&
                expansion.SubType != ExpansionSubType.Water &&
                (city.UnlockedExpansions.Count == 0 || city.UnlockedExpansions.Contains(expansion.Id) ||
                    city.UnlockedPremiumExpansions.Contains(expansion.Id));
        }

        var buildings = cityPlannerData.Buildings.ToDictionary(b => b.Id);
        var groups = city.Entities
            .Select(e => (Entity: e, Building: buildings.GetValueOrDefault(e.CityEntityId)))
            .Where(t => t.Building != null)
            .GroupBy(t => t.Building!.Group)
            .ToDictionary(g => g.Key, g => g.ToList());
        int Count(BuildingGroup group) => groups.TryGetValue(group, out var list) ? list.Count : 0;

        settings.PremiumFarm = Count(BuildingGroup.PremiumFarm);
        settings.PremiumHome = Count(BuildingGroup.PremiumHome);
        settings.PremiumCulture = Count(BuildingGroup.PremiumCulture);
        settings.RuralFarms = Count(BuildingGroup.RuralFarm);
        settings.DomesticFarms = Count(BuildingGroup.DomesticFarm);
        settings.School = Count(BuildingGroup.CollectableSchoolV2) > 0;
        settings.ArchitectsStudio = Count(BuildingGroup.CollectableArchitectsStudioV2) > 0;
        settings.HeroAcademy = Count(BuildingGroup.HeroAcademy) > 0;
        if (groups.TryGetValue(BuildingGroup.CollectableMinoanWatchtowerV2, out var towers))
        {
            settings.Tower = true;
            settings.TowerLevel = Math.Clamp(towers.Max(t => t.Building!.Level), 1, 10);
        }

        if (groups.TryGetValue(BuildingGroup.EvolvingFountainOfYouth, out var fountains))
        {
            settings.Fountain = era.FountainLevels.Count > 0;
            settings.FountainLevel = Math.Clamp(fountains.Max(t => t.Entity.Level), 1, 50);
        }

        foreach (var workshopId in era.Current)
        {
            var group = ZenmarCatalogFactory.BuildingIds.First(kvp => kvp.Value == workshopId).Key;
            var count = groups.TryGetValue(group, out var list)
                ? list.Count(t => t.Building!.Age?.Id == era.SourceAge)
                : 0;
            settings.WorkshopCounts[workshopId] = Math.Min(20, count > 0 ? count : DEFAULT_WORKSHOP_COUNT);
        }

        return settings;
    }

    public IReadOnlyDictionary<string, object?> CreateEngineInput(AutoLayoutSettings settings,
        ZenmarCatalog catalog)
    {
        var era = catalog.Eras.First(e => e.Id == settings.EraId);
        var optional = new List<string>();
        if (settings.School)
        {
            optional.Add("collectableSchoolV2");
        }

        if (settings.ArchitectsStudio)
        {
            optional.Add("collectableArchitectsStudioV2");
        }

        if (settings.HeroAcademy)
        {
            optional.Add("heroAcademy");
        }

        if (settings.Tower)
        {
            optional.Add(ZenmarCatalogFactory.TOWER_ID);
        }

        var availableOptional = era.Buildings.Select(b => b.Id).ToHashSet();
        var oldCount = era.OldWorkshop != null ? Math.Clamp(settings.OldCount, 0, 50) : 0;
        var input = new Dictionary<string, object?>
        {
            ["catalog"] = catalog,
            ["referenceSchemes"] = true,
            ["balanced"] = false,
            ["goalVersion"] = 4,
            ["searchBudgetVersion"] = 1,
            ["fuelVersion"] = 3,
            ["engineMode"] = "portfolio",
            ["era"] = era.Id,
            ["fountainMode"] = "catalog",
            ["tiles"] = settings.Tiles,
            ["optional"] = optional.Where(availableOptional.Contains).ToList(),
            ["towerLevel"] = Math.Clamp(settings.TowerLevel, 1, 10),
            ["premium"] = new Dictionary<string, int>
            {
                ["premiumFarm"] = Math.Clamp(settings.PremiumFarm, 0, 100),
                ["premiumHome"] = Math.Clamp(settings.PremiumHome, 0, 100),
                ["premiumCulture"] = Math.Clamp(settings.PremiumCulture, 0, 100),
            },
            ["ownedFarms"] = new Dictionary<string, int>
            {
                ["ruralFarm"] = Math.Clamp(settings.RuralFarms, 0, 80),
                ["domesticFarm"] = Math.Clamp(settings.DomesticFarms, 0, 80),
            },
            ["fullArmy"] = settings.FullArmy,
            ["subscription"] = settings.Subscription,
            ["fountain"] = settings.Fountain && era.FountainLevels.Count > 0,
            ["fountainLevel"] = Math.Clamp(settings.FountainLevel, 1, 50),
            ["fountainPoints"] = 0,
            ["fountainRange"] = 0,
            ["interval"] = Math.Clamp(settings.Interval, 0.25, 16),
            ["night"] = Math.Clamp(settings.Night, 0, 16),
            ["goodsTarget"] = 0,
            // Conversion rates of the in-game spark exchange. They are not part of the game data.
            ["sparkCap"] = Math.Clamp(settings.SparkCap, 0, 100000),
            ["goodsPerBatch"] = 70,
            ["sparksPerBatch"] = 95,
            ["goldPerBatch"] = 84000,
            ["goldSparksPerBatch"] = 170,
            ["goldCap"] = 8400000,
            ["searchStarts"] = 6,
            ["searchSeconds"] = AutoLayoutSettings.SearchSecondsOptions.Contains(settings.SearchSeconds)
                ? settings.SearchSeconds
                : 60,
            ["fuelSuggested"] = false,
            ["oldCount"] = oldCount,
            ["fuelGoods"] = 0,
            ["workshopCounts"] = era.Current.ToDictionary(id => id,
                id => Math.Clamp(settings.WorkshopCounts.GetValueOrDefault(id), 0, 20)),
            ["compareSparks"] = settings.CompareSparks && era.OldWorkshop != null,
            ["armyMin"] = era.Mandatory.Where(id => id.EndsWith("Barracks", StringComparison.Ordinal))
                .ToDictionary(id => id, _ => 100),
            ["seedLayouts"] = Array.Empty<object>(),
            ["startSeed"] = Random.Shared.Next(1, int.MaxValue),
        };
        return input;
    }

    public HohCity CreateCity(HohCity sourceCity, CityPlannerDataDto cityPlannerData, ZenmarCatalog catalog,
        AutoLayoutSettings settings, ZenmarLayoutResult result, string name)
    {
        var era = catalog.Eras.First(e => e.Id == settings.EraId);
        var entities = new List<HohCityMapEntity>();
        var nextId = 1;
        foreach (var placed in result.Buildings)
        {
            var source = placed.Id == ZenmarCatalogFactory.TOWER_ID
                ? era.SourceBuildings.GetValueOrDefault($"{placed.Id}:{settings.TowerLevel}") ??
                era.SourceBuildings.GetValueOrDefault(placed.Id)
                : era.SourceBuildings.GetValueOrDefault(placed.Id);
            if (source == null)
            {
                continue;
            }

            var kind = era.Buildings.FirstOrDefault(b => b.Id == placed.Id)?.Kind;
            entities.Add(new HohCityMapEntity
            {
                Id = nextId++,
                CityEntityId = source.Id,
                Level = placed.Id == ZenmarCatalogFactory.FOUNTAIN_ID ? settings.FountainLevel : source.Level,
                X = settings.MapOriginX + placed.X,
                Y = settings.MapOriginY + placed.Y,
                IsRotated = source.Width != source.Length && placed.W != source.Width,
                SelectedProductId = kind != null && ProductionKinds.Contains(kind) ? GetFirstProductId(source) : null,
            });
        }

        // Buildings the engine does not place stay available in the inventory
        var buildings = cityPlannerData.Buildings.ToDictionary(b => b.Id);
        var inventory = sourceCity.InventoryBuildings.Select(e => e.Clone()).ToList();
        foreach (var entity in sourceCity.Entities)
        {
            if (!buildings.TryGetValue(entity.CityEntityId, out var building))
            {
                continue;
            }

            if (ZenmarCatalogFactory.BuildingIds.TryGetValue(building.Group, out var engineId) &&
                result.Buildings.Any(b => b.Id == engineId))
            {
                continue;
            }

            if (building.Type is BuildingType.Farm or BuildingType.Home or BuildingType.Workshop
                or BuildingType.CultureSite or BuildingType.Barracks)
            {
                continue;
            }

            var clone = entity.Clone();
            clone.Id = nextId++;
            inventory.Add(clone);
        }

        var city = hohCityFactory.Create(Guid.NewGuid().ToString(), sourceCity.InGameCityId, settings.AgeId, name,
            entities, sourceCity.UnlockedExpansions.ToHashSet(), sourceCity.UnlockedPremiumExpansions,
            FogConstants.CITY_PLANNER_VERSION, sourceCity.WonderId, sourceCity.WonderLevel);
        city.InventoryBuildings = inventory;
        return city;
    }

    private static string? GetFirstProductId(BuildingDto building)
    {
        return building.Components.OfType<ProductionComponent>().FirstOrDefault()?.Id;
    }
}
