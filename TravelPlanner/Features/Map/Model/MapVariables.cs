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

    public static string GoogleMapFieldMaskLocations =
            "places.displayName," +
            "places.formattedAddress," +
            "places.location," +
            "places.rating," +
            "places.userRatingCount";
            
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

    public static readonly string[] PreferenceCategories =
    [
            "Review",
            "Cheap",
            "Luxury",
            "Convenient",
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

    public static readonly Dictionary<string, double> TypeTravelDuration =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Hotels
            ["hotel"] = 0,
            ["resort_hotel"] = 0,
            ["campground"] = 0,

            // Food & Drink
            ["restaurant"] = 1.5,
            ["breakfast_restaurant"] = 1.0,
            ["brunch_restaurant"] = 1.5,
            ["fine_dining_restaurant"] = 2.0,
            ["cafe"] = 1.0,
            ["coffee_shop"] = 1.0,
            ["bakery"] = 0.5,
            ["dessert_shop"] = 0.5,
            ["ice_cream_shop"] = 0.5,

            // Nightlife
            ["bar"] = 2.0,
            ["cocktail_bar"] = 2.0,
            ["lounge_bar"] = 2.0,
            ["night_club"] = 3.0,
            ["karaoke"] = 2.5,
            ["brewery"] = 2.0,
            ["beer_garden"] = 2.0,
            ["live_music_venue"] = 2.5,

            // Museums & Culture
            ["museum"] = 2.5,
            ["history_museum"] = 2.5,
            ["art_gallery"] = 2.0,
            ["art_museum"] = 2.5,
            ["cultural_center"] = 2.0,
            ["cultural_landmark"] = 1.5,
            ["castle"] = 2.5,
            ["monument"] = 1.0,
            ["historical_place"] = 2.0,
            ["historical_landmark"] = 1.5,
            ["visitor_center"] = 1.0,
            ["observation_deck"] = 1.0,
            ["plaza"] = 1.0,
            ["performing_arts_theater"] = 3.0,
            ["opera_house"] = 3.0,
            ["auditorium"] = 2.5,

            // Nature
            ["beach"] = 4.0,
            ["lake"] = 2.5,
            ["river"] = 2.0,
            ["garden"] = 2.0,
            ["botanical_garden"] = 2.5,
            ["woods"] = 2.5,
            ["nature_preserve"] = 4.0,
            ["national_park"] = 5.0,
            ["state_park"] = 4.0,
            ["mountain_peak"] = 4.5,
            ["scenic_spot"] = 1.5,
            ["wildlife_refuge"] = 3.5,
            ["wildlife_park"] = 3.0,
            ["city_park"] = 2.0,
            ["picnic_ground"] = 2.0,

            // Entertainment
            ["aquarium"] = 3.0,
            ["zoo"] = 4.0,
            ["amusement_park"] = 6.0,
            ["water_park"] = 5.0,
            ["amusement_center"] = 2.5,
            ["movie_theater"] = 2.5,
            ["concert_hall"] = 3.0,
            ["planetarium"] = 2.0,
            ["video_arcade"] = 1.5,
            ["event_venue"] = 3.0,
            ["ferris_wheel"] = 1.0,
            ["bowling_alley"] = 2.0,

            // Shopping
            ["shopping_mall"] = 3.0,
            ["department_store"] = 2.0,
            ["market"] = 2.0,
            ["farmers_market"] = 1.5,
            ["gift_shop"] = 0.75,
            ["book_store"] = 1.0,
            ["clothing_store"] = 1.5,
            ["jewelry_store"] = 1.0,
            ["toy_store"] = 1.0,

            // Religion
            ["church"] = 1.0,
            ["buddhist_temple"] = 1.5,
            ["mosque"] = 1.0,
            ["synagogue"] = 1.0,
            ["shinto_shrine"] = 1.5,
            ["hindu_temple"] = 1.5,

            // Adventure
            ["hiking_area"] = 4.0,
            ["cycling_park"] = 3.0,
            ["off_roading_area"] = 4.0,
            ["adventure_sports_center"] = 3.5,
            ["paintball_center"] = 3.0,
            ["marina"] = 2.0,

            // Generic fallback
            ["tourist_attraction"] = 2.0,
        };
}