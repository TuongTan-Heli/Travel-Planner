using TravelPlanner.Features.Chat.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TravelPlanner;

public class PresentationService
{
    private readonly ChatService _chatService;
    private readonly Utils _utils;

    public PresentationService(ChatService chatService, Utils utils)
    {
        _chatService = chatService;
        _utils = utils;
    }
    public async Task<FinalPresentation> Present(TravelResponse response, TravelSession session)
    {
        var prompt = PromptBuilder.Build(
                    TravelStage.FinalPresentation,
                    session.Context,
                    response);

        var replyText = await _chatService.GenerateReplyAsync(prompt, session);
        var cleanedJson = FixInvalidJsonEscapes(replyText);
        try
        {
            var result = JsonSerializer.Deserialize<FinalPresentation>(
                cleanedJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() },
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                }) ?? throw new AppException(
                    "PRESENTATION_PARSE_ERROR",
                    "Failed to parse final presentation result.");


            result.CandidatePlaces = response.TripPlanningData.RecommendedPlaces
                .GroupBy(BuildPlaceId)
                .Select(x => x.First())
                .Select(MapPlace)
                .ToList();

            BuildAlternatives(result);

            HydratePlaces(result, response);

            return result;
        }
        catch (JsonException ex)
        {
            throw new AppException(
        "PRESENTATION_PARSE_ERROR",
        $"""
        Failed to deserialize FinalPresentation.

        Error: {ex.Message}
        Path: {ex.Path}
        Line: {ex.LineNumber}
        Byte Position: {ex.BytePositionInLine}

        JSON:
        {cleanedJson}
        """);
        }
        catch (Exception ex)
        {
            throw new AppException(
                "PRESENTATION_PARSE_ERROR",
                $"Unexpected deserialization error: {ex}");
        }

    }
    private static string BuildPlaceId(Place place)
    {
        return $"{place.Name}|{place.Address}|{place.Location.Latitude}|{place.Location.Longitude}";
    }
    private static PlacePresentation MapPlace(Place place)
    {
        return new PlacePresentation
        {
            PlaceId = BuildPlaceId(place),
            Name = place.Name,
            Address = place.Address,
            PrimaryType = place.PrimaryType,
            Category = place.Category.ToString(),
            Rating = place.Rating,
            Location = new LocationPresentation
            {
                Latitude = place.Location.Latitude,
                Longitude = place.Location.Longitude
            },
            Types = [.. place.Types],
            OpenTime = [.. place.OpenTime],
            Reviews = [.. place.Reviews],
            PriceRange = place.PriceRange,
            PhoneNumber = place.PhoneNumber,
            WebsiteUrl = place.WebsiteUrl,
            DineIn = place.DineIn,
            AllowsDogs = place.AllowsDogs,
            GoodForChildren = place.GoodForChildren,
            GoodForGroups = place.GoodForGroups,
            GoodForWatchingSports = place.GoodForWatchingSports,
            LiveMusic = place.LiveMusic,
            PaymentOptions = place.PaymentOptions,
            OutdoorSeating = place.OutdoorSeating,
            Reservable = place.Reservable,
            Description = place.Description,
            ServesBeer = place.ServesBeer,
            ServesBreakfast = place.ServesBreakfast,
            ServesCocktails = place.ServesCocktails,
            ServesLunch = place.ServesLunch,
            ServesDinner = place.ServesDinner,
            ServesBrunch = place.ServesBrunch,
            ServesCoffee = place.ServesCoffee,
            ServesDessert = place.ServesDessert,
            ReviewSummary = place.ReviewSummary,
            UserRatingCount = place.UserRatingCount,
            PriceLevel = place.PriceLevel,
            Takeout = place.Takeout,
        };
    }

    private static void HydratePlaces(
    FinalPresentation presentation,
    TravelResponse response)
    {
        var sourcePlaces = response.Itinerary.DayPlans
            .SelectMany(day =>
            {
                var places = new List<Place>();


                places.AddRange(day.Stops.Select(x => x.Place));

                return places;
            })
            .Concat(response.TripPlanningData.RecommendedPlaces)
            .Where(x => x != null)
            .GroupBy(BuildPlaceId)
            .ToDictionary(
                x => x.Key,
                x => x.First()
            );

        foreach (var day in presentation.Itinerary)
        {
            if (day.Hotel != null)
            {
                HydrateActivity(day.Hotel, sourcePlaces);
            }

            foreach (var activity in day.Activities)
            {
                HydrateActivity(activity, sourcePlaces);
            }
        }
    }

    private static void HydrateActivity(
        PresentationActivity activity,
        Dictionary<string, Place> source)
    {
        activity.Place = ReplacePlace(activity.Place, source);

        for (var i = 0; i < activity.Alternatives.Count; i++)
        {
            activity.Alternatives[i] =
                ReplacePlace(activity.Alternatives[i], source);
        }
    }
    private void BuildAlternatives(FinalPresentation presentation)
    {

        presentation.Itinerary.ForEach(day =>
        {
            if (day.Hotel != null)
            {
                day.Hotel.Alternatives.AddRange(
                    FindAlternatives(
                        day.Hotel.Place,
                        presentation.CandidatePlaces));
            }

            foreach (var activity in day.Activities)
            {
                activity.Alternatives.AddRange(
                    FindAlternatives(
                        activity.Place,
                        presentation.CandidatePlaces));
            }
        });
    }

    private IEnumerable<PlacePresentation> FindAlternatives(
    PlacePresentation place,
    IEnumerable<PlacePresentation> candidates)
    {
        return candidates
            .Where(p => p.PlaceId != place.PlaceId && _utils.Haversine(place.Location.Latitude, place.Location.Longitude, p.Location.Latitude, p.Location.Longitude) <= 50)
            .Select(p => new
            {
                Place = p,
                Score = (p.PrimaryType == place.PrimaryType ? 100 : 0) +
                    p.Types.Intersect(place.Types).Count() * 10
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Place.Rating ?? 0)
            .Take(5)
            .Select(x => x.Place);
    }

    private static PlacePresentation ReplacePlace(
    PlacePresentation current,
    Dictionary<string, Place> source)
    {
        if (current == null || string.IsNullOrEmpty(current.PlaceId))
        {
            return current!;
        }

        if (!source.TryGetValue(current.PlaceId, out var place))
        {
            return current;
        }

        return MapPlace(place);
    }

    private static string FixInvalidJsonEscapes(string json)
    {
        return Regex.Replace(
            json,
            @"\\(?![""\\/bfnrtu])",
            @"\\");
    }
}