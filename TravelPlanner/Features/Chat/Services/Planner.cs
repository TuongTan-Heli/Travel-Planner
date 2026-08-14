using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TravelPlanner.Features.Chat.Services;

public sealed class Planner
{
    private readonly Utils _utils;
    private readonly TravelPlanningService _travelPlanningService;
    private readonly ScoringService _scoringService;
    private readonly SetupItineraryService _setupItineraryService;
    private readonly PresentationService _presentationService;

    public Planner(Utils utils, TravelPlanningService travelPlanningService, ScoringService scoringService,
     SetupItineraryService setupItineraryService, PresentationService presentationService)
    {
        _utils = utils;
        _travelPlanningService = travelPlanningService;
        _scoringService = scoringService;
        _setupItineraryService = setupItineraryService;
        _presentationService = presentationService;
    }

    public async Task ContinuePlanningAsync(
    WebSocket socket,
    TravelSession session,
    TravelResponse response)
    {
        if (session.Context.StartDate is not null && session.Context.EndDate is not null)
        {
            session.Context.Days = (session.Context.EndDate.Value - session.Context.StartDate.Value).Days + 1;
        }

        if (session.Stage == TravelStage.LocationSelection)
        {
            await _utils.BroadcastStateAsync(socket, true, "Selecting best location");
            response.TripPlanningData = await _travelPlanningService.BuildPlanningDataAsync(session);
            session.Stage = TravelStage.Scoring;
        }

        if (session.Stage == TravelStage.Scoring)
        {
            await _utils.BroadcastStateAsync(socket, true, "Scoring places");
            response.TripPlanningData.RecommendedPlaces = await _scoringService.ScorePlaces(response.TripPlanningData.RecommendedPlaces, session);
            session.Stage = TravelStage.SetupItinerary;
        }

        if (session.Stage == TravelStage.SetupItinerary)
        {
            await _utils.BroadcastStateAsync(socket, true, "Setting up your trip");
            response.Itinerary = await _setupItineraryService.Setup(response, session);
            session.Stage = TravelStage.FinalPresentation;
        }

        if (session.Stage == TravelStage.FinalPresentation)
        {
            await _utils.BroadcastStateAsync(socket, true, "Preparing final presentation");
            response.FinalPresentation = await _presentationService.Present(response, session);

            var reply = new ChatMessage
            {
                Type = WebSocketMessType.Chat,
                Id = "Presentation",
                Text = JsonSerializer.Serialize(
                    response.FinalPresentation,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }),
                ChatType = ChatMessageType.Incoming,
                Sender = "Bot",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Thinking = false
            };

            await _utils.BroadcastAsync(socket, reply);
            await _utils.BroadcastStateAsync(socket, false, "");

            session.Stage = TravelStage.IntentExtraction;
        }
    }
}
