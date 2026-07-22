import Chat from './chat';
import InteractiveMap from './InteractiveMap';
import '../styles/tripPlanner.css';
import { useAppSelector } from '../store/hooks';
import TripCarousel from './tripCarousel';
import type { Itinerary } from '../models/itinerary';

export default function TripPlanner() {
  const presentationData = useAppSelector((state) => state.itinerary.presentationData);
  const itinerary = presentationData as Itinerary | null;

  return (
    <section className="planner-root">
      <div className="planner-top">
        <div className="planner-panel">
          <h2>Itinerary Preview</h2>
          {itinerary ? (
            <TripCarousel data={itinerary} />
          ) : (
            <p>No itinerary data yet. Send a message in chat to load the preview.</p>
          )}
        </div>
        <InteractiveMap />
      </div>
      <div className="planner-bottom">
        <Chat />
      </div>
    </section>
  );
}
