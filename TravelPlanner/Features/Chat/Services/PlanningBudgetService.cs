public sealed class PlanningBudgetService
{
    public (decimal Min, decimal Max) GetBudgetAllocation(
        PlaceCategory category)
    {
        return category switch
        {
            PlaceCategory.Hotel => (0.30m, 0.40m),
            PlaceCategory.Restaurant => (0.30m, 0.40m),
            PlaceCategory.Travel => (0.40m, 0.50m),
            _ => (1.0m, 1.0m)
        };
    }

    public bool IsPerPersonCategory(PlaceCategory category)
    {
        return category switch
        {
            PlaceCategory.Travel => true,
            PlaceCategory.Restaurant => true,
            PlaceCategory.Hotel => false,
            _ => true
        };
    }
}