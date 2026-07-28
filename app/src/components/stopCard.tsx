import type { Activity, Place } from '../models/itinerary';
import '../styles/tripPlanner.css';

export interface SelectedStop {
  place: Place;
  label?: string;
  dayNumber?: number;
  activity?: Activity;
}

interface StopCardProps {
  selectedStop: SelectedStop;
  onGoBack: () => void;
}

export default function StopCard({ selectedStop, onGoBack }: StopCardProps) {
  const { place, label, dayNumber } = selectedStop;

  const renderRatingStars = (rating?: number | null) => {
    const safeRating = typeof rating === 'number' && Number.isFinite(rating) ? rating : 0;
    const stars = Array.from({ length: 5 }, (_, index) => {
      const fill = Math.max(0, Math.min(100, (safeRating - index) * 100));
      const percent = Math.max(0, Math.min(100, fill));

      return (
        <span
          key={index}
          className="stop-card-star"
          style={{ background: `linear-gradient(90deg, #f59e0b 0%, #f59e0b ${percent}%, #cbd5e1 ${percent}%, #cbd5e1 100%)` }}
        >
          ★
        </span>
      );
    });

    return (
      <div className="stop-card-rating-row">
        <div className="stop-card-stars">{stars}</div>
        <span className="stop-card-rating-value">{safeRating.toFixed(1)}</span>
      </div>
    );
  };

  return (
    <section className="stop-card">
      <div className="stop-card-header">
        <div>
          <h3>{place.name}</h3>
          {(label || dayNumber !== undefined) && (
            <p className="stop-card-subtitle">
              {/* {label ?? 'Stop'} */}
              {dayNumber !== undefined ? `Day ${dayNumber}` : ''}
            </p>
          )}
        </div>
        <button type="button" className="stop-card-back" onClick={onGoBack}>
          ←
        </button>
      </div>

      <div className="stop-card-body">
        <p className="stop-card-address">{place.address}</p>

        {renderRatingStars(place.rating)}

        <div className="stop-card-meta">
          {place.userRatingCount !== undefined && place.userRatingCount !== null && (
            <span>{place.userRatingCount} reviews</span>
          )}

          {place.primaryType ? <span>• {place.primaryType}</span> : null}
        </div>

        {place.types?.length ? (
          <div className="stop-card-tags">
            {place.types.slice(0, 4).map((type) => (
              <span key={type} className="stop-card-tag">
                {type}
              </span>
            ))}
          </div>
        ) : null}

        {place.reviewSummary ? <p className="stop-card-description">{place.reviewSummary}</p> : null}

        <div className="stop-card-links">
          {place.phoneNumber ? (
            <a className="stop-card-link" href={`tel:${place.phoneNumber}`}>
              📞 {place.phoneNumber}
            </a>
          ) : null}
          {place.websiteUrl ? (
            <a className="stop-card-link" href={place.websiteUrl} target="_blank" rel="noreferrer">
              🌐 Website
            </a>
          ) : null}
        </div>
        {selectedStop.activity && selectedStop.activity.alternatives.length > 0 && (
          <div className="stop-card-alternatives">
            <h4>Alternative places</h4>

            {selectedStop.activity.alternatives.map((alternative) => (
              <div
                key={alternative.place.placeId}
                className="stop-card-alternative"
              >
                <h5>
                  {alternative.place.name}
                </h5>

                <p>
                  {alternative.whyVisit}
                </p>

                <span>
                  {alternative.place.primaryType}
                </span>
              </div>
            ))}
          </div>
        )}

      </div>
    </section>
  );
}
