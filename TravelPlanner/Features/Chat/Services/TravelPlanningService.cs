public sealed class TravelPlanningService
{
    private readonly WeatherService _weatherService;
    private readonly MapService _mapService;

    public TravelPlanningService(WeatherService weatherService, MapService mapService)
    {
        _weatherService = weatherService;
        _mapService = mapService;
    }

    public async Task<TripPlanningData> BuildPlanningDataAsync(TravelSession session)
    {
        if (session.Context.Destination == null || session.Context.Days == null)
        {
            throw new AppException("INSUFFICIENT_DATA", "Destination and days are required for planning.");
        }
        Task<TravelTime> weatherTask;
        if (session.Context.StartDate.HasValue && session.Context.EndDate.HasValue)
        {
            weatherTask = _weatherService.GetWeatherAsync(
                session.Context.Destination,
                session.Context.StartDate,
                session.Context.EndDate);
        }
        else
        {
            weatherTask = _weatherService.GetRecomendedTimeAsync(
                session.Context.Destination,
                session.Context.Days);
        }

        var placesTask = _mapService.GetMapDataAsync(session.Context.Destination!);

        await Task.WhenAll(weatherTask, placesTask);

        return new TripPlanningData
        {
            TravelTime = weatherTask.Result,
            RecommendedPlaces = placesTask.Result
        };
    }
    
}