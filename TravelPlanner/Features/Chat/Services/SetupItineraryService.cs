using TravelPlanner.Features.AI.Model;
using TravelPlanner.Features.Map.Model;

namespace TravelPlanner.Features.Chat.Services;

public sealed class SetupItineraryService
{
    private readonly Utils _utils;

    public SetupItineraryService(Utils utils)
    {
        _utils = utils;
    }
    private const double MaxDailyHours = 8;
    private const double ClusterRadiusKm = 100;
    public async Task<Itinerary> Setup(TravelResponse response, TravelSession session)
    {
        try
        {
            var itinerary = new Itinerary();

            var days = session.Context.Days ?? 1;

            var clusters = response.TripPlanningData.RecommendedPlaces
                    .Select(p => p.PlaceCluster)
                    .Where(c => c != null)
                    .GroupBy(c => c?.Center)
                    .Select(g => g.First())
                    .ToList();

            if (!clusters.Any()) return itinerary;

            var daysPerCluster = AllocateDays(days, clusters.Count);

            var currentDay = 1;

            for (int i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];

                var hotel = response.TripPlanningData.RecommendedPlaces
                    .Where(x => x.PlaceCluster == cluster)
                    .Where(x => x.Category == PlaceCategory.Hotel)
                    .OrderByDescending(x => x.Score.TotalScore)
                    .FirstOrDefault();

                if (hotel == null)
                    continue;

                var remaining = response.TripPlanningData.RecommendedPlaces
                    .Where(x => x.PlaceCluster == cluster)
                    .Where(x => x != hotel)
                    .OrderByDescending(x => x.Score.TotalScore)
                    .ToList();

                for (int d = 0; d < daysPerCluster[i]; d++)
                {
                    if (!remaining.Any())
                        break;

                    var dayPlan = BuildDay(currentDay, hotel, remaining, session.Context.TravelFrequency ?? TravelFrequency.Medium);

            itinerary.DayPlans.Add(dayPlan);

            currentDay++;
        }
            }

            return itinerary;
        }
        catch (Exception ex)
        {
            throw new AppException("ITI_SETUP", "Error while setup itinerary", ex);
        }
    }

    private static List<int> AllocateDays(int totalDays, int clusterCount)
{
    var result = new List<int>();

    var baseDays = totalDays / clusterCount;
    var extra = totalDays % clusterCount;

    for (int i = 0; i < clusterCount; i++)
    {
        result.Add(baseDays + (i < extra ? 1 : 0));
    }
    return result;
}

private DayPlan BuildDay(int day, Place hotel, List<Place> remaining, TravelFrequency travelFrequency)
{
    var dayPlan = new DayPlan
    {
        DayNumber = day,
        Hotel = hotel
    };

    double hours = 0;

    Place currentLocation = hotel;

    var attractionsCount = 0;
    var coffeeCount = 0;
    var restaurantCount = 0;

    // Get target attraction count from travel frequency
    var (minAttractions, maxAttractions) = _utils.GetTravelPlaceCount(travelFrequency);

    // --------------------------------------------------
    // Breakfast
    // --------------------------------------------------

    var breakfast = FindNearestRestaurant(
        currentLocation,
        remaining,
        p => p.ServesBreakfast ?? false);

    if (breakfast != null &&
        restaurantCount < 3 &&
        AddStop(
            dayPlan,
            currentLocation,
            breakfast,
            StopType.Breakfast,
            ref hours,
            remaining))
    {
        currentLocation = breakfast;
        restaurantCount++;
    }
    else
    {
        var firstCoffee = FindCoffee(currentLocation, remaining);

        if (firstCoffee != null &&
            coffeeCount < 2 &&
            AddStop(
                dayPlan,
                currentLocation,
                firstCoffee,
                StopType.Coffee,
                ref hours,
                remaining))
        {
            currentLocation = firstCoffee;
            coffeeCount++;
        }
    }

    // --------------------------------------------------
    // Main attraction loop
    // --------------------------------------------------

    var lunchAdded = false;

    while (
        attractionsCount < maxAttractions &&
        hours < MaxDailyHours)
    {
        var attraction = FindBestNearby(
            currentLocation,
            remaining,
            PlaceCategory.Travel);

        if (attraction == null)
            break;

        if (!AddStop(
                dayPlan,
                currentLocation,
                attraction,
                StopType.Attraction,
                ref hours,
                remaining))
        {
            break;
        }

        currentLocation = attraction;
        attractionsCount++;

        // --------------------------------------------------
        // Lunch after approximately half the attractions
        // --------------------------------------------------

        if (!lunchAdded &&
            attractionsCount >= Math.Max(1, maxAttractions / 2))
        {
            var lunch = FindNearestRestaurant(
                currentLocation,
                remaining,
                p => p.ServesLunch ?? false);

            if (lunch != null &&
                restaurantCount < 3 &&
                AddStop(
                    dayPlan,
                    currentLocation,
                    lunch,
                    StopType.Lunch,
                    ref hours,
                    remaining))
            {
                currentLocation = lunch;
                restaurantCount++;
                lunchAdded = true;
            }
        }
    }

    // --------------------------------------------------
    // If lunch wasn't added during the attraction loop
    // --------------------------------------------------

    if (!lunchAdded)
    {
        var lunch = FindNearestRestaurant(
            currentLocation,
            remaining,
            p => p.ServesLunch ?? false);

        if (lunch != null &&
            restaurantCount < 3 &&
            AddStop(
                dayPlan,
                currentLocation,
                lunch,
                StopType.Lunch,
                ref hours,
                remaining))
        {
            currentLocation = lunch;
            restaurantCount++;
        }
    }

    // --------------------------------------------------
    // Dinner
    // --------------------------------------------------

    var dinner = FindNearestRestaurant(
        currentLocation,
        remaining,
        p => p.ServesDinner ?? false);

    if (dinner != null &&
        restaurantCount < 3 &&
        AddStop(
            dayPlan,
            currentLocation,
            dinner,
            StopType.Dinner,
            ref hours,
            remaining))
    {
        currentLocation = dinner;
        restaurantCount++;
    }

    // --------------------------------------------------
    // Evening / coffee
    // --------------------------------------------------

    if (hours < MaxDailyHours)
    {
        var night =
            FindEveningPlace(currentLocation, remaining)
            ?? FindCoffee(currentLocation, remaining);

        if (night != null)
        {
            var type =
                MapVariables.EveningTypes.Any(t =>
                    night.PrimaryType?.Contains(
                        t,
                        StringComparison.OrdinalIgnoreCase) == true)
                    ? StopType.FreeTime
                    : StopType.Coffee;

            if (night.PrimaryType != null &&
                (night.PrimaryType.Contains(
                    "cafe",
                    StringComparison.OrdinalIgnoreCase) ||
                 night.PrimaryType.Contains(
                    "coffee",
                    StringComparison.OrdinalIgnoreCase)))
            {
                type = StopType.Coffee;
            }

            if (type == StopType.Coffee)
            {
                if (coffeeCount < 2 &&
                    AddStop(
                        dayPlan,
                        currentLocation,
                        night,
                        type,
                        ref hours,
                        remaining))
                {
                    coffeeCount++;
                }
            }
            else
            {
                AddStop(
                    dayPlan,
                    currentLocation,
                    night,
                    type,
                    ref hours,
                    remaining);
            }
        }
    }

    dayPlan.TotalHours = hours;

    return dayPlan;
}

private Place? FindEveningPlace(Place center, List<Place> remaining)
{
    return remaining
    .Where(x => x.Category != PlaceCategory.Hotel)
    .Where(x => !IsFoodRelatedPlace(x))
    .Where(x => MapVariables.EveningTypes.Contains(x.PrimaryType))
    .OrderBy(x => DistanceKm(center, x))
    .ThenByDescending(x => x.Score.TotalScore)
    .FirstOrDefault();
}

private bool AddStop(DayPlan plan, Place from, Place place, StopType type, ref double hours, List<Place> remaining)
{
    var travel = EstimatedTravelTime(from, place);
    var duration = GetDuration(place);

    if (hours + travel + duration > MaxDailyHours)
        return false;

    hours += travel + duration;

    plan.Stops.Add(new ItineraryStop
    {
        Place = place,
        Type = type,
        EstimatedHours = duration
    });

    remaining.Remove(place);

    return true;
}


private Place? FindBestNearby(Place center, List<Place> remaining, PlaceCategory category)
{
    return remaining
        .Where(x => x.Category == category)
        .Where(x => !IsFoodRelatedPlace(x))
        .Where(x => DistanceKm(center, x) <= ClusterRadiusKm)
        .OrderByDescending(x => x.Score.TotalScore - DistanceKm(center, x) * 0.25)
        .FirstOrDefault();
}

private Place? FindNearestRestaurant(Place center, List<Place> remaining, Func<Place, bool> filter)
{
    return remaining
        .Where(x => x.Category == PlaceCategory.Restaurant)
        .Where(filter)
        .OrderBy(x => DistanceKm(center, x))
        .ThenByDescending(x => x.Score.TotalScore)
        .FirstOrDefault();
}

private Place? FindCoffee(Place center, List<Place> remaining)
{
    return remaining
        .Where(x =>
            x.PrimaryType.Contains("cafe", StringComparison.OrdinalIgnoreCase) ||
            x.PrimaryType.Contains("coffee", StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => DistanceKm(center, x))
        .ThenByDescending(x => x.Score.TotalScore)
        .FirstOrDefault();
}

private bool IsFoodRelatedPlace(Place place)
{
    if (place is null)
    {
        return false;
    }

    var foodTypes = MapVariables.InterestTypes["food"]
        .Concat(MapVariables.PrimaryRestaurantTypes)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    if (!string.IsNullOrWhiteSpace(place.PrimaryType) && foodTypes.Contains(place.PrimaryType))
    {
        return true;
    }

    return place.Types?.Any(type => foodTypes.Contains(type)) ?? false;
}

private double EstimatedTravelTime(Place from, Place to)
{
    var distance = DistanceKm(from, to);

    // average speed 25 km/h
    return distance / 25.0;
}

private double DistanceKm(Place a, Place b)
{
    return _utils.Haversine(
        a.Location.Latitude,
        a.Location.Longitude,
        b.Location.Latitude,
        b.Location.Longitude);
}

public static double GetDuration(Place place)
{
    // Prefer PrimaryType
    if (!string.IsNullOrWhiteSpace(place.PrimaryType) &&
        MapVariables.TypeTravelDuration.TryGetValue(place.PrimaryType, out var duration))
    {
        return duration;
    }

    // Fallback to any Google type
    foreach (var type in place.Types)
    {
        if (MapVariables.TypeTravelDuration.TryGetValue(type, out duration))
        {
            return duration;
        }
    }

    // Default for unknown attractions
    return place.Category switch
    {
        PlaceCategory.Restaurant => 1.5,
        PlaceCategory.Hotel => 0,
        _ => 2.0
    };
}
}