import Chat from './chat';
import InteractiveMap from './InteractiveMap';
import '../styles/tripPlanner.css';
import { useAppSelector } from '../store/hooks';
import TripCarousel from './tripCarousel';
import type { Itinerary } from '../models/itinerary';
import type { SelectedStop } from './stopCard';
import { useEffect, useState } from 'react';
import StopCard from './stopCard';

export default function TripPlanner() {
  const presentationData = useAppSelector((state) => state.itinerary.presentationData);
  const itinerary = presentationData as Itinerary | null;
  const [selectedStop, setSelectedStop] = useState<SelectedStop | null>(null);

  return (
    <section className="planner-root">
      <div className="planner-top">
        <div className="planner-panel">
          <h2>Itinerary Preview</h2>
          {selectedStop ? (
            <StopCard
              selectedStop={selectedStop}
              onGoBack={() => setSelectedStop(null)}
            />
          ) : itinerary ? (
            <TripCarousel data={itinerary} />
          ) : (
            <p>No itinerary data yet. Send a message in chat to load the preview.</p>
          )}
        </div>
        <InteractiveMap onStopSelect={setSelectedStop} />
      </div>
      <div className="planner-bottom">
        <Chat />
      </div>
    </section>
  );
}
