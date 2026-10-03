using Ingweland.Fog.Application.Client.Web.Models;
using Ingweland.Fog.Application.Client.Web.Services.Abstractions;
using Ingweland.Fog.Application.Core.Helpers;
using Ingweland.Fog.Models.Fog.Entities;
using Ingweland.Fog.Models.Hoh.Enums;
using Ingweland.Fog.WebApp.Client.Components.Pages.Abstractions;
using Microsoft.AspNetCore.Components;

namespace Ingweland.Fog.WebApp.Client.Components.Pages;

public partial class CityPlannerAutoLayoutPage : FogPageBase
{
    private IReadOnlyCollection<HohCityBasicData> _cities = [];
    private bool _isLoading = true;
    private bool _isRunning;
    private string? _selectedCityId;
    private HohCity? _sourceCity;

    [Inject]
    private CityPlannerNavigationState CityPlannerNavigationState { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; }

    [Inject]
    private IPersistenceService PersistenceService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (!OperatingSystem.IsBrowser())
        {
            return;
        }

        _cities = (await PersistenceService.GetCities()).Where(c => c.InGameCityId == CityId.Capital)
            .OrderByDescending(c => c.UpdatedAt)
            .ToList();
        if (_cities.Count > 0)
        {
            await OnCityChanged(_cities.First().Id);
        }

        _isLoading = false;
    }

    private async Task OnCityChanged(string cityId)
    {
        _selectedCityId = cityId;
        _sourceCity = await PersistenceService.LoadCity(cityId);
    }

    private void OpenCity(HohCity city)
    {
        CityPlannerNavigationState.Data = new CityPlannerNavigationState.CityPlannerNavigationStateData
        {
            City = city,
        };
        NavigationManager.NavigateTo(FogUrlBuilder.PageRoutes.CITY_PLANNER_APP_PATH);
    }
}
