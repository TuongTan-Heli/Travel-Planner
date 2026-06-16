using TravelPlanner.Features.Chat.Models;

public class TravelSession
{
    public TravelStage Stage { get; set; }
        = TravelStage.IntentExtraction;

    public TravelPromptContext Context { get; set; }
        = new();

    // public List<ChatMessage> Messages { get; set; }
    //     = [];

    // public bool IsReadyForPlanning { get; set; }

    // public DateTime LastUpdated { get; set; }
    //     = DateTime.UtcNow;
}