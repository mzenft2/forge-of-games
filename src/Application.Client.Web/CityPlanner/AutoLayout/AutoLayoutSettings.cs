namespace Ingweland.Fog.Application.Client.Web.CityPlanner.AutoLayout;

public class AutoLayoutSettings
{
    public const int GRID_COLUMNS = 10;
    public const int GRID_ROWS = 8;

    public static readonly IReadOnlyCollection<int> SearchSecondsOptions = [30, 60, 120, 300, 600];

    public required string AgeId { get; set; }
    public bool ArchitectsStudio { get; set; }
    public bool CompareSparks { get; set; } = true;
    public int DomesticFarms { get; set; }
    public required string EraId { get; set; }

    /// <summary>
    ///     Expansion ids per engine grid cell (row * 10 + column).
    /// </summary>
    public string?[] ExpansionIds { get; init; } = new string?[GRID_COLUMNS * GRID_ROWS];

    public int ExpansionSize { get; init; }
    public bool Fountain { get; set; }
    public int FountainLevel { get; set; } = 1;
    public bool FullArmy { get; set; } = true;
    public bool HeroAcademy { get; set; }
    public double Interval { get; set; } = 3;

    /// <summary>
    ///     City planner coordinates of the top left corner of the engine grid.
    /// </summary>
    public int MapOriginX { get; init; }

    public int MapOriginY { get; init; }
    public double Night { get; set; } = 8;
    public int OldCount { get; set; }
    public int PremiumCulture { get; set; }
    public int PremiumFarm { get; set; }
    public int PremiumHome { get; set; }
    public int RuralFarms { get; set; }
    public bool School { get; set; }
    public int SearchSeconds { get; set; } = 60;
    public int SparkCap { get; set; } = 7000;
    public bool Subscription { get; set; }

    /// <summary>
    ///     Open land per engine grid cell (row * 10 + column).
    /// </summary>
    public bool[] Tiles { get; init; } = new bool[GRID_COLUMNS * GRID_ROWS];

    public bool Tower { get; set; }
    public int TowerLevel { get; set; } = 1;

    /// <summary>
    ///     Current age workshops: engine id -> count.
    /// </summary>
    public Dictionary<string, int> WorkshopCounts { get; init; } = new();
}
