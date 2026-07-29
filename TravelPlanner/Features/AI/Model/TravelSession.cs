public class TravelSession
{
    public string SessionId { get; set; }
        = Guid.NewGuid().ToString("N");

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