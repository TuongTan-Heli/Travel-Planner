public class TravelWorkflowResult
{
    public string Message { get; set; } = "";

    public bool IsReadyForPlanning { get; set; }

    public TravelStage NextAction { get; set; }
}