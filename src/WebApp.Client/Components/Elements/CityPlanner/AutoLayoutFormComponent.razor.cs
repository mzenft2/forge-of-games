using Ingweland.Fog.Application.Client.Core.Localization;
using Ingweland.Fog.Application.Client.Web.CityPlanner.Abstractions;
using Ingweland.Fog.Application.Client.Web.CityPlanner.AutoLayout;
using Ingweland.Fog.Application.Client.Web.Services.Abstractions;
using Ingweland.Fog.Application.Core.CityPlanner.Abstractions;
using Ingweland.Fog.Dtos.Hoh.CityPlanner;
using Ingweland.Fog.Models.Fog.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;

namespace Ingweland.Fog.WebApp.Client.Components.Elements.CityPlanner;

/// <summary>
///     Auto layout settings, search and result for one capital city. The settings start from
///     <see cref="SourceCity" />; the source city is never changed, the result is saved as a new city.
/// </summary>
public partial class AutoLayoutFormComponent : ComponentBase, IDisposable
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
    private DotNetObjectReference<AutoLayoutFormComponent>? _dotNetRef;
    private bool _isLoading;
    private bool _isRunning;
    private double _progress;
    private ZenmarLayoutResult? _result;
    private AutoLayoutSettings? _settings;
    private HohCity? _settingsCity;
    private string? _statusMessage;

    [Inject]
    private IAutoLayoutService AutoLayoutService { get; set; }

    [Inject]
    private IZenmarCatalogFactory CatalogFactory { get; set; }

    [Inject]
    private ICityPlannerDataService CityPlannerDataService { get; set; }

    [Inject]
    private IDialogService DialogService { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Inject]
    private IStringLocalizer<FogResource> Loc { get; set; }

    /// <summary>
    ///     Called with the new city after it has been saved.
    /// </summary>
    [Parameter]
    public EventCallback<HohCity> OnCityCreated { get; set; }

    /// <summary>
    ///     Called when a search starts or ends.
    /// </summary>
    [Parameter]
    public EventCallback<bool> RunningChanged { get; set; }

    [Inject]
    private IPersistenceService PersistenceService { get; set; }

    /// <summary>
    ///     Capital city the settings are taken from.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public HohCity? SourceCity { get; set; }

    private int FreeTiles => _settings == null || _result == null
        ? 0
        : _settings.Tiles.Count(t => t) * _settings.ExpansionSize * _settings.ExpansionSize - _result.UsedTiles;

    private IEnumerable<(string Id, string Label)> OptionalBuildings =>
        OptionalBuildingIds.Where(id => SelectedEra.Buildings.Any(b => b.Id == id))
            .Select(id => (id, GetBuildingName(id)));

    private ZenmarEra SelectedEra => _catalog!.Eras.First(e => e.Id == _settings!.EraId);

    public void Dispose()
    {
        if (_isRunning)
        {
            _ = JsRuntime.InvokeVoidAsync("Fog.Webapp.AutoLayout.cancel").AsTask();
        }

        _dotNetRef?.Dispose();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!OperatingSystem.IsBrowser() || ReferenceEquals(SourceCity, _settingsCity))
        {
            return;
        }

        _settingsCity = SourceCity;
        _result = null;
        _statusMessage = null;
        _settings = null;
        if (SourceCity == null)
        {
            return;
        }

        if (_catalog == null)
        {
            _isLoading = true;
            _cityPlannerData = await CityPlannerDataService.GetCityPlannerDataAsync(SourceCity.InGameCityId);
            _catalog = CatalogFactory.Create(_cityPlannerData);
            _isLoading = false;
        }

        _settings = AutoLayoutService.CreateSettings(SourceCity, _cityPlannerData!, _catalog);
    }

    [JSInvokable]
    public async Task OnAutoLayoutFailed(string message)
    {
        Console.Error.WriteLine($"Auto layout failed: {message}");
        _statusMessage = _result == null
            ? Loc[FogResource.CityPlanner_AutoLayout_Status_NotFound]
            : Loc[FogResource.CityPlanner_AutoLayout_Status_BrowserError];
        await InvokeAsync(() => SetRunningAsync(false));
    }

    [JSInvokable]
    public async Task OnAutoLayoutFinished(ZenmarLayoutResult? best)
    {
        _progress = 1;
        if (best != null)
        {
            _result = best;
        }

        _statusMessage = _result == null
            ? Loc[FogResource.CityPlanner_AutoLayout_Status_NotFound]
            : Loc[FogResource.CityPlanner_AutoLayout_Status_Finished];
        await InvokeAsync(() => SetRunningAsync(false));
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

    private async Task CancelAsync()
    {
        await JsRuntime.InvokeVoidAsync("Fog.Webapp.AutoLayout.cancel");
        await SetRunningAsync(false);
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

    private void OnEraChanged(string eraId)
    {
        if (_settings == null || _catalog == null)
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
        await SetRunningAsync(true);
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
        if (_settings == null || _catalog == null || _cityPlannerData == null || SourceCity == null ||
            _result == null)
        {
            return;
        }

        var name = string.Format(Loc[FogResource.CityPlanner_AutoLayout_NewCityName], SourceCity.Name);
        var city = AutoLayoutService.CreateCity(SourceCity, _cityPlannerData, _catalog, _settings, _result, name);
        await PersistenceService.SaveCity(city);
        await OnCityCreated.InvokeAsync(city);
    }

    private async Task SetRunningAsync(bool isRunning)
    {
        _isRunning = isRunning;
        await RunningChanged.InvokeAsync(isRunning);
        StateHasChanged();
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
