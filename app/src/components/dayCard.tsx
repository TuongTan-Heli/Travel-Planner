import { WeatherForecast } from './planner';
import '../styles/dayCard.css';

interface DayCardProps {
  forecast: WeatherForecast;
}

export default function DayCard({ forecast }: DayCardProps) {
  return (
    <article className="forecast-card">
      <h3>{forecast.summary}</h3>
      <p>{forecast.date}</p>
      <p>{forecast.temperatureC} °C</p>
      <p>{forecast.temperatureF} °F</p>
    </article>
  );
}
