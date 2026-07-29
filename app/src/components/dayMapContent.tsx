import { Map, AdvancedMarker, InfoWindow, Pin, useMap, useMapsLibrary, } from '@vis.gl/react-google-maps';
import { useEffect, useState } from 'react';
import type { Activity, Day, Place, SelectedStop, } from '../models/itinerary';
import { useAppSelector } from '../store/hooks';

import '../styles/dayMap.css';

interface DayMapContentProps {
    day: Day;
    onStopSelect?: (stop: SelectedStop | null) => void;
}

export default function DayMapContent({ day, onStopSelect }: DayMapContentProps) {
    const map = useMap();
    const routesLibrary = useMapsLibrary('routes');

    const activeDayIndex = useAppSelector((state) => state.itinerary.activeDayIndex);

    const [selectedStop, setSelectedStop] = useState<SelectedStop | null>(null);

    const createStop = (
        activity: Activity,
        label?: string
    ): SelectedStop => ({
        place: activity.place,
        label,
        dayNumber: day.dayNumber,
        stop: activity
    });

    useEffect(() => {
        setSelectedStop(null);
    }, [day.dayNumber]);

    useEffect(() => {
        if (!map || !routesLibrary) return;
        if (!day.activities.length) return;

        const service = new routesLibrary.DirectionsService();

        const renderer = new routesLibrary.DirectionsRenderer({ map, suppressMarkers: true, });

        const origin = day.hotel ? { lat: day.hotel.place.location.latitude, lng: day.hotel.place.location.longitude, }
            : { lat: day.activities[0].place.location.latitude, lng: day.activities[0].place.location.longitude, };

        const destination = { lat: day.activities.at(-1)!.place.location.latitude, lng: day.activities.at(-1)!.place.location.longitude, };

        const waypoints = day.activities
            .slice(0, -1)
            .map((activity) => ({
                location: { lat: activity.place.location.latitude, lng: activity.place.location.longitude, },
                stopover: true,
            }));

        service.route(
            {
                origin,
                destination,
                waypoints,
                travelMode: 'DRIVING',
            },
            (result, status) => {
                if (status === 'OK' && result) {
                    renderer.setDirections(result);
                }
            }
        );

        return () => renderer.setMap(null);
    }, [map, routesLibrary, day.dayNumber]);

    const activities = day.activities ?? [];

    const handleMarkerSelect = (activity: Activity) => {
        const stop: SelectedStop = {
            place: activity.place,
            label: activity.type,
            dayNumber: day.dayNumber,
            stop: activity
        };

        setSelectedStop(stop);
        onStopSelect?.(stop);
    };

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

    const openGoogleMaps = (
        lat: number,
        lon: number,
        name?: string
    ) => {
        window.open(
            `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(name ?? `${lat},${lon}`)}`,
            '_blank',
            'noopener,noreferrer'
        );
    };

    return (
        <div className="day-map-shell">
            <Map
                mapId={`${day.dayNumber}-${activeDayIndex}`}
                style={{ width: '100%', height: '500px', }}
                defaultCenter={{ lat: day.hotel?.place.location.latitude ?? 0, lng: day.hotel?.place.location.longitude ?? 0, }}
                defaultZoom={10}
                gestureHandling="greedy">
                {!selectedStop && day.hotel && (
                    <AdvancedMarker
                        position={{ lat: day.hotel.place.location.latitude, lng: day.hotel.place.location.longitude, }}
                        onClick={() => {
                            if (!day.hotel) return;

                            const stop = createStop(day.hotel, "Hotel");

                            setSelectedStop(stop);
                            onStopSelect?.(stop);
                        }} >
                        <div>
                            <Pin background="#0f9d58"
                                borderColor="#006425"
                                glyphColor="#60d98f" />

                            <button
                                className="day-map-google-button"
                                onClick={(e) => {
                                    e.stopPropagation();
                                    openGoogleMaps(
                                        day.hotel!.place.location.latitude,
                                        day.hotel!.place.location.longitude,
                                        day.hotel!.place.name);
                                }} >
                                🗺️ Open in Google Maps
                            </button>
                        </div>
                    </AdvancedMarker>
                )}
                {selectedStop === null && activities.map((activity, index) => (
                    <AdvancedMarker
                        key={`${activity.place.placeId}-${index}`}
                        position={{
                            lat: activity.place.location.latitude,
                            lng: activity.place.location.longitude
                        }}
                        onClick={() => {
                            handleMarkerSelect(activity);
                        }}
                    >
                        <Pin
                            background="#4285F4"
                            borderColor="#1a73e8"
                            glyphColor="#fff"
                        />
                    </AdvancedMarker>

                ))}

                {selectedStop && (
                    <AdvancedMarker
                        position={{
                            lat: selectedStop.place.location.latitude,
                            lng: selectedStop.place.location.longitude
                        }} >
                        <Pin
                            background="#ea4335"
                            borderColor="#b31412"
                            glyphColor="#fff"
                        />
                    </AdvancedMarker>
                )}

                {selectedStop?.stop.alternatives.map((alternative: Place) => (
                    <AdvancedMarker
                        key={alternative.placeId}
                        position={{
                            lat: alternative.location.latitude,
                            lng: alternative.location.longitude
                        }}
                        onClick={() => {
                            const stop: SelectedStop = {
                                place: alternative,
                                label: "Alternative",
                                dayNumber: day.dayNumber,

                                // keep original alternatives
                                stop: selectedStop.stop
                            };

                            setSelectedStop(stop);
                            onStopSelect?.(stop);
                        }} >
                        <Pin
                            background="#4285F4"
                            borderColor="#1a73e8"
                            glyphColor="#fff"
                        />
                    </AdvancedMarker>
                ))}

                {selectedStop && (
                    <InfoWindow
                        position={{ lat: selectedStop.place.location.latitude, lng: selectedStop.place.location.longitude, }}
                        onCloseClick={() => { setSelectedStop(null); onStopSelect?.(null) }} >
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
                    </InfoWindow>
                )}
            </Map>
        </div>
    );
}