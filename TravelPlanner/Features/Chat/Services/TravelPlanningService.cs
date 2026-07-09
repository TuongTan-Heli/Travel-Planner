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

        var clusters = await _mapService.GetLocations(session.Context.Destination);

        // Randomly select a subset of clusters based on the number of days
        var count = Math.Min(
        Math.Max(1, (int)Math.Ceiling(session.Context.Days ?? 1 / 3.5)),
        clusters.Count);
        clusters = clusters
            .OrderBy(_ => Random.Next())
            .Take(count)
            .ToList();

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