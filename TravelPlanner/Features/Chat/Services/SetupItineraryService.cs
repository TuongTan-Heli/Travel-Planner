using TravelPlanner.Features.Map.Model;

namespace TravelPlanner.Features.Chat.Services;

public sealed class SetupItineraryService
{
    private readonly Utils _utils;

    public SetupItineraryService(Utils utils)
    {
        _utils = utils;
    }
    private const double MaxDailyHours = 9;
    private const double ClusterRadiusKm = 100;
    public async Task<Itinerary> Setup(
        TravelResponse response,
        TravelSession session)
    {
        try
        {
            var itinerary = new Itinerary();

            var remaining =
                response.TripPlanningData.RecommendedPlaces
                .OrderByDescending(x => x.Score.TotalScore)
                .ToList();

            if (!remaining.Any() || session.Context.Days is null)
                return itinerary;

            var hotels = remaining
                .Where(x => x.Category == PlaceCategory.Hotel)
                .ToList();

            Place? currentHotel = ChooseBestHotel(hotels, remaining);

            remaining.Remove(currentHotel!);

            for (int day = 1; day <= session.Context.Days; day++)
            {
                var remainingDays = session.Context.Days ?? 0 - day + 1;

                var remainingTravel =  remaining.Count(x => x.Category == PlaceCategory.Travel);

                var targetTravelToday = (int)Math.Ceiling((double)remainingTravel / remainingDays);
                
                if (currentHotel == null)
                    break;

                if (NeedHotelChange(currentHotel, remaining))
                {
                    var nextHotel = ChooseBestHotel(
                        hotels.Where(x => x != currentHotel).ToList(),
                        remaining);

                    if (nextHotel != null)
                    {
                        currentHotel = nextHotel;
                        remaining.Remove(currentHotel);
                    }
                }

                var dayPlan = BuildDay(
                    day,
                    currentHotel,
                    remaining,
                    0,
                    targetTravelToday);

                dayPlan.Hotel = currentHotel;

                itinerary.DayPlans.Add(dayPlan);

            }

            return itinerary;
        }
        catch (Exception ex)
        {
            throw new AppException(
                "ITI_SETUP",
                "Error while setup itinerary",
                ex);
        }


    }

    private DayPlan BuildDay(
    int day,
    Place hotel,
    List<Place> remaining,
    int targetTravelToday,
    int travelCount)
    {
        var dayPlan = new DayPlan
        {
            DayNumber = day,
            Hotel = hotel
        };

        double hours = 0;

        // Current location starts at hotel
        Place currentLocation = hotel;

        //-------------------------
        // Breakfast
        //-------------------------

        var breakfast =
            FindNearestRestaurant(
                currentLocation,
                remaining,
                p => p.ServesBreakfast ?? false);

        if (breakfast != null)
        {
            AddStop(
                dayPlan,
                breakfast,
                StopType.Breakfast,
                ref hours,
                remaining);

            currentLocation = breakfast;
        }

        //-------------------------
        // Attractions
        //-------------------------

        while (hours < MaxDailyHours && travelCount < targetTravelToday)
        {
            var attraction =
                FindBestNearby(
                    currentLocation,
                    remaining,
                    PlaceCategory.Travel);

            if (attraction == null)
                break;

            AddStop(
                dayPlan,
                attraction,
                StopType.Attraction,
                ref hours,
                remaining);

            currentLocation = attraction;

            if (hours >= MaxDailyHours)
                break;

            //---------------------
            // Lunch
            //---------------------

            if (!dayPlan.Stops.Any(x => x.Type == StopType.Lunch))
            {
                var lunch =
                    FindNearestRestaurant(
                        currentLocation,
                        remaining,
                        p => p.ServesLunch ?? false);

                if (lunch != null)
                {
                    AddStop(
                        dayPlan,
                        lunch,
                        StopType.Lunch,
                        ref hours,
                        remaining);

                    currentLocation = lunch;
                }
            }

            //---------------------
            // Coffee
            //---------------------

            if (hours < MaxDailyHours - 1)
            {
                var coffee =
                    FindCoffee(
                        currentLocation,
                        remaining);

                if (coffee != null)
                {
                    AddStop(
                        dayPlan,
                        coffee,
                        StopType.Coffee,
                        ref hours,
                        remaining);

                    currentLocation = coffee;
                }
            }
            travelCount++;
        }

        //-------------------------
        // Dinner
        //-------------------------

        var dinner =
            FindNearestRestaurant(
                currentLocation,
                remaining,
                p => p.ServesDinner ?? false);

        if (dinner != null)
        {
            AddStop(
                dayPlan,
                dinner,
                StopType.Dinner,
                ref hours,
                remaining);

            currentLocation = dinner;
        }

        dayPlan.TotalHours = hours;

        return dayPlan;
    }

    private void AddStop(
    DayPlan plan,
    Place place,
    StopType type,
    ref double hours,
    List<Place> remaining)
    {
        plan.Stops.Add(new ItineraryStop
        {
            Place = place,
            Type = type,
            EstimatedHours = GetDuration(place)
        });

        hours += GetDuration(place);

        remaining.Remove(place);
    }

    private Place? ChooseBestHotel(
        List<Place> hotels,
        List<Place> allPlaces)
    {
        if (!hotels.Any())
            return null;

        return hotels
            .Select(h => new
            {
                Hotel = h,
                ClusterScore =
                    allPlaces
                        .Where(p =>
                            p.Category == PlaceCategory.Travel &&
                            DistanceKm(h, p) <= ClusterRadiusKm)
                        .Sum(p => p.Score.TotalScore)
            })
            .OrderByDescending(x => x.ClusterScore)
            .ThenByDescending(x => x.Hotel.Score.TotalScore)
            .First()
            .Hotel;
    }

    private bool NeedHotelChange(
        Place hotel,
        List<Place> remaining)
    {
        var attractions =
            remaining
            .Where(x => x.Category == PlaceCategory.Travel)
            .ToList();

        if (!attractions.Any())
            return false;

        var nearby =
            attractions.Count(x =>
                DistanceKm(hotel, x) <= ClusterRadiusKm);

        return nearby < 3;
    }

    private Place? FindBestNearby(
        Place center,
        List<Place> remaining,
        PlaceCategory category)
    {
        return remaining
            .Where(x => x.Category == category)
            .Where(x => DistanceKm(center, x) <= ClusterRadiusKm)
            .OrderByDescending(x => x.Score.TotalScore)
            .FirstOrDefault();
    }

    private Place? FindNearestRestaurant(
        Place center,
        List<Place> remaining,
        Func<Place, bool> filter)
    {
        return remaining
            .Where(x => x.Category == PlaceCategory.Restaurant)
            .Where(filter)
            .OrderBy(x => DistanceKm(center, x))
            .ThenByDescending(x => x.Score.TotalScore)
            .FirstOrDefault();
    }

    private Place? FindCoffee(
        Place center,
        List<Place> remaining)
    {
        return remaining
            .Where(x =>
                x.PrimaryType.Contains("cafe",
                StringComparison.OrdinalIgnoreCase) ||
                x.PrimaryType.Contains("coffee",
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => DistanceKm(center, x))
            .ThenByDescending(x => x.Score.TotalScore)
            .FirstOrDefault();
    }

    private double DistanceKm(
        Place a,
        Place b)
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
    private static double DegreesToRadians(double degrees)
        => degrees * Math.PI / 180;
}