
using TravelPlanner.Features.Map.Model;

namespace TravelPlanner.Features.Chat.Services;

public sealed class ScoringService
{
    private readonly CurrencyExchangeService _currencyExchangeService;

    private readonly Utils _utils;

    public ScoringService(CurrencyExchangeService currencyExchangeService, Utils utils)
    {
        _currencyExchangeService = currencyExchangeService;
        _utils = utils;
    }
    public async Task<TravelResponse> ScorePlaces(
    TravelResponse response,
    TravelSession session)
    {
        try
        {
            var weights = BuildWeights(session);

            foreach (var place in response.TripPlanningData.RecommendedPlaces)
            {
                place.Score = await ScorePlace(place, session, weights, response.TripPlanningData.Altitude);
            }

            response.TripPlanningData.RecommendedPlaces =
                response.TripPlanningData.RecommendedPlaces
                    .OrderByDescending(p => p.Score.TotalScore)
                    .ToList();

            session.Stage = TravelStage.SetupItinerary;

            return response;
        }
        catch (Exception ex)
        {
           throw new AppException("SCRO_ERR","Error while scoring places", ex); 
        }

    }

    private async Task<PlaceScore> ScorePlace(
    Place place,
    TravelSession session,
    ScoreWeights weights,
    Altitude altitude)
    {
        var result = new PlaceScore();

        AddScore(
            result,
            "Rating",
            ScoreRating(place),
            weights.Rating);

        AddScore(
            result,
            "Budget",
            await ScoreBudget(place, session),
            weights.Budget);

        AddScore(
            result,
            "Interest",
            ScoreInterest(place, session),
            weights.Interest);

        AddScore(
            result,
            "Route",
            ScoreRoute(place, altitude),
            weights.Route);

        return result;
        //Adjust weigth, score range
    }

    private static void AddScore(
    PlaceScore score,
    string category,
    double value,
    double weight)
    {
        var weighted = value * weight;

        score.Breakdown[category] = weighted;

        score.TotalScore += weighted;
    }

    private double ScoreRating(Place place)
    {
        double rating =
            place.Rating / 5.0;

        double popularity =
            Math.Min(
                place.UserRatingCount,
                1000) / 1000.0;

        return
            rating * 0.7 +
            popularity * 0.3;
    }

    private async Task<double> ScoreBudget(
    Place place,
    TravelSession session)
    {
        if (session.Context.Budget?.Units is null ||
            session.Context.Days is null ||
            session.Context.Days <= 0)
        {
            return 1.0;
        }
        var dailyBudget =
       await GetDailyBudgetInPlaceCurrency(
           session,
           place.PriceRange?.StartPrice?.CurrencyCode ?? session.Context.Budget.CurrencyCode ?? "USD");

        var allocation = GetBudgetAllocation(place.Category);

        decimal minBudget = dailyBudget * allocation.Min;
        decimal maxBudget = dailyBudget * allocation.Max;

        decimal budgetFit = ScoreBudgetFit(place, minBudget, maxBudget);

        decimal priceLevel = (decimal)ScorePriceLevel(place, session);

        // decimal confidence = (decimal)ScorePriceConfidence(place);

        decimal score =
            budgetFit * 0.5m +
            priceLevel * 0.3m;
        // confidence * 0.2m;

        return (double)score;
    }

    private async Task<decimal> GetDailyBudgetInPlaceCurrency(
    TravelSession session,
    string targetCurrency)
    {
        var budget = session.Context.Budget!.Units!;
        var currency = session.Context.Budget.CurrencyCode ?? "USD";

        var converted = await _currencyExchangeService.ConvertAsync(
            budget,
            currency,
            targetCurrency);

        return converted / session.Context.Days!.Value;
    }

