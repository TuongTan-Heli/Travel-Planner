import { APIProvider, Map, AdvancedMarker, InfoWindow, Pin } from '@vis.gl/react-google-maps';
import type { Activity, Day, Place } from '../models/itinerary';
import { useEffect, useState } from 'react';
import { useAppSelector } from '../store/hooks';
import '../styles/dayMap.css';

const apiKey = process.env.REACT_APP_MAP_JS_API_KEY || '';

export default function DayMap({ day }: { day: Day }) {
  const activeDayIndex = useAppSelector((state) => state.itinerary.activeDayIndex);
  const [selectedActivity, setSelectedActivity] = useState<Place | null>(null);
  const [activeActivityIndex, setActiveActivityIndex] = useState(0);

  useEffect(() => {
    setSelectedActivity(null);
    setActiveActivityIndex(0);
  }, [day.dayNumber]);

  const activities = day.activities ?? [];
  const currentActivity = activities[activeActivityIndex];

  const goToActivity = (nextIndex: number) => {
    if (!activities.length) return;

    const clampedIndex = (nextIndex + activities.length) % activities.length;
    setActiveActivityIndex(clampedIndex);
    setSelectedActivity(activities[clampedIndex].place);
  };

  const handleMarkerSelect = (activity: Activity, index: number) => {
    setActiveActivityIndex(index);
    setSelectedActivity(activity.place);
  };

  const renderRatingStars = (rating?: number | null) => {
    const safeRating = typeof rating === 'number' && Number.isFinite(rating) ? rating : 0;
    const stars = Array.from({ length: 5 }, (_, index) => {
      const fill = Math.max(0, Math.min(100, (safeRating - index) * 100));
      const percent = Math.max(0, Math.min(100, fill));

      return (
        <span
          key={index}
          className="day-map-star"
          style={{ background: `linear-gradient(90deg, #f59e0b 0%, #f59e0b ${percent}%, #cbd5e1 ${percent}%, #cbd5e1 100%)` }}
        >
          ★
        </span>
      );
    });

    return (
      <div className="day-map-rating-row">
        <div className="day-map-stars">{stars}</div>
        <span className="day-map-rating-value">{safeRating.toFixed(1)}</span>
      </div>
    );
  };

  const openGoogleMaps = (lat: number, lon: number, name?: string) => {
    window.open(
      `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(name || `${lat},${lon}`)}`,
      '_blank',
      'noopener,noreferrer'
    );
  };

  return (
    <APIProvider apiKey={apiKey}>
      <div className="day-map-shell">
        <Map
          mapId={`${day.dayNumber}-${activeDayIndex}`}
          style={{ width: '100%', height: '500px' }}
          defaultCenter={{ lat: day.hotel?.location.latitude ?? 0, lng: day.hotel?.location.longitude ?? 0 }}
          defaultZoom={10}
          gestureHandling={'greedy'}
          disableDefaultUI={false}
        >
          {day.hotel && (() => {
            const lat = day.hotel.location.latitude;
            const lon = day.hotel.location.longitude;

            return (
              <AdvancedMarker
                position={{ lat, lng: lon }}
                onClick={() => {
                  setSelectedActivity(day.hotel);
                }}
              >
                <div>
                  <Pin
                    background="#0f9d58"
                    borderColor="#006425"
                    glyphColor="#60d98f"/>

                  <button
                    type="button"
                    className="day-map-google-button"
                    onClick={(e) => {
                      e.stopPropagation();
                      openGoogleMaps(lat, lon, day.hotel?.name);
                    }}
                  >
                    <span className="day-map-google-button-icon">🗺️</span>
                    <span>Open in Google Maps</span>
                  </button>
                </div>
              </AdvancedMarker>
            );
          })()}

          {activities.map((activity, index) => {
            const lat = activity.place.location.latitude;
            const lon = activity.place.location.longitude;
            return (
              <AdvancedMarker
                key={`${activity.place.placeId}-${index}`}
                position={{ lat, lng: lon }}
                onClick={() => handleMarkerSelect(activity, index)}
              />
            );
          })}

          {selectedActivity && (
            <InfoWindow
              position={{ lat: selectedActivity.location.latitude, lng: selectedActivity.location.longitude }}
              onCloseClick={() => {
                setSelectedActivity(null);
              }}
            >
              <div className="day-map-info-window">
                <div className="day-map-info-header">
                  <div>
                    <h3 className="day-map-title">{selectedActivity.name}</h3>
                    <p className="day-map-address">{selectedActivity.address}</p>
                  </div>
                </div>

                <div className="day-map-rating-row">
                  {renderRatingStars(selectedActivity.rating)}
                  <span className="day-map-meta">· {selectedActivity.userRatingCount} reviews</span>
                </div>

                {selectedActivity.types?.length ? (
                  <div className="day-map-tags">
                    {selectedActivity.types.slice(0, 4).map((type) => (
                      <span key={type} className="day-map-tag">
                        {type}
                      </span>
                    ))}
                  </div>
                ) : null}

                {selectedActivity.description ? (
                  <p className="day-map-description">{selectedActivity.description}</p>
                ) : null}

                <button
                  type="button"
                  className="day-map-google-button"
                  onClick={(e) => {
                    e.stopPropagation();
                    openGoogleMaps(selectedActivity.location.latitude, selectedActivity.location.longitude, selectedActivity.name);
                  }}
                >
                  <span className="day-map-google-button-icon">🗺️</span>
                  <span>Open in Google Maps</span>
                </button>

                <div className="day-map-links">
                  {selectedActivity.phoneNumber ? (
                    <a className="day-map-link" href={`tel:${selectedActivity.phoneNumber}`}>
                      📞 {selectedActivity.phoneNumber}
                    </a>
                  ) : null}
                  {selectedActivity.websiteUrl ? (
                    <a className="day-map-link" href={selectedActivity.websiteUrl} target="_blank" rel="noreferrer">
                      🌐 {selectedActivity.websiteUrl}
                    </a>
                  ) : null}
                </div>
              </div>
            </InfoWindow>
          )}
        </Map>

        {/* {activities.length > 0 && (
          <div className="day-map-activity-bar">
            <button type="button" className="day-map-activity-button" onClick={() => goToActivity(activeActivityIndex - 1)} aria-label="Previous activity">
              ←
            </button>
            <div className="day-map-activity-content">
              <div className="day-map-activity-title">{currentActivity?.place.name ?? 'No activity'}</div>
              <div className="day-map-activity-subtitle">
                {currentActivity?.type ?? ''} · {currentActivity?.place.address ?? ''}
              </div>
            </div>
            <button type="button" className="day-map-activity-button" onClick={() => goToActivity(activeActivityIndex + 1)} aria-label="Next activity">
              →
            </button>
          </div>
        )} */}
      </div>
    </APIProvider>
  );
}