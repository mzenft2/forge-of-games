using Ingweland.Fog.Application.Server.StatsHub.AllianceProduction;
using Ingweland.Fog.Dtos.Hoh.Stats;
using Ingweland.Fog.Models.Fog.Entities;

namespace Ingweland.Fog.Application.Server.StatsHub.Factories;

public interface IAllianceProductionDtoFactory
{
    AllianceMemberProductionDto CreateMember(Player player, PlayerCitySnapshot snapshot,
        AllianceMemberCityMeasures measures, AllianceProductionValues values, int? deviationFromMedianPercent);
}
