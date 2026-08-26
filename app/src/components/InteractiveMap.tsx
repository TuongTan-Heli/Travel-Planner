import type { Itinerary, SelectedStop } from '../models/Itinerary';
import { useAppSelector } from '../store/Hooks';
import DayMap from './DayMap';

interface InteractiveMapProps {
  selectedStop: SelectedStop | null;
  onStopSelect: (stop: SelectedStop | null) => void;
}

export default function InteractiveMap({ selectedStop, onStopSelect }: InteractiveMapProps) {
  const presentationData = useAppSelector((state) => state.itinerary.presentationData);
  const activeDayIndex = useAppSelector((state) => state.itinerary.activeDayIndex);
  const itinerary = presentationData as Itinerary | null;
  const activeDay = itinerary?.itinerary?.[activeDayIndex] ?? itinerary?.itinerary?.[0];

  return (
      <div className="flex-1 h-full rounded-lg bg-gradient-to-br from-sky-100 to-slate-100 grid place-items-center text-slate-900 overflow-hidden">
        <div className="w-full h-full">
          {activeDay && (<DayMap day={activeDay} selectedStop={selectedStop} onStopSelect={onStopSelect} />)}
          {!activeDay && (
            <div className="flex flex-col items-center justify-center h-full">
              <p className="text-lg font-semibold text-slate-700">Your interactive map will be shown here</p>
            </div>
          )}
        </div>
      </div>
  );
}
