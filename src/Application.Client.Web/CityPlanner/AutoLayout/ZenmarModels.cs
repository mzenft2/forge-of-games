using System.Text.Json.Serialization;
using Ingweland.Fog.Dtos.Hoh.City;

namespace Ingweland.Fog.Application.Client.Web.CityPlanner.AutoLayout;

// Data contracts of the Zenmar Strategy engine (wwwroot/scripts/zenmar/zenmar-engine.js).
// Property names are fixed by the engine, so they are set explicitly.

public class ZenmarCatalog
{
    [JsonPropertyName("eras")]
    public required IReadOnlyCollection<ZenmarEra> Eras { get; init; }

    [JsonPropertyName("fountainSourceId")]
    public string? FountainSourceId { get; init; }
}

public class ZenmarEra
{
    [JsonPropertyName("buildings")]
    public required IReadOnlyCollection<ZenmarBuilding> Buildings { get; init; }

    [JsonPropertyName("current")]
    public required IReadOnlyCollection<string> Current { get; init; }

    [JsonPropertyName("fountainLevels")]
    public required IReadOnlyCollection<ZenmarFountainLevel> FountainLevels { get; init; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("mandatory")]
    public required IReadOnlyCollection<string> Mandatory { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("oldWorkshop")]
    public string? OldWorkshop { get; init; }

    [JsonPropertyName("sourceAge")]
    public required string SourceAge { get; init; }

    /// <summary>
    ///     Engine building id -> Forge building, used to turn the engine result back into a city.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, BuildingDto> SourceBuildings { get; init; } =
        new Dictionary<string, BuildingDto>();
}

public class ZenmarBuilding
{
    [JsonPropertyName("age")]
    public string? Age { get; init; }

    [JsonPropertyName("factor")]
    public double Factor { get; init; }

    [JsonPropertyName("factors")]
    public IReadOnlyDictionary<string, double> Factors { get; init; } = new Dictionary<string, double>();

    [JsonPropertyName("h")]
    public int H { get; init; }

    [JsonPropertyName("happyMax")]
    public int HappyMax { get; init; }

    [JsonPropertyName("hours")]
    public double Hours { get; init; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("level")]
    public int Level { get; init; }

    [JsonPropertyName("levels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<ZenmarBuildingLevel>? Levels { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("needs")]
    public int Needs { get; init; }

    [JsonPropertyName("oldWorkshop")]
    public bool OldWorkshop { get; set; }

    [JsonPropertyName("points")]
    public int Points { get; init; }

    [JsonPropertyName("premium")]
    public bool Premium { get; init; }

    [JsonPropertyName("range")]
    public int Range { get; init; }

    [JsonPropertyName("rewards")]
    public IReadOnlyCollection<ZenmarReward> Rewards { get; init; } = [];

    [JsonPropertyName("sourceId")]
    public required string SourceId { get; init; }

    [JsonPropertyName("w")]
    public int W { get; init; }

    [JsonPropertyName("workers")]
    public int Workers { get; init; }

    public ZenmarBuilding With(bool oldWorkshop)
    {
        var clone = (ZenmarBuilding) MemberwiseClone();
        clone.OldWorkshop = oldWorkshop;
        return clone;
    }
}

public record ZenmarBuildingLevel(
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("points")] int Points,
    [property: JsonPropertyName("range")] int Range,
    [property: JsonPropertyName("workers")] int Workers,
    [property: JsonPropertyName("sourceId")] string SourceId);

public record ZenmarReward(
    [property: JsonPropertyName("resource")] string Resource,
    [property: JsonPropertyName("amount")] int Amount);

public record ZenmarFountainLevel(
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("points")] int Points,
    [property: JsonPropertyName("range")] int Range);

/// <summary>
///     Trimmed engine result sent back by auto-layout-worker.js.
/// </summary>
public class ZenmarLayoutResult
{
    [JsonPropertyName("armyDeficit")]
    public bool ArmyDeficit { get; init; }

    [JsonPropertyName("buildings")]
    public IReadOnlyCollection<ZenmarPlacedBuilding> Buildings { get; init; } = [];

    [JsonPropertyName("isComplete")]
    public bool IsComplete { get; init; }

    [JsonPropertyName("spareWorkers")]
    public double SpareWorkers { get; init; }

    [JsonPropertyName("totalValue")]
    public double TotalValue { get; init; }

    [JsonPropertyName("usedTiles")]
    public int UsedTiles { get; init; }
}

public record ZenmarPlacedBuilding(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("w")] int W,
    [property: JsonPropertyName("h")] int H);
