import Chat from './chat';
import InteractiveMap from './InteractiveMap';
import '../styles/tripPlanner.css';
import { useAppSelector } from '../store/hooks';
import TripCarousel from './tripCarousel';
import type { Itinerary, SelectedStop } from '../models/itinerary';
import { useState } from 'react';
import StopCard from './stopCard';

export default function TripPlanner() {
  const presentationData = useAppSelector((state) => state.itinerary.presentationData);
  const systemState = useAppSelector(state => state.system);
  const itinerary = presentationData as Itinerary | null;
  const [selectedStop, setSelectedStop] = useState<SelectedStop | null>(null);

  return (
    <section className="planner-root">
      <div className="planner-top">
        <div className="planner-panel">
          <h2>Itinerary Preview</h2>
          <h1>
            {
              systemState.processing && (
                <div className="ai-status">
                  <span className="spinner" />
                  {systemState.message}
                </div>
              )
            }
          </h1>
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