    private static (decimal Min, decimal Max) GetBudgetAllocation(PlaceCategory category)
    {
        return category switch
        {
            PlaceCategory.Hotel => (0.30m, 0.40m),
            PlaceCategory.Restaurant => (0.30m, 0.40m),
            PlaceCategory.Travel => (0.40m, 0.50m),
            _ => (1.0m, 1.0m)
        };
    }

    private decimal ScoreBudgetFit(
    Place place,
    decimal minBudget,
    decimal maxBudget)
    {
        if (place.PriceRange?.StartPrice?.Units is null ||
            place.PriceRange?.EndPrice?.Units is null)
        {
            return 0.8m;
        }

        decimal avgPrice =
            (place.PriceRange.StartPrice.Units +
             place.PriceRange.EndPrice.Units) / 2m;

        if (avgPrice <= minBudget)
            return 1.2m;

        if (avgPrice <= maxBudget)
            return 1.0m;

        decimal ratio = avgPrice / maxBudget;

        return ratio switch
        {
            <= 1.2m => 0.8m,
            <= 1.5m => 0.5m,
            _ => 0.1m
        };
    }

    private double ScorePriceLevel(
    Place place,
    TravelSession session)
    {
        bool preferCheap =
            session.Context.Preferences
                .Contains("Cheap", StringComparer.OrdinalIgnoreCase);

        bool preferLuxury =
            session.Context.Preferences
                .Contains("Luxury", StringComparer.OrdinalIgnoreCase);

        if (preferCheap)
        {
            return place.PriceLevel switch
            {
                "PRICE_LEVEL_FREE" => 1.20,
                "PRICE_LEVEL_INEXPENSIVE" => 1.10,
                "PRICE_LEVEL_MODERATE" => 0.80,
                "PRICE_LEVEL_EXPENSIVE" => 0.40,
                "PRICE_LEVEL_VERY_EXPENSIVE" => 0.10,
                _ => 0.80
            };
        }

        else if (preferLuxury)
        {
            return place.PriceLevel switch
            {
                "PRICE_LEVEL_FREE" => 0.60,
                "PRICE_LEVEL_INEXPENSIVE" => 0.70,
                "PRICE_LEVEL_MODERATE" => 1.00,
                "PRICE_LEVEL_EXPENSIVE" => 1.10,
                "PRICE_LEVEL_VERY_EXPENSIVE" => 1.20,
                _ => 1.00
            };
        }
        // Balanced traveler
        else return place.PriceLevel switch
        {
            "PRICE_LEVEL_FREE" => 1.00,
            "PRICE_LEVEL_INEXPENSIVE" => 1.00,
            "PRICE_LEVEL_MODERATE" => 1.00,
            "PRICE_LEVEL_EXPENSIVE" => 0.90,
            "PRICE_LEVEL_VERY_EXPENSIVE" => 0.70,
            _ => 1.00
        };
    }

    // private double ScorePriceConfidence(
    // Place place)
    // {
    //     return place.PriceRange != null
    //         ? 1.0
    //         : 0.8;
    // }


    private double ScoreInterest(
    Place place,
    TravelSession session)
    {
        if (!session.Context.Interests.Any())
            return 1;

        var matches =
            session.Context.Interests.Count(i =>
                MapVariables.InterestTypes[i]
                    .Intersect(place.Types)
                    .Any());

        return
            matches /
            (double)session.Context.Interests.Count;
    }

    private double ScoreRoute(
    Place place,
    Altitude altitude)
    {
        double distance = _utils.Haversine(
                                place.Location.Latitude,
                                place.Location.Longitude,
                                altitude.Latitude,
                                altitude.Longitude);

        return 5 - (distance * 0.1);
    }


    private ScoreWeights BuildWeights(
    TravelSession session)
    {
        var w = new ScoreWeights();

        if (session.Context.Preferences.Contains("Review"))
            w.Rating *= 1.5;

        if (session.Context.Preferences.Contains("Convenient"))
            w.Route *= 1.5;

        if (session.Context.Interests.Any())
            w.Interest *= 1.3;

        return w;
    }
}