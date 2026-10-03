using Ingweland.Fog.Application.Client.Web.CityPlanner.AutoLayout;
using Ingweland.Fog.Dtos.Hoh.CityPlanner;

namespace Ingweland.Fog.Application.Client.Web.CityPlanner.Abstractions;

public interface IZenmarCatalogFactory
{
    ZenmarCatalog Create(CityPlannerDataDto cityPlannerData);
}
