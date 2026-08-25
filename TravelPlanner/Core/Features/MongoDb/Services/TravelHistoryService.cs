public sealed class TravelHistoryService
{
    private readonly TravelHistoryRepository _repository;

    public TravelHistoryService(
        TravelHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task SaveSuccessAsync(
        TravelSession session)
    {
        var record = new TravelSessionRecord
        {
            Session = session,
            ChatHistory = session.ChatHistory,
            Success = true,
            Error = null,
            CompletedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(record);
    }

    public async Task SaveFailureAsync(
        TravelSession session,
        string errorCode,
        string errorMessage)
    {
        var record = new TravelSessionRecord
        {
            Session = session,
            ChatHistory = session.ChatHistory,
            Success = false,
            Error = errorCode + " : " + errorMessage,
            CompletedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(record);
    }
}