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
        if (session.Context.Destination == null || session.Context.Days == null)
        {
            throw new AppException("INSUFFICIENT_DATA", "Destination and days are required for planning.");
        }
        Task<TravelTime> weatherTask;

        var clusters = await _mapService.GetLocations(session.Context.Destination, session.Context.Days ?? 1);

        if (session.Context.StartDate.HasValue && 
            session.Context.EndDate.HasValue)
        {
            weatherTask = _weatherService.GetWeatherAsync(
                clusters,
                session.Context);
        }
        else
        {
            weatherTask = _weatherService.GetRecommendedTimeAsync(
                clusters,
                session.Context);
        }

        var placesTask = _mapService.GetMapDataAsync(session.Context);

        // var (lat, lon) = await _utils.GetCoordinatesAsync(
        //                     session.Context.Destination ?? "");

        await Task.WhenAll(weatherTask, placesTask); //wrong year in weather task

        session.Stage = TravelStage.Scoring;
        return new TripPlanningData
        {
            TravelTime = weatherTask.Result,
            RecommendedPlaces = placesTask.Result,
            // Altitude = new Altitude
            // {
            //     Latitude = lat,
            //     Longitude = lon
            // }
        };
    }
}