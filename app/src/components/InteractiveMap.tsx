import '../styles/InteractiveMap.css';
import type { Itinerary } from '../models/itinerary';
import { useAppSelector } from '../store/hooks';
import DayMap from './dayMap';
import { SelectedStop } from './stopCard';

interface InteractiveMapProps {
  onStopSelect?: (stop: SelectedStop | null) => void;
}

export default function InteractiveMap({onStopSelect} : InteractiveMapProps) {
  const presentationData = useAppSelector((state) => state.itinerary.presentationData);
  const activeDayIndex = useAppSelector((state) => state.itinerary.activeDayIndex);
  const itinerary = presentationData as Itinerary | null;

  const activeDay = itinerary?.itinerary?.[activeDayIndex] ?? itinerary?.itinerary?.[0];

  return (
    <div className="map-panel">
      <div className="map-frame">
        <div>
          {activeDay ? (
            <DayMap key={activeDay.dayNumber} day={activeDay} onStopSelect={onStopSelect}/>
          ) : (<div/>)}
        </div>
      </div>
    </div>
  );
}
