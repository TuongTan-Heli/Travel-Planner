using TravelPlanner.Features.Chat.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public class PresentationService
{
    private readonly ChatService _chatService;

    public PresentationService(ChatService chatService)
    {
        _chatService = chatService;
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

            HydratePlaces(result, response);

            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex.Path);
            Console.WriteLine(ex.LineNumber);
            Console.WriteLine(ex.BytePositionInLine);
            Console.WriteLine(ex.Message);

            throw;
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
            Types = place.Types,
            PriceRange = place.PriceRange,
            Reviews = place.Reviews,
            OpenTime = place.OpenTime,
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

                if (day.Hotel != null)
                    places.Add(day.Hotel);

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
            day.Hotel = day.Hotel == null ? null : ReplacePlace(day.Hotel, sourcePlaces);


            foreach (var activity in day.Activities)
            {
                activity.Place = ReplacePlace(activity.Place, sourcePlaces);

                foreach (var alt in activity.Alternatives)
                {
                    alt.Place = ReplacePlace(alt.Place, sourcePlaces);
                }
            }
        }
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