export interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

export async function fetchWeatherForecast(): Promise<WeatherForecast[]> {
  const response = await fetch('/WeatherForecast');

  if (!response.ok) {
    throw new Error('Unable to load weather forecast from the backend.');
  }

  return response.json();
}
