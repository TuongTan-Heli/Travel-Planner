public class TravelSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

    public TravelStage Stage { get; set; } = TravelStage.IntentExtraction;

    public TravelPromptContext Context { get; set; } = new();
    
    public List<string> ChatHistory { get; set; } = new List<string>();

    public void Reset()
    {
        Stage = TravelStage.IntentExtraction;
        Context = new TravelPromptContext();
    }
}