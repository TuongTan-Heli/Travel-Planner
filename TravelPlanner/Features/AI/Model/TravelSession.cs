public class TravelSession
{
    public string SessionId { get; set; }
        = Guid.NewGuid().ToString("N");

    public TravelStage Stage { get; set; }
        = TravelStage.IntentExtraction;

    public TravelPromptContext Context { get; set; }
        = new();
}