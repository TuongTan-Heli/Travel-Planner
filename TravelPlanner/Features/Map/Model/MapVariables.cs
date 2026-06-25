namespace TravelPlanner.Features.Map.Model;

public static class MapVariables
{
    public static string GoogleMapFieldMask =
        "places.displayName," +
        "places.formattedAddress," +
        "places.location," +
        "places.photos," +
        "places.primaryType," +
        "places.types," +
        "places.rating," +
        "places.userRatingCount," +
        "places.currentOpeningHours," +
        "places.priceLevel," +
        "places.priceRange," +
        "places.reviews," +
        "places.reviewSummary," +
        "places.internationalPhoneNumber," +
        "places.websiteUri," +
        "places.dineIn," +
        "places.allowsDogs," +
        "places.goodForChildren," +
        "places.goodForGroups," +
        "places.goodForWatchingSports," +
        "places.liveMusic," +
        "places.paymentOptions," +
        "places.outdoorSeating," +
        "places.reservable," +
        "places.editorialSummary," +
        "places.servesBeer," +
        "places.servesBreakfast," +
        "places.servesBrunch," +
        "places.servesCocktails," +
        "places.servesCoffee," +
        "places.servesDessert," +
        "places.servesDinner," +
        "places.servesLunch," +
        "places.servesVegetarianFood," +
        "places.servesWine," +
        "places.takeout";

    public static List<string> PrimaryTypes = [
        "tourist_attraction",
        "museum",
        "national_park",
        "scenic_spot",
        "hotel",
        "resort_hotel",
        "campground",
        "restaurant",
        "cafe",
        "bar"
        ];
    public static List<string> PrimaryTravelTypes = [
        "tourist_attraction",
        "museum",
        "national_park",
        "scenic_spot"
    ];
    public static List<string> PrimaryHotelTypes = [
        "hotel",
        "resort_hotel",
        "campground"
    ];
    public static List<string> PrimaryRestaurantTypes = [
        "restaurant",
        "cafe",
        "bar"
    ];

    public static readonly Dictionary<string, string[]> InterestTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["attraction"] =
                [
                    "tourist_attraction",
                "historical_place",
                "historical_landmark",
                "visitor_center",
                "observation_deck",
                "plaza"
                ],

                ["nature"] =
                [
                    "beach",
                "lake",
                "mountain_peak",
                "river",
                "nature_preserve",
                "national_park",
                "state_park",
                "scenic_spot",
                "woods",
                "wildlife_refuge",
                "wildlife_park",
                "garden",
                "botanical_garden"
                ],

                ["culture"] =
                [
                    "museum",
                "art_gallery",
                "art_museum",
                "cultural_landmark",
                "castle",
                "monument",
                "history_museum",
                "performing_arts_theater",
                "auditorium",
                "opera_house",
                "cultural_center"
                ],

                ["entertainment"] =
                [
                    "aquarium",
                "zoo",
                "amusement_park",
                "amusement_center",
                "water_park",
                "movie_theater",
                "video_arcade",
                "concert_hall",
                "live_music_venue",
                "event_venue",
                "ferris_wheel",
                "planetarium",
                "bowling_alley"
                ],

                ["food"] =
                [
                    "restaurant",
                "cafe",
                "coffee_shop",
                "bakery",
                "breakfast_restaurant",
                "brunch_restaurant",
                "fine_dining_restaurant",
                "ice_cream_shop",
                "dessert_shop"
                ],

                ["nightlife"] =
                [
                    "bar",
                "cocktail_bar",
                "night_club",
                "karaoke",
                "lounge_bar",
                "live_music_venue",
                "brewery",
                "beer_garden"
                ],

                ["shopping"] =
                [
                    "shopping_mall",
                "market",
                "gift_shop",
                "book_store",
                "clothing_store",
                "department_store",
                "jewelry_store",
                "toy_store",
                "farmers_market"
                ],

                ["religious"] =
                [
                    "buddhist_temple",
                "church",
                "hindu_temple",
                "mosque",
                "synagogue",
                "shinto_shrine"
                ],

                ["family"] =
                [
                    "zoo",
                "aquarium",
                "water_park",
                "amusement_park",
                "botanical_garden",
                "city_park",
                "picnic_ground"
                ],

                ["adventure"] =
                [
                    "hiking_area",
                "cycling_park",
                "off_roading_area",
                "adventure_sports_center",
                "paintball_center",
                "marina"
                ],

                ["relaxation"] =
                [
                    "beach",
                "garden",
                "botanical_garden",
                "scenic_spot",
                "nature_preserve",
                "resort_hotel"
                ]
            };
    public static readonly string[] InterestCategories =
    [
    "attraction",
            "nature",
            "culture",
            "entertainment",
            "food",
            "nightlife",
            "shopping",
            "religious",
            "family",
            "adventure",
            "relaxation"
    ];

    public static readonly string[] DefaultTypes =
    [
        "tourist_attraction",
        "museum",
        "scenic_spot",
        "national_park",
        "restaurant",
        "cafe",
        "hotel",
        "resort_hotel"
    ];
}