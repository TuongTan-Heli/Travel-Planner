import { APIProvider } from '@vis.gl/react-google-maps';
import type { Day, SelectedStop } from '../models/Itinerary';
import DayMapContent from './DayMapContent';
import { useState } from 'react';

const apiKey = process.env.MAP_JS_API_KEY || '';

interface DayMapProps {
  day: Day;
  selectedStop: SelectedStop | null;
  onStopSelect: (stop: SelectedStop | null) => void;
}

export default function DayMap({ day, selectedStop, onStopSelect }: DayMapProps) {
  return (
    <APIProvider apiKey={apiKey}>
      <DayMapContent day={day} onStopSelect={onStopSelect} selectedStop={selectedStop} />
    </APIProvider>
  );
}