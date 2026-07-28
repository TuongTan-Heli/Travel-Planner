import { Map, AdvancedMarker, InfoWindow, Pin, useMap, useMapsLibrary, } from '@vis.gl/react-google-maps';
import { useEffect, useState } from 'react';
import type { Activity, Day, Place, } from '../models/itinerary';
import { useAppSelector } from '../store/hooks';
import type { SelectedStop } from './stopCard';

import '../styles/dayMap.css';

interface DayMapContentProps {
    day: Day;
    onStopSelect?: (stop: SelectedStop | null) => void;
}

export default function DayMapContent({ day, onStopSelect }: DayMapContentProps) {
    const map = useMap();
    const routesLibrary = useMapsLibrary('routes');

    const activeDayIndex = useAppSelector((state) => state.itinerary.activeDayIndex);

    const [selectedActivity, setSelectedActivity] = useState<Place | null>(null);

    useEffect(() => {
        setSelectedActivity(null);
    }, [day.dayNumber]);

    useEffect(() => {
        if (!map || !routesLibrary) return;
        if (!day.activities.length) return;

        const service = new routesLibrary.DirectionsService();

        const renderer = new routesLibrary.DirectionsRenderer({ map, suppressMarkers: true, });

        const origin = day.hotel ? { lat: day.hotel.location.latitude, lng: day.hotel.location.longitude, }
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
    }, [map, routesLibrary, day]);

    const activities = day.activities ?? [];

    const handleMarkerSelect = (activity: Activity) => {
        setSelectedActivity(activity.place);
        onStopSelect?.({ place: activity.place, label: activity.type, dayNumber: day.dayNumber, activity: activity });
    };

    const renderRatingStars = (rating?: number | null) => {
        const safeRating =
            typeof rating === 'number' && Number.isFinite(rating) ? rating : 0;

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
                defaultCenter={{ lat: day.hotel?.location.latitude ?? 0, lng: day.hotel?.location.longitude ?? 0, }}
                defaultZoom={10}
                gestureHandling="greedy">
                {day.hotel && (
                    <AdvancedMarker
                        position={{ lat: day.hotel.location.latitude, lng: day.hotel.location.longitude, }}
                        onClick={() => {
                            if (!day.hotel) return;
                            setSelectedActivity(day.hotel);
                            onStopSelect?.({ place: day.hotel, label: 'Hotel', dayNumber: day.dayNumber });
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
                                        day.hotel!.location.latitude,
                                        day.hotel!.location.longitude,
                                        day.hotel!.name);
                                }} >
                                🗺️ Open in Google Maps
                            </button>
                        </div>
                    </AdvancedMarker>
                )}

                {activities.map((activity, index) => (
                    <AdvancedMarker
                        key={`${activity.place.placeId}-${index}`}
                        position={{ lat: activity.place.location.latitude, lng: activity.place.location.longitude, }}
                        onClick={() => handleMarkerSelect(activity)} />
                ))}

                {selectedActivity && (
                    <InfoWindow
                        position={{ lat: selectedActivity.location.latitude, lng: selectedActivity.location.longitude, }}
                        onCloseClick={() => { setSelectedActivity(null); onStopSelect?.(null) }} >
                        <div className="day-map-info-window">
                            <h3 className="day-map-title">
                                {selectedActivity.name}
                            </h3>

                            <p className="day-map-address">
                                {selectedActivity.address}
                            </p>

                            {renderRatingStars(selectedActivity.rating)}
                            {
                                selectedActivity.userRatingCount != 0 &&
                                <span className="day-map-meta">
                                    · {selectedActivity.userRatingCount} reviews
                                </span>
                            }


                            {!!selectedActivity.types?.length && (
                                <div className="day-map-tags">
                                    {selectedActivity.types
                                        .slice(0, 4)
                                        .map((type) => (
                                            <span key={type} className="day-map-tag" >
                                                {type}
                                            </span>
                                        ))}
                                </div>
                            )}

                            {selectedActivity.description && (
                                <p className="day-map-description">
                                    {selectedActivity.description}
                                </p>
                            )}

                            <button
                                className="day-map-google-button"
                                onClick={() => openGoogleMaps(
                                    selectedActivity.location.latitude,
                                    selectedActivity.location.longitude,
                                    selectedActivity.name)}>
                                🗺️ Open in Google Maps
                            </button>

                            <div className="day-map-links">
                                {selectedActivity.phoneNumber && (
                                    <a className="day-map-link" href={`tel:${selectedActivity.phoneNumber}`} >
                                        📞 {selectedActivity.phoneNumber}
                                    </a>
                                )}

                                {selectedActivity.websiteUrl && (
                                    <a className="day-map-link"
                                        href={selectedActivity.websiteUrl}
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