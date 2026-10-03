namespace Ingweland.Fog.Dtos.Hoh.Stats;

public class AllianceProductionDto
{
    public IReadOnlyCollection<AllianceProductionAgeDto> Ages { get; init; } = [];
    public double CoinsPerGood { get; init; }
    public int DailyGoodsFurnaceLimit { get; init; }
    public int ExpansionArea { get; init; }
    public int FreePremiumExpansions { get; init; }
    public int MemberCount { get; init; }
    public IReadOnlyCollection<AllianceProductionMissingMemberDto> MembersWithoutCity { get; init; } = [];
}
