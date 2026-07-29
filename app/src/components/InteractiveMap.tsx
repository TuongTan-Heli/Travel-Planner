import '../styles/InteractiveMap.css';
import type { Itinerary, SelectedStop } from '../models/itinerary';
import { useAppSelector } from '../store/hooks';
import DayMap from './dayMap';

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
            <DayMap day={activeDay} onStopSelect={onStopSelect}/>
          ) : (<div/>)}
        </div>
      </div>
    </div>
  );
}
