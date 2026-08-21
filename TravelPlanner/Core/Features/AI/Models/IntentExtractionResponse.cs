public sealed class IntentExtractionResponse
{
    public required string Message { get; init; }

    public required bool IsReadyForPlanning { get; init; }

    public TravelIntentResult? IntentResult { get; init; }
}