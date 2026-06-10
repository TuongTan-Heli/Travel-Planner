import { useEffect, useState } from 'react';
import { fetchWeatherForecast, WeatherForecast } from './planner';
import Carousel from './carousel';
import Chat from './chat';
import DayCard from './dayCard';
import ErrorMessage from './error';
import InteractiveMap from './InteractiveMap';
import LoadingScreen from './LoadingScreen';
import '../styles/tripPlanner.css';

export default function TripPlanner() {
  const [forecast, setForecast] = useState<WeatherForecast[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchWeatherForecast()
      .then((data) => setForecast(data))
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  return (
    <section className="planner-root">
      <div className="planner-top">
        <div className="planner-panel">
          <h2>Weather & route summary</h2>
          {loading && <LoadingScreen message="Loading weather forecast..." />}
          {error && <ErrorMessage message={error} />}
          {!loading && !error && (
            <div className="forecast-grid">
              {forecast.map((item) => (
                <DayCard key={item.date} forecast={item} />
              ))}
            </div>
          )}
        </div>
        <InteractiveMap />
      </div>
      <div className="planner-bottom">
        <Carousel />
        <Chat />
      </div>
    </section>
  );
}
