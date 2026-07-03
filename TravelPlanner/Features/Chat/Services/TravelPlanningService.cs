using TravelPlanner.Features.Map;
using TravelPlanner.Features.Weather.Services.WeatherService;

namespace TravelPlanner;

public sealed class TravelPlanningService
{
    private readonly WeatherService _weatherService;
    private readonly MapService _mapService;
    private readonly Utils _utils;

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
        if (session.Context.StartDate.HasValue && session.Context.EndDate.HasValue
        && session.Context.StartDate <= new DateTime().AddDays(15))
        {
            weatherTask = _weatherService.GetWeatherAsync(
                session.Context.Destination,
                session.Context.StartDate,
                session.Context.EndDate);
        }
        else
        {
            weatherTask = _weatherService.GetRecommendedTimeAsync(
                session.Context.Destination,
                session.Context.Days);
        }

        var placesTask = _mapService.GetMapDataAsync(session.Context);

        var (lat, lon) = await _utils.GetCoordinatesAsync(
                            session.Context.Destination ?? "");

        await Task.WhenAll(weatherTask, placesTask);

        session.Stage = TravelStage.Scoring;
        return new TripPlanningData
        {
            TravelTime = weatherTask.Result,
            RecommendedPlaces = placesTask.Result,
            Altitude = new Altitude
            {
                Latitude = lat,
                Longitude = lon
            }
        };
    }
}