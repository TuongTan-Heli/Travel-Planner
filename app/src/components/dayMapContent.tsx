import { Map, AdvancedMarker, InfoWindow, Pin, useMap, useMapsLibrary, } from '@vis.gl/react-google-maps';
import { useEffect, useState } from 'react';
import type { Activity, Day, Place, SelectedStop, } from '../models/itinerary';
import { useAppSelector } from '../store/hooks';
import * as utils from '../utils'

import '../styles/dayMap.css';
import StopInfo from './StopInfo';

interface DayMapContentProps {
    day: Day;
    selectedStop: SelectedStop | null;
    onStopSelect: (stop: SelectedStop | null) => void;
}

export default function DayMapContent({ day, selectedStop, onStopSelect }: DayMapContentProps) {
    const map = useMap();
    const routesLibrary = useMapsLibrary('routes');
    const activeDayIndex = useAppSelector((state) => state.itinerary.activeDayIndex);

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
        onStopSelect(null);
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

        onStopSelect(stop);
    };



    return (
        <div className="day-map-shell">
            <Map
                mapId={`${day.dayNumber}-${activeDayIndex}`}
                style={{ width: '100%', height: '100%', }}
                defaultCenter={{ lat: day.hotel?.place.location.latitude ?? 0, lng: day.hotel?.place.location.longitude ?? 0, }}
                defaultZoom={10}
                gestureHandling="greedy">
                {!selectedStop && day.hotel && (
                    <AdvancedMarker
                        position={{ lat: day.hotel.place.location.latitude, lng: day.hotel.place.location.longitude, }}
                        onClick={() => {
                            if (!day.hotel) return;

                            const stop = createStop(day.hotel, "Hotel");

                            onStopSelect(stop);
                        }} >
                        <div>
                            <Pin background="#0f9d58"
                                borderColor="#006425"
                                glyphColor="#60d98f" />

                            <button
                                className="day-map-google-button"
                                onClick={(e) => {
                                    e.stopPropagation();
                                    utils.openGoogleMaps(
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

                            onStopSelect(stop);
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
                        onCloseClick={() => { onStopSelect(null) }} >
                        <StopInfo
                            selectedStop={selectedStop}
                            onAlternativeSelect={(place) => {
                                const updatedStop : SelectedStop = {
                                    ...selectedStop,
                                    place,
                                    label: "Alternative",

                                    stop: selectedStop.stop
                                };
                                onStopSelect(updatedStop);
                            }}/>

                    </InfoWindow>
                )}
            </Map>
        </div>
    );
}