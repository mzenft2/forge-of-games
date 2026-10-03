namespace Ingweland.Fog.Dtos.Hoh.Stats;

public class AllianceProductionDto
{
    public IReadOnlyCollection<AllianceProductionAgeDto> Ages { get; init; } = [];
    public int MemberCount { get; init; }
    public IReadOnlyCollection<AllianceProductionMissingMemberDto> MembersWithoutCity { get; init; } = [];
}
