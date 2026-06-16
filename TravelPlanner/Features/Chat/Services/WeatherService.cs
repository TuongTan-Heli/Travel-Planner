public class WeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<TravelTime> GetRecomendedTimeAsync(string location, int? days)
    {
        return new TravelTime();
    }

    public async Task<TravelTime> GetWeatherAsync(
    string location,
    DateTime? startDate,
    DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
        {
            throw new AppException(
                "INVALID_DATE_RANGE",
                "StartDate and EndDate are required.");
        }

        var (lat, lon) =
            await GetCoordinatesAsync(location);

        var url =
            $"https://api.open-meteo.com/v1/forecast" +
            $"?latitude={lat}" +
            $"&longitude={lon}" +
            $"&hourly=temperature_2m,precipitation_probability" +
            $"&start_date={startDate.Value:yyyy-MM-dd}" +
            $"&end_date={endDate.Value:yyyy-MM-dd}";

        var forecast =
            await _httpClient.GetFromJsonAsync<ForecastResponse>(url);

        if (forecast == null)
        {
            throw new AppException(
                "WEATHER_API_ERROR",
                "Failed to retrieve forecast.");
        }

        return new TravelTime
        {
            Location = location,
            StartTime = startDate.Value,
            EndTime = endDate.Value,
            WeatherForecasts = BuildForecasts(forecast)
        };
    }

    private async Task<(double lat, double lon)> GetCoordinatesAsync(
    string location)
    {
        var url =
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}";

        var response =
            await _httpClient.GetFromJsonAsync<Location>(url);

        var first = response?.Results?.FirstOrDefault();

        if (first == null)
        {
            throw new AppException(
                "LOCATION_NOT_FOUND",
                $"Could not find coordinates for {location}");
        }

        return (first.Latitude, first.Longitude);
    }

    private static List<WeatherForecast> BuildForecasts(
    ForecastResponse forecast)
    {
        var result = new List<WeatherForecast>();

        for (var i = 0; i < forecast.Hourly.Time.Count; i++)
        {
            var rain =
                forecast.Hourly.Precipitation_Probability[i];

            var temp =
                (int)Math.Round(
                    forecast.Hourly.Temperature_2m[i]);

            result.Add(new WeatherForecast
            {
                Name = forecast.Hourly.Time[i].ToString("g"),

                Time = forecast.Hourly.Time[i],

                TemperatureC = temp,

                rainChance = rain,

                Weather = rain > 60
                    ? "Rainy"
                    : rain > 30
                        ? "Cloudy"
                        : "Sunny",

                Rating = CalculateRating(temp, rain)
            });
        }

        return result;
    }
    private static int CalculateRating(
        int temperature,
        int rainChance)
    {
        var score = 100;

        score -= rainChance;

        if (temperature < 10)
            score -= 25;

        if (temperature > 32)
            score -= 20;

        return Math.Max(score, 0);
    }
}