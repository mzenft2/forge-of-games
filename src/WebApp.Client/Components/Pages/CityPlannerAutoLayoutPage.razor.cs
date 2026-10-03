using Ingweland.Fog.Application.Client.Core.Localization;
using Ingweland.Fog.Application.Client.Web.CityPlanner.Abstractions;
using Ingweland.Fog.Application.Client.Web.CityPlanner.AutoLayout;
using Ingweland.Fog.Application.Client.Web.Models;
using Ingweland.Fog.Application.Client.Web.Services.Abstractions;
using Ingweland.Fog.Application.Core.CityPlanner.Abstractions;
using Ingweland.Fog.Application.Core.Helpers;
using Ingweland.Fog.Dtos.Hoh.CityPlanner;
using Ingweland.Fog.Models.Fog.Entities;
using Ingweland.Fog.Models.Hoh.Enums;
using Ingweland.Fog.WebApp.Client.Components.Pages.Abstractions;
using Microsoft.AspNetCore.Components;
using Ingweland.Fog.WebApp.Client.Components.Elements.CityPlanner;
using Microsoft.JSInterop;
using MudBlazor;

namespace Ingweland.Fog.WebApp.Client.Components.Pages;

public partial class CityPlannerAutoLayoutPage : FogPageBase
{
    /// <summary>
    ///     Shows the optional Ko-fi support window of the auto layout author when a search starts.
    ///     Set to false to switch it off; the window never blocks the calculation.
    /// </summary>
    private const bool SHOW_SUPPORT_DIALOG = true;

    private static readonly string[] OptionalBuildingIds =
    [
        "collectableSchoolV2", "collectableArchitectsStudioV2", "heroAcademy", ZenmarCatalogFactory.TOWER_ID,
    ];

    private ZenmarCatalog? _catalog;
    private CityPlannerDataDto? _cityPlannerData;
    private IReadOnlyCollection<HohCityBasicData> _cities = [];
    private DotNetObjectReference<CityPlannerAutoLayoutPage>? _dotNetRef;
    private bool _isLoading = true;
    private bool _isRunning;
    private double _progress;
    private ZenmarLayoutResult? _result;
    private string? _selectedCityId;
    private AutoLayoutSettings? _settings;
    private HohCity? _sourceCity;
    private string? _statusMessage;

    [Inject]
    private IAutoLayoutService AutoLayoutService { get; set; }

    [Inject]
    private ICityPlannerDataService CityPlannerDataService { get; set; }

    [Inject]
    private CityPlannerNavigationState CityPlannerNavigationState { get; set; }

    [Inject]
    private IDialogService DialogService { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; }

    [Inject]
    private IPersistenceService PersistenceService { get; set; }

    [Inject]
    private IZenmarCatalogFactory CatalogFactory { get; set; }

    private int FreeTiles => _settings == null || _result == null
        ? 0
        : _settings.Tiles.Count(t => t) * _settings.ExpansionSize * _settings.ExpansionSize - _result.UsedTiles;

    private IEnumerable<(string Id, string Label)> OptionalBuildings =>
        OptionalBuildingIds.Where(id => SelectedEra.Buildings.Any(b => b.Id == id))
            .Select(id => (id, GetBuildingName(id)));

