using Ingweland.Fog.Application.Client.Web.StatsHub.ViewModels;
using Ingweland.Fog.Dtos.Hoh;
using Ingweland.Fog.Dtos.Hoh.Stats;

namespace Ingweland.Fog.Application.Client.Web.StatsHub.Abstractions;

public interface IAllianceProductionViewModelFactory
{
    AllianceProductionViewModel Create(AllianceProductionDto dto, IReadOnlyDictionary<string, AgeDto> ages);
}
