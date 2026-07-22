import { useState, type PointerEvent } from 'react';
import '../styles/carousel.css';

const cards = [
  {
    title: 'Local discovery',
    description: 'Browse nearby attractions and weather-friendly routes.',
    accent: 'Sunlit stops',
  },
  {
    title: 'Packing guide',
    description: 'Get suggestions for what to pack based on forecast data.',
    accent: 'Smart prep',
  },
  {
    title: 'Itinerary tip',
    description: 'Plan your next destination with a single click.',
    accent: 'Smooth flow',
  },
];

export default function Carousel() {
  const [activeIndex, setActiveIndex] = useState(0);
  const [dragOffset, setDragOffset] = useState(0);
  const [dragging, setDragging] = useState(false);
  const [startX, setStartX] = useState<number | null>(null);

  const goToSlide = (index: number) => {
    setActiveIndex((index + cards.length) % cards.length);
  };

  const nextSlide = () => goToSlide(activeIndex + 1);
  const prevSlide = () => goToSlide(activeIndex - 1);

  const handlePointerDown = (event: PointerEvent<HTMLDivElement>) => {
    setDragging(true);
    setStartX(event.clientX);
  };

  const handlePointerMove = (event: PointerEvent<HTMLDivElement>) => {
    if (!dragging || startX === null) {
      return;
    }

    setDragOffset(event.clientX - startX);
  };

  const handlePointerUp = () => {
    if (!dragging || startX === null) {
      return;
    }

    if (dragOffset < -50) {
      nextSlide();
    } else if (dragOffset > 50) {
      prevSlide();
    }

    setDragging(false);
    setDragOffset(0);
    setStartX(null);
  };

  return (
    <section className="carousel-panel">
      <div className="carousel-header">
        <div>
          <p className="carousel-eyebrow">Trip highlights</p>
          <h3>Explore your plan</h3>
        </div>
        <div className="carousel-nav" aria-label="Carousel controls">
          <button type="button" onClick={prevSlide} aria-label="Previous slide">
            ←
          </button>
          <button type="button" onClick={nextSlide} aria-label="Next slide">
            →
          </button>
        </div>
      </div>

      <div
        className={`carousel-viewport ${dragging ? 'dragging' : ''}`}
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerUp}
      >
        <div
          className="carousel-track"
          style={{ transform: `translateX(calc(${-activeIndex * 100}% + ${dragOffset}px))` }}
        >
          {cards.map((card) => (
            <article key={card.title} className="carousel-card">
              <p className="carousel-accent">{card.accent}</p>
              <h4>{card.title}</h4>
              <p>{card.description}</p>
            </article>
          ))}
        </div>
      </div>

      <div className="carousel-dots" aria-label="Carousel dots">
        {cards.map((card, index) => (
          <button
            key={card.title}
            type="button"
            className={index === activeIndex ? 'active' : ''}
            onClick={() => goToSlide(index)}
            aria-label={`Go to slide ${index + 1}`}
          />
        ))}
      </div>
    </section>
  );
}
