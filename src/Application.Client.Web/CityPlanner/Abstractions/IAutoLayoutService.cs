using Ingweland.Fog.Application.Client.Web.CityPlanner.AutoLayout;
using Ingweland.Fog.Dtos.Hoh.CityPlanner;
using Ingweland.Fog.Models.Fog.Entities;

namespace Ingweland.Fog.Application.Client.Web.CityPlanner.Abstractions;

public interface IAutoLayoutService
{
    AutoLayoutSettings CreateSettings(HohCity city, CityPlannerDataDto cityPlannerData, ZenmarCatalog catalog);
    IReadOnlyDictionary<string, object?> CreateEngineInput(AutoLayoutSettings settings, ZenmarCatalog catalog);

    HohCity CreateCity(HohCity sourceCity, CityPlannerDataDto cityPlannerData, ZenmarCatalog catalog,
        AutoLayoutSettings settings, ZenmarLayoutResult result, string name);
}
