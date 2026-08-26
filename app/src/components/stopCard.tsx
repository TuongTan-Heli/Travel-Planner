import type { SelectedStop } from '../models/Itinerary';

interface StopCardProps {
  selectedStop: SelectedStop;
  onStopSelect: (stop: SelectedStop | null) => void;
  onGoBack: () => void;
}

export default function StopCard({ selectedStop, onStopSelect, onGoBack }: StopCardProps) {
  const { place, label, dayNumber } = selectedStop;

  const renderRatingStars = (rating?: number | null) => {
    const safeRating = typeof rating === 'number' && Number.isFinite(rating) ? rating : 0;
    const stars = Array.from({ length: 5 }, (_, index) => {
      const fill = Math.max(0, Math.min(100, (safeRating - index) * 100));
      const percent = Math.max(0, Math.min(100, fill));

      return (
        <span
          key={index}
          className="text-[15px] leading-none"
          style={{ background: `linear-gradient(90deg, #f59e0b 0%, #f59e0b ${percent}%, #cbd5e1 ${percent}%, #cbd5e1 100%)`, WebkitBackgroundClip: 'text', color: 'transparent' }}
        >
          ★
        </span>
      );
    });

    return (
      <div className="flex items-center gap-2">
        <div className="flex gap-1">{stars}</div>
        <span className="text-sm font-bold text-slate-700">{safeRating.toFixed(1)}</span>
      </div>
    );
  };

  return (
    <section className="flex flex-col flex-1 mt-3 p-4 rounded-lg bg-gradient-to-br from-white to-slate-50 shadow-inner">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="text-lg font-semibold text-slate-900">{place.name}</h3>
          {(label || dayNumber !== undefined) && (
            <p className="text-indigo-600 text-sm font-semibold mt-1">
              {dayNumber !== undefined ? `Day ${dayNumber}` : ''}
            </p>
          )}
        </div>

        <button
          type="button"
          onClick={onGoBack}
          className="button rounded-full ">
          ←
        </button>
      </div>

      <div className="flex flex-col gap-2">
        <p className="text-slate-600">{place.address}</p>

        {renderRatingStars(place.rating)}

        <div className="flex flex-wrap items-center gap-2 text-sm text-slate-500">
          {place.userRatingCount !== undefined && place.userRatingCount !== null && (
            <span>{place.userRatingCount} reviews</span>
          )}
          {place.primaryType ? <span>• {place.primaryType}</span> : null}
        </div>

        {place.types?.length ? (
          <div className="flex flex-wrap gap-2">
            {place.types.slice(0, 4).map((type) => (
              <span key={type} className="inline-flex items-center px-2 py-0.5 rounded-full bg-slate-100 text-slate-700 text-xs font-semibold">
                {type}
              </span>
            ))}
          </div>
        ) : null}
        {selectedStop.stop.description ? <span className="text-sm mt-2 text-slate-600">{selectedStop.stop.description}</span> : null}
        {selectedStop.stop.whyVisit ? <span className="text-sm text-slate-600">{selectedStop.stop.whyVisit}</span> : null}

        {place.reviewSummary ? <p className="text-slate-700">{place.reviewSummary}</p> : null}

        <div className="flex flex-col gap-2">
          {place.phoneNumber ? (
            <a className="text-blue-600 hover:underline" href={`tel:${place.phoneNumber}`}>
              📞 {place.phoneNumber}
            </a>) : null}
          {place.websiteUrl ? (
            <a className="text-blue-600 hover:underline" href={place.websiteUrl} target="_blank" rel="noreferrer">
              🌐 Website
            </a>) : null}
        </div>

        {selectedStop.stop.alternatives.length > 0 && (
          <div>
            <h4 className="mt-2 text-sm font-semibold text-slate-700">Alternative places</h4>

            {selectedStop.stop.alternatives.map((alternative) => (
              <div
                key={alternative.placeId}
                role="button"
                onClick={() => {
                  const updatedStop: SelectedStop = {
                    ...selectedStop,
                    place: alternative,
                    label: 'Alternative',
                    stop: selectedStop.stop
                  };

                  onStopSelect(updatedStop);
                }}
                className="button p-2 flex justify-between items-center mt-2 border rounded-lg bg-white hover:bg-slate-50 transform transition hover:-translate-y-1 cursor-pointer"
              >
                <span>{alternative.name}</span>

                <span className="text-xl transition-transform">→</span>
              </div>
            ))}
          </div>
        )}

      </div>
    </section>
  );
}
