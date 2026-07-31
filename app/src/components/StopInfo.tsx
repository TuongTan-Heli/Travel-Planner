import { Place, SelectedStop } from "../models/itinerary";
import { openGoogleMaps } from "../utils";

interface StopInfoProps {
    selectedStop: SelectedStop;
    onAlternativeSelect: (place: Place) => void;
}

export default function StopInfo({ selectedStop, onAlternativeSelect }: StopInfoProps) {
    const renderRatingStars = (rating?: number | null) => {
        const safeRating = typeof rating === 'number' && Number.isFinite(rating) ? rating : 0;

        return (
            <div className="day-map-rating-row">
                <div className="day-map-stars">
                    {Array.from({ length: 5 }, (_, index) => {
                        const fill = Math.max(0, Math.min(100, (safeRating - index) * 100));

                        return (
                            <span
                                key={index}
                                className="day-map-star"
                                style={{ background: `linear-gradient( 90deg,   #f59e0b 0%,  #f59e0b ${fill}%,   #cbd5e1 ${fill}%,  #cbd5e1 100% )`, }}   >
                                ★
                            </span>
                        );
                    })}
                </div>

                <span className="day-map-rating-value">
                    {safeRating.toFixed(1)}
                </span>
            </div>
        );
    };

    return (
        <div className="day-map-info-window">
            <h3 className="day-map-title">
                {selectedStop.place.name}
            </h3>

            <p className="day-map-address">
                {selectedStop.place.address}
            </p>

            {renderRatingStars(selectedStop.place.rating)}
            {
                selectedStop.place.userRatingCount != 0 &&
                <span className="day-map-meta">
                    · {selectedStop.place.userRatingCount} reviews
                </span>
            }


            {selectedStop.place.types?.length && (
                <div className="day-map-tags">
                    {selectedStop.place.types
                        .slice(0, 4)
                        .map((type: string) => (
                            <span key={type} className="day-map-tag" >
                                {type}
                            </span>
                        ))}
                </div>
            )}

            {selectedStop.place.description && (
                <p className="day-map-description">
                    {selectedStop.place.description}
                </p>
            )}

            <button
                className="day-map-google-button"
                onClick={() => openGoogleMaps(
                    selectedStop.place.location.latitude,
                    selectedStop.place.location.longitude,
                    selectedStop.place.name)}>
                🗺️ Open in Google Maps
            </button>

            <div className="day-map-links">
                {selectedStop.place.phoneNumber && (
                    <a className="day-map-link" href={`tel:${selectedStop.place.phoneNumber}`} >
                        📞 {selectedStop.place.phoneNumber}
                    </a>
                )}

                {selectedStop.place.websiteUrl && (
                    <a className="day-map-link"
                        href={selectedStop.place.websiteUrl}
                        target="_blank"
                        rel="noreferrer" >
                        🌐 Website
                    </a>
                )}
            </div>
        </div>


    )
}