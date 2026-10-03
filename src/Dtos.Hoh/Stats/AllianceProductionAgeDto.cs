namespace Ingweland.Fog.Dtos.Hoh.Stats;

public class AllianceProductionAgeDto
{
    public required string AgeId { get; init; }
    public double GoodsToFoodRate { get; init; }
    public double? MedianDailyValue { get; init; }
    public IReadOnlyCollection<AllianceMemberProductionDto> Members { get; init; } = [];
}
