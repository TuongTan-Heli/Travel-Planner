import '../styles/carousel.css';

const cards = [
  {
    title: 'Local discovery',
    description: 'Browse nearby attractions and weather-friendly routes.',
  },
  {
    title: 'Packing guide',
    description: 'Get suggestions for what to pack based on forecast data.',
  },
  {
    title: 'Itinerary tip',
    description: 'Plan your next destination with a single click.',
  },
];

export default function Carousel() {
  return (
    <section className="carousel-panel">
      <h3>Carousel</h3>
      <ul>
        {cards.map((card) => (
          <li key={card.title} className="carousel-card">
            <h4>{card.title}</h4>
            <p>{card.description}</p>
          </li>
        ))}
      </ul>
    </section>
  );
}