    private ZenmarEra SelectedEra => _catalog!.Eras.First(e => e.Id == _settings!.EraId);

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
            _cityPlannerData = await CityPlannerDataService.GetCityPlannerDataAsync(CityId.Capital);
            _catalog = CatalogFactory.Create(_cityPlannerData);
            await OnCityChanged(_cities.First().Id);
        }

        _isLoading = false;
    }

    [JSInvokable]
    public Task OnAutoLayoutFailed(string message)
    {
        Console.Error.WriteLine($"Auto layout failed: {message}");
        _isRunning = false;
        _statusMessage = _result == null
            ? Loc[FogResource.CityPlanner_AutoLayout_Status_NotFound]
            : Loc[FogResource.CityPlanner_AutoLayout_Status_BrowserError];
        return InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public Task OnAutoLayoutFinished(ZenmarLayoutResult? best)
    {
        _isRunning = false;
        _progress = 1;
        if (best != null)
        {
            _result = best;
        }

        _statusMessage = _result == null
            ? Loc[FogResource.CityPlanner_AutoLayout_Status_NotFound]
            : Loc[FogResource.CityPlanner_AutoLayout_Status_Finished];
        return InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public Task OnAutoLayoutProgress(double done, double total, ZenmarLayoutResult? best)
    {
        _progress = total > 0 ? Math.Clamp(done / total, 0, 1) : 0;
        if (best != null)
        {
            _result = best;
        }

        return InvokeAsync(StateHasChanged);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_isRunning)
            {
                _ = JsRuntime.InvokeVoidAsync("Fog.Webapp.AutoLayout.cancel").AsTask();
            }

            _dotNetRef?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task CancelAsync()
    {
        await JsRuntime.InvokeVoidAsync("Fog.Webapp.AutoLayout.cancel");
        _isRunning = false;
    }

    private string FormatSeconds(int seconds)
    {
        return $"{seconds / 60.0:0.#} {Loc[FogResource.Common_Minutes_Abbr]}";
    }

    private string GetBuildingName(string engineId)
    {
        return SelectedEra.SourceBuildings.TryGetValue(engineId, out var building) ? building.Name : engineId;
    }

    private bool GetOptional(string id)
    {
        return id switch
        {
            "collectableSchoolV2" => _settings!.School,
            "collectableArchitectsStudioV2" => _settings!.ArchitectsStudio,
            "heroAcademy" => _settings!.HeroAcademy,
            ZenmarCatalogFactory.TOWER_ID => _settings!.Tower,
            _ => false,
        };
    }

    private async Task OnCityChanged(string cityId)
    {
        _selectedCityId = cityId;
        _result = null;
        _statusMessage = null;
        _sourceCity = await PersistenceService.LoadCity(cityId);
        _settings = _sourceCity != null && _cityPlannerData != null && _catalog != null
            ? AutoLayoutService.CreateSettings(_sourceCity, _cityPlannerData, _catalog)
            : null;
    }

    private void OnEraChanged(string eraId)
    {
        if (_settings == null || _sourceCity == null || _cityPlannerData == null || _catalog == null)
        {
            return;
        }

        var era = _catalog.Eras.First(e => e.Id == eraId);
        _settings.EraId = era.Id;
        _settings.AgeId = era.SourceAge;
        _settings.WorkshopCounts.Clear();
        foreach (var workshopId in era.Current)
        {
            _settings.WorkshopCounts[workshopId] = 2;
        }

        if (era.OldWorkshop == null)
        {
            _settings.OldCount = 0;
        }

        _result = null;
    }

    private async Task RunAsync()
    {
        if (_settings == null || _catalog == null)
        {
            return;
        }

        _result = null;
        _statusMessage = null;
        _progress = 0;
        _isRunning = true;
        _dotNetRef ??= DotNetObjectReference.Create(this);
        var input = AutoLayoutService.CreateEngineInput(_settings, _catalog);
        await JsRuntime.InvokeVoidAsync("Fog.Webapp.AutoLayout.start", _dotNetRef, input);
        if (SHOW_SUPPORT_DIALOG)
        {
            await DialogService.ShowAsync<AutoLayoutSupportDialog>(null, new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true,
                BackdropClick = true,
            });
        }
    }

    private async Task SaveAndOpenAsync()
    {
        if (_settings == null || _catalog == null || _cityPlannerData == null || _sourceCity == null ||
            _result == null)
        {
            return;
        }

        var name = string.Format(Loc[FogResource.CityPlanner_AutoLayout_NewCityName], _sourceCity.Name);
        var city = AutoLayoutService.CreateCity(_sourceCity, _cityPlannerData, _catalog, _settings, _result, name);
        await PersistenceService.SaveCity(city);

        CityPlannerNavigationState.Data = new CityPlannerNavigationState.CityPlannerNavigationStateData
        {
            City = city,
        };
        NavigationManager.NavigateTo(FogUrlBuilder.PageRoutes.CITY_PLANNER_APP_PATH);
    }

    private void SetOptional(string id, bool value)
    {
        switch (id)
        {
            case "collectableSchoolV2":
                _settings!.School = value;
                break;
            case "collectableArchitectsStudioV2":
                _settings!.ArchitectsStudio = value;
                break;
            case "heroAcademy":
                _settings!.HeroAcademy = value;
                break;
            case ZenmarCatalogFactory.TOWER_ID:
                _settings!.Tower = value;
                break;
        }
    }

    private async Task StopAsync()
    {
        await JsRuntime.InvokeVoidAsync("Fog.Webapp.AutoLayout.stop");
    }
}
