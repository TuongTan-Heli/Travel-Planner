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
            <div className="flex items-center gap-2 mb-2">
                <div className="flex gap-1">
                    {Array.from({ length: 5 }, (_, index) => {
                        const fill = Math.max(0, Math.min(100, (safeRating - index) * 100));

                        return (
                            <span
                                key={index}
                                className="text-[15px] leading-none"
                                style={{ background: `linear-gradient(90deg,#f59e0b 0%,#f59e0b ${fill}%,#cbd5e1 ${fill}%,#cbd5e1 100%)`, WebkitBackgroundClip: 'text', color: 'transparent' }}>
                                ★
                            </span>
                        );
                    })}
                </div>

                <span className="text-sm font-bold text-slate-700">{safeRating.toFixed(1)}</span>
            </div>
        );
    };

    return (
        <div className="min-w-[260px] max-w-[320px] p-1 font-sans">
            <h3 className="m-0 mb-1 text-base text-slate-900 font-semibold">{selectedStop.place.name}</h3>

            <p className="m-0 mb-2 text-sm text-slate-500">{selectedStop.place.address}</p>

            {renderRatingStars(selectedStop.place.rating)}
            {selectedStop.place.userRatingCount != 0 && (
                <span className="text-xs text-slate-500">· {selectedStop.place.userRatingCount} reviews</span>
            )}

            {selectedStop.place.types?.length && (
                <div className="flex flex-wrap gap-2 mb-2">
                    {selectedStop.place.types.slice(0, 4).map((type: string) => (
                        <span key={type} className="px-2 py-0.5 rounded-full bg-slate-100 text-slate-700 text-xs font-semibold">{type}</span>
                    ))}
                </div>
            )}

            {selectedStop.stop.description && (
                <p className="text-sm text-slate-600">{selectedStop.stop.description}</p>
            )}

            <button
                className="button inline-flex items-center gap-2 mt-2 px-3 py-2 rounded-full bg-gradient-to-r from-blue-600 to-blue-500 text-white font-bold shadow-md hover:-translate-y-0.5 transition-transform"
                onClick={() => openGoogleMaps(selectedStop.place.location.latitude, selectedStop.place.location.longitude, selectedStop.place.name)}
            >
                🗺️ Open in Google Maps
            </button>

            <div className="flex flex-col gap-1 mt-2">
                {selectedStop.place.phoneNumber && (
                    <a className="text-sm text-blue-600 hover:underline" href={`tel:${selectedStop.place.phoneNumber}`}>📞 {selectedStop.place.phoneNumber}</a>
                )}

                {selectedStop.place.websiteUrl && (
                    <a className="text-sm text-blue-600 hover:underline" href={selectedStop.place.websiteUrl} target="_blank" rel="noreferrer">🌐 Website</a>
                )}
            </div>
        </div>
    )
}