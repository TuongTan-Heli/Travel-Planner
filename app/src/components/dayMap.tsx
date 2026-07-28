import { APIProvider } from '@vis.gl/react-google-maps';
import type { Day } from '../models/itinerary';
import DayMapContent from '../components/dayMapContent';
import type { SelectedStop } from './stopCard';

import '../styles/dayMap.css';

const apiKey = process.env.REACT_APP_MAP_JS_API_KEY || '';

interface DayMapProps {
  day: Day;
  onStopSelect?: (stop: SelectedStop | null) => void;
}

export default function DayMap({ day, onStopSelect }: DayMapProps) {
  return (
    <APIProvider apiKey={apiKey}>
      <DayMapContent day={day} onStopSelect={onStopSelect} />
    </APIProvider>
  );
}