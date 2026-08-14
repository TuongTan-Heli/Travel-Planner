using TravelPlanner.Features.Map;
using TravelPlanner.Features.Weather.Services.WeatherService;

namespace TravelPlanner;

public sealed class TravelPlanningService
{
    private readonly WeatherService _weatherService;
    private readonly MapService _mapService;
    private readonly Utils _utils;
    private static readonly Random Random = new();

    public TravelPlanningService(WeatherService weatherService, MapService mapService, Utils utils)
    {
        _weatherService = weatherService;
        _mapService = mapService;
        _utils = utils;
    }

    public async Task<TripPlanningData> BuildPlanningDataAsync(TravelSession session)
    {
        if (session.Context.Destination == null || session.Context.Country == null ||
            (
                session.Context.Days == null &&
                (
                    session.Context.StartDate == null ||
                    session.Context.EndDate == null
                )
            ))
        {
            throw new AppException("INSUFFICIENT_DATA", "Destination, days and country are required for planning.", "Destination, days and country are required for planning.");
        }
        Task<TravelTime> weatherTask;

        var clusters = await _mapService.GetLocations(session);

        if (session.Context.StartDate.HasValue &&
            session.Context.EndDate.HasValue)
        {
            weatherTask = _weatherService.GetWeatherAsync(clusters, session.Context);
        }
        else
        {
            weatherTask = _weatherService.GetRecommendedTimeAsync(clusters, session.Context);
        }

        var placesTask = _mapService.GetMapDataAsync(clusters, session);

        await Task.WhenAll(weatherTask, placesTask);

        return new TripPlanningData
        {
            TravelTime = weatherTask.Result,
            RecommendedPlaces = placesTask.Result
        };
    }
}