namespace TravelPlanner.Features.Weather.Services.WeatherService;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly Utils _utils;
    public WeatherService(HttpClient httpClient, Utils utils)
    {
        _httpClient = httpClient;
        _utils = utils;
    }
    public async Task<TravelTime> GetRecommendedTimeAsync(
    string location,
    int? days)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddYears(-3);

        var (lat, lon) =
            await _utils.GetCoordinatesAsync(location);

        var url =
            $"https://archive-api.open-meteo.com/v1/archive" +
            $"?latitude={lat}" +
            $"&longitude={lon}" +
            $"&start_date={startDate:yyyy-MM-dd}" +
            $"&end_date={endDate:yyyy-MM-dd}" +
            $"&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum" +
            $"&timezone=auto";

        var response =
            await _httpClient.GetFromJsonAsync<HistoricalWeatherResponse>(url);

        if (response == null)
        {
            throw new AppException(
                "WEATHER_API_ERROR",
                "Failed to retrieve historical weather.");
        }

        var weatherDays =
            BuildHistoricalDays(response);

        return FindBestTravelWindow(
            location,
            weatherDays,
            days ?? 7);
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

        if (endDate > new DateTime().AddDays(15))
        {
            endDate = new DateTime().AddDays(15);
        }

        var (lat, lon) =
            await _utils.GetCoordinatesAsync(location);

        var url =
            $"https://api.open-meteo.com/v1/forecast" +
            $"?latitude={lat}" +
            $"&longitude={lon}" +
            $"&hourly=precipitation_probability" +
            $"&daily=weather_code,temperature_2m_min,temperature_2m_max,uv_index_max,precipitation_probability_max" +
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

    private TravelTime FindBestTravelWindow(
    string location,
    List<WeatherDay> historicalDays,
    int days)
    {
        var weeklyClimate =
        historicalDays
        .GroupBy(x => new
        {
            x.Date.Year,
            Week = GetWeekOfYear(x.Date)
        })
        .Select(g => new
        {
            AvgMaxTemp = g.Average(x => x.MaxTemp),
            AvgMinTemp = g.Average(x => x.MinTemp),
            AvgRainfall = g.Average(x => x.Rainfall),
            g.Key.Year,
            g.Key.Week,
            AvgScore = g.Average(x => x.Score),
            RepresentativeDate = g.First().Date
        })
        .OrderByDescending(x => x.AvgScore)
        .ToList();

        var best = weeklyClimate.First();

        var nextYear = DateTime.UtcNow.Year + 1;

        var today = DateTime.UtcNow.Date;

        var (start, end) = _utils.GetNextBestTravelWindow(
        best.Week,
        days,
        DateTime.UtcNow);

        return new TravelTime
        {
            Location = location,
            StartTime = start,
            EndTime = end,
            WeatherForecasts =
            [
                new WeatherDay
            {
                Date = start,
                MaxTemp = best.AvgMaxTemp,
                MinTemp = best.AvgMinTemp,
                Rainfall = best.AvgRainfall,
                WeatherCode = 0,
                Score = (int)Math.Round(best.AvgScore)
            }
            ]
        };
    }
    private static List<WeatherDay> BuildForecasts(ForecastResponse forecast)
    {
        var result = new List<WeatherDay>();

        for (var i = 0; i < forecast.Daily.Time.Count; i++)
        {
            var avgTemp =
                (forecast.Daily.Temperature_2m_Max[i] +
                 forecast.Daily.Temperature_2m_Min[i]) / 2;

            var rainfall =
                forecast.Daily.Precipitation_Probability_Max[i];

            result.Add(new WeatherDay
            {
                Date = forecast.Daily.Time[i],
                MaxTemp = forecast.Daily.Temperature_2m_Max[i],
                MinTemp = forecast.Daily.Temperature_2m_Min[i],
                Rainfall = rainfall,
                WeatherCode = forecast.Daily.Weather_Code[i],
                Score = CalculateScore(avgTemp, rainfall, forecast.Daily.Weather_Code[i])
            });
        }

        return result;
    }

    private static List<WeatherDay> BuildHistoricalDays(HistoricalWeatherResponse response)
    {
        var result = new List<WeatherDay>();

        for (var i = 0; i < response.Daily.Time.Count; i++)
        {
            var avgTemp =
                (response.Daily.Temperature_2m_Max[i] +
                 response.Daily.Temperature_2m_Min[i]) / 2;

            var rainfall = response.Daily.Precipitation_Sum[i];

            result.Add(new WeatherDay
            {
                Date = response.Daily.Time[i],
                MaxTemp = response.Daily.Temperature_2m_Max[i],
                MinTemp = response.Daily.Temperature_2m_Min[i],
                Rainfall = rainfall,
                WeatherCode = response.Daily.Weather_Code[i],
                Score = CalculateScore(avgTemp, rainfall, response.Daily.Weather_Code[i])
            });
        }

        return result;
    }

    private static int GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        return culture.Calendar.GetWeekOfYear(
            date,
            System.Globalization.CalendarWeekRule.FirstDay,
            DayOfWeek.Monday);
    }

    private static int CalculateScore(
    double avgTemp,
    double rainfall,
    int weatherCode)
    {
        var score = 100;

        if (avgTemp < 15)
            score -= 20;

        if (avgTemp > 30)
            score -= 20;

        score -= (int)Math.Min(rainfall * 2, 40);

        score -= weatherCode switch
        {
            >= 95 => 40,
            >= 80 => 25,
            >= 70 => 15,
            >= 50 => 10,
            _ => 0
        };

        return Math.Max(score, 0);
    }
}