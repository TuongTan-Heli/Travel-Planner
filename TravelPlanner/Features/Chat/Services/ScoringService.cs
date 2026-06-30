
using TravelPlanner.Features.Map.Model;

namespace TravelPlanner;

public sealed class ScoringService
{
    public async Task<TravelResponse> ScorePlaces(
    TravelResponse response,
    TravelSession session)
    {
        var weights = BuildWeights(session);

        foreach (var place in response.TripPlanningData.RecommendedPlaces)
        {
            place.Score = ScorePlace(place, session, weights);
        }

        response.TripPlanningData.RecommendedPlaces =
            response.TripPlanningData.RecommendedPlaces
                .OrderByDescending(p => p.Score.TotalScore)
                .ToList();

        return response;
    }

    private PlaceScore ScorePlace(
    Place place,
    TravelSession session,
    ScoreWeights weights)
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
            ScoreBudget(place, session),
            weights.Budget);

        AddScore(
            result,
            "Interest",
            ScoreInterest(place, session),
            weights.Interest);

        AddScore(
            result,
            "Route",
            ScoreRoute(place, session),
            weights.Route);

        AddScore(
            result,
            "Crowd",
            ScoreCrowd(place),
            weights.Crowd);

        return result;
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

    private double ScoreBudget(
    Place place,
    TravelSession session)
    {
        if (session.Context.Budget == null)
            return 1;
        return 0;
    }

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
    TravelSession session)
    {
        return 1;
    }

    private double ScoreCrowd(
    Place place)
    {
        return 0.5;
    }

    private ScoreWeights BuildWeights(
    TravelSession session)
    {
        var w = new ScoreWeights();

        if (session.Context.Preferences.Contains("Reviews"))
            w.Rating *= 1.5;

        if (session.Context.Preferences.Contains("Cheaps"))
            w.Budget *= 1.5;

        if (session.Context.Preferences.Contains("Convenient"))
            w.Route *= 1.5;

        if (session.Context.Interests.Any())
            w.Interest *= 1.3;

        return w;
    }
}