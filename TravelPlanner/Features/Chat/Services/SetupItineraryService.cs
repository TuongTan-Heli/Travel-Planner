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

                    var dayPlan = BuildDay(currentDay, hotel, remaining);

                    itinerary.DayPlans.Add(dayPlan);

                    currentDay++;
                }
            }

            session.Stage = TravelStage.FinalPresentation;
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

    private DayPlan BuildDay(int day, Place hotel, List<Place> remaining)
    {
        var dayPlan = new DayPlan
        {
            DayNumber = day,
            Hotel = hotel
        };

        double hours = 0;

        // Current location starts at hotel
        Place currentLocation = hotel;

        // Counters to enforce per-day limits
        var attractionsCount = 0;
        var coffeeCount = 0;
        var restaurantCount = 0;

        // First breakfast (or coffee)
        var breakfast = FindNearestRestaurant(
            currentLocation,
            remaining,
            p => p.ServesBreakfast ?? false);

        if (breakfast != null)
        {
            if (restaurantCount < 2 && AddStop(dayPlan, currentLocation, breakfast, StopType.Breakfast, ref hours, remaining))
            {
                currentLocation = breakfast;
                restaurantCount++;
            }
        }
        else
        {
            var firstCoffee = FindCoffee(currentLocation, remaining);
            if (firstCoffee != null)
            {
                if (coffeeCount < 2 && AddStop(dayPlan, currentLocation, firstCoffee, StopType.Coffee, ref hours, remaining))
                {
                    currentLocation = firstCoffee;
                    coffeeCount++;
                }
            }
        }

        // Then attraction
        var firstAttraction = FindBestNearby(currentLocation, remaining, PlaceCategory.Travel);
        if (firstAttraction != null)
        {
            if (AddStop(dayPlan, currentLocation, firstAttraction, StopType.Attraction, ref hours, remaining))
            {
                currentLocation = firstAttraction;
                attractionsCount++;
            }
        }

        // Then lunch
        var lunch = FindNearestRestaurant(currentLocation, remaining, p => p.ServesLunch ?? false);
        if (lunch != null)
        {
            if (restaurantCount < 2 && AddStop(dayPlan, currentLocation, lunch, StopType.Lunch, ref hours, remaining))
            {
                currentLocation = lunch;
                restaurantCount++;
            }
        }

        // Then another attraction
        var secondAttraction = FindBestNearby(currentLocation, remaining, PlaceCategory.Travel);
        if (secondAttraction != null)
        {
            if (AddStop(dayPlan, currentLocation, secondAttraction, StopType.Attraction, ref hours, remaining))
            {
                currentLocation = secondAttraction;
                attractionsCount++;
            }
        }

        // Then dinner
        var dinner = FindNearestRestaurant(currentLocation, remaining, p => p.ServesDinner ?? false);
        if (dinner != null)
        {
            if (restaurantCount < 2 && AddStop(dayPlan, currentLocation, dinner, StopType.Dinner, ref hours, remaining))
            {
                currentLocation = dinner;
                restaurantCount++;
            }
        }

        // Night travel (or coffee)
        if (hours < MaxDailyHours)
        {
            var night = FindEveningPlace(currentLocation, remaining) ?? FindCoffee(currentLocation, remaining);
            if (night != null)
            {
                var type = MapVariables.EveningTypes.Any(t => night.PrimaryType?.Contains(t, StringComparison.OrdinalIgnoreCase) == true)
                    ? StopType.FreeTime
                    : StopType.Coffee;

                // choose appropriate stop type if it's clearly a coffee place
                if (night.PrimaryType != null && (night.PrimaryType.Contains("cafe", StringComparison.OrdinalIgnoreCase) || night.PrimaryType.Contains("coffee", StringComparison.OrdinalIgnoreCase)))
                {
                    type = StopType.Coffee;
                }

                // enforce coffee/restaurant caps
                if (type == StopType.Coffee)
                {
                    if (coffeeCount < 2)
                    {
                        if (AddStop(dayPlan, currentLocation, night, type, ref hours, remaining))
                            coffeeCount++;
                    }
                }
                else if (type == StopType.FreeTime)
                {
                    AddStop(dayPlan, currentLocation, night, type, ref hours, remaining);
                }
            }
        }

        // Ensure each day contains at least 2 attractions if time and remaining allow
        while (attractionsCount < 2 && hours < MaxDailyHours)
        {
            var extraAttraction = FindBestNearby(currentLocation, remaining, PlaceCategory.Travel);
            if (extraAttraction == null) break;

            if (AddStop(dayPlan, currentLocation, extraAttraction, StopType.Attraction, ref hours, remaining))
            {
                attractionsCount++;
                currentLocation = extraAttraction;
            }
            else
            {
                break; // no more time
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