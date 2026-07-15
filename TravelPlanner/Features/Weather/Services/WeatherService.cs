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
    List<PlaceCluster> clusters,
    TravelPromptContext context)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddYears(-3);
        var tasks = clusters.Select(async cluster =>
        {
            var historical = await GetHistoricalWeatherAsync(cluster, startDate, endDate);

            return FindBestTravelWindow(cluster, BuildHistoricalDays(historical), context.Days ?? 1);
        });

        var bestTimes = await Task.WhenAll(tasks);
        return MergeTravelWindows(
            bestTimes,
            context);
    }

    public async Task<TravelTime> GetWeatherAsync(
    List<PlaceCluster> clusters,
    TravelPromptContext context)
    {
        var startDate = context.StartDate;
        var endDate = context.EndDate;
        if (!startDate.HasValue || !endDate.HasValue)
        {
            throw new AppException(
                "INVALID_DATE_RANGE",
                "StartDate and EndDate are required.");
        }

        var maxDate = DateTime.UtcNow.Date.AddDays(15);

        if (endDate > maxDate)
        {
            endDate = maxDate;
        }

        var weatherTasks = clusters.Select(cluster => GetForecastAsync(
        cluster,
        startDate.Value,
        endDate.Value));

        var responses = await Task.WhenAll(weatherTasks);

        var forecasts = responses
            .Where(x => x != null)
            .Select((response, index) => new LocationForecast
            {
                Location = clusters[index].Center,

                Days = BuildForecasts(response!)
            })
            .ToList();

        return new TravelTime
        {
            StartTime = startDate.Value,
            EndTime = endDate.Value,
            Forecasts = forecasts
        };
    }

    private TravelTime FindBestTravelWindow(
    PlaceCluster cluster,
    List<WeatherDay> historicalDays,
    int days)
    {
        var bestClimate = historicalDays
            .GroupBy(x =>
            (
                Month: x.Date.Month,
                Week: (x.Date.Day - 1) / 7 + 1
            ))
            .Select(g => new
            {
                g.Key.Month,
                g.Key.Week,

                AvgMaxTemp = g.Average(x => x.MaxTemp),
                AvgMinTemp = g.Average(x => x.MinTemp),
                AvgRainfall = g.Average(x => x.Rainfall),

                AvgScore = g.Average(x => x.Score)
            })
            .OrderByDescending(x => x.AvgScore)
            .First();


        var (start, end) =
            _utils.GetNextBestTravelWindow(
                bestClimate.Month,
                bestClimate.Week,
                days,
                DateTime.UtcNow);


        return new TravelTime
        {
            // Location = cluster,
            StartTime = start,
            EndTime = end,
            WeatherScore = bestClimate.AvgScore
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

    private async Task<HistoricalWeatherResponse> GetHistoricalWeatherAsync(
     PlaceCluster cluster,
     DateTime start,
     DateTime end)
    {
        var url =
            $"https://archive-api.open-meteo.com/v1/archive" +
            $"?latitude={cluster.Center.Latitude}" +
            $"&longitude={cluster.Center.Longitude}" +
            $"&start_date={start:yyyy-MM-dd}" +
            $"&end_date={end:yyyy-MM-dd}" +
            $"&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum" +
            $"&timezone=auto";

        return await _httpClient.GetFromJsonAsync<HistoricalWeatherResponse>(url)
            ?? throw new AppException(
                "WEATHER_API_ERROR",
                $"Failed to retrieve historical weather for the following location, long: {cluster.Center.Longitude} , lat: {cluster.Center.Latitude}");
    }
    private bool CanForecast(DateTime start, DateTime end)
    {
        var today = DateTime.UtcNow.Date;

        return start >= today &&
               end <= today.AddDays(15);
    }
    private async Task<ForecastResponse?> GetForecastAsync(
    PlaceCluster cluster,
    DateTime start,
    DateTime end)
    {
        if (!CanForecast(start, end))
        {
            return null;
        }
        var url =
            $"https://api.open-meteo.com/v1/forecast" +
            $"?latitude={cluster.Center.Latitude}" +
            $"&longitude={cluster.Center.Longitude}" +
            $"&hourly=precipitation_probability" +
            $"&daily=weather_code,temperature_2m_min,temperature_2m_max,uv_index_max,precipitation_probability_max" +
            $"&start_date={start:yyyy-MM-dd}" +
            $"&end_date={end:yyyy-MM-dd}";

        return await _httpClient.GetFromJsonAsync<ForecastResponse>(url)
            ?? throw new AppException(
                "WEATHER_API_ERROR",
                $"Failed to retrieve forecast for the following location, long: {cluster.Center.Longitude} , lat: {cluster.Center.Latitude}");
    }

    private TravelTime MergeTravelWindows(
    IEnumerable<TravelTime> bestTimes,
    TravelPromptContext context)
    {
        var dateScores = new Dictionary<DateTime, double>();
        foreach (var time in bestTimes)
        {
            for (
                var date = time.StartTime;
                date <= time.EndTime;
                date = date.AddDays(1))
            {
                if (!dateScores.ContainsKey(date))
                    dateScores[date] = 0;

                dateScores[date] += time.WeatherScore;
            }
        }

        var tripDays = context.Days ?? 1;
        DateTime bestStart = DateTime.MinValue;
        double bestScore = -1;

        foreach (var start in dateScores.Keys)
        {
            double score = 0;

            for (int i = 0; i < tripDays; i++)
            {
                var day = start.AddDays(i);
                if (dateScores.TryGetValue(day, out var value))
                    score += value;
            }


            if (score > bestScore)
            {
                bestScore = score;
                bestStart = start;
            }
        }


        return new TravelTime
        {
            // Location = context.Destination ?? "",
            StartTime = bestStart,
            EndTime = bestStart.AddDays(tripDays - 1),
            WeatherScore = bestScore
        };
    }
    private static int GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        return culture.Calendar.GetWeekOfYear(
            date,
            System.Globalization.CalendarWeekRule.FirstDay,
            DayOfWeek.Monday);
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