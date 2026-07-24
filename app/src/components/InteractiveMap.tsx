import '../styles/InteractiveMap.css';
import type { Itinerary } from '../models/itinerary';
import { useAppSelector } from '../store/hooks';
import DayMap from './dayMap';

export default function InteractiveMap() {
  const presentationData = useAppSelector((state) => state.itinerary.presentationData);
  const activeDayIndex = useAppSelector((state) => state.itinerary.activeDayIndex);
  const itinerary = presentationData as Itinerary | null;

  const activeDay = itinerary?.itinerary?.[activeDayIndex] ?? itinerary?.itinerary?.[0];

  return (
    <div className="map-panel">
      <div className="map-frame">
        <div>
          {activeDay ? (
            <DayMap key={activeDay.dayNumber} day={activeDay} />
          ) : (<div/>)}
        </div>
      </div>
    </div>
  );
}
