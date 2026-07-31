import { useEffect, useState, type PointerEvent } from 'react';
import { useAppDispatch, useAppSelector } from '../store/hooks';
import { setActiveDayIndex } from '../store/itinerarySlice';
import type { Itinerary } from '../models/itinerary';

interface TripCarouselProps {
  data?: Itinerary | null;
}

interface Slide {
  title: string;
  subtitle: string;
  body: string;
  dayIndex?: number;
}

export default function TripCarousel({ data }: TripCarouselProps) {
  const slides = buildSlides(data);
  const dispatch = useAppDispatch();
  const [activeIndex, setActiveIndex] = useState(0); 
  const [dragOffset, setDragOffset] = useState(0);
  const [dragging, setDragging] = useState(false);
  const [startX, setStartX] = useState<number | null>(null);

  useEffect(() => {
    setDragOffset(0);
  }, [slides.length, dispatch]);

  const goToSlide = (index: number) => {
    if (slides.length <= 1) return;

    const nextIndex = (index + slides.length) % slides.length;
    setActiveIndex(nextIndex);

    const slide = slides[nextIndex];
    if (slide.dayIndex !== undefined) {
      dispatch(setActiveDayIndex(slide.dayIndex));
    }
  };

  const nextSlide = () => goToSlide(activeIndex + 1);
  const prevSlide = () => goToSlide(activeIndex - 1);

  const handlePointerDown = (event: PointerEvent<HTMLDivElement>) => {
    if (slides.length <= 1) return;
    setDragging(true);
    setStartX(event.clientX);
  };

  const handlePointerMove = (event: PointerEvent<HTMLDivElement>) => {
    if (!dragging || startX === null || slides.length <= 1) return;
    setDragOffset(event.clientX - startX);
  };

  const handlePointerUp = () => {
    if (!dragging || startX === null || slides.length <= 1) return;

    if (dragOffset < -50) {
      nextSlide();
    } else if (dragOffset > 50) {
      prevSlide();
    }

    setDragging(false);
    setDragOffset(0);
    setStartX(null);
  };

  if (!slides.length) {
    return null;
  }

  return (
    <section className="trip-carousel">
      <div className="trip-carousel-header">
        <div>
          <p className="trip-carousel-eyebrow">Itinerary flow</p>
          <h3>Explore your plan</h3>
        </div>
        {slides.length > 1 && (
          <div className="trip-carousel-nav" aria-label="Carousel controls">
            <button type="button" onClick={prevSlide} aria-label="Previous slide">
              ←
            </button>
            <button type="button" onClick={nextSlide} aria-label="Next slide">
              →
            </button>
          </div>
        )}
      </div>

      <div
        className={`trip-carousel-viewport ${dragging ? 'dragging' : ''}`}
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerUp}
        onPointerCancel={handlePointerUp}>
        <div
          className="trip-carousel-track"
          style={{ transform: `translateX(calc(${-activeIndex * 100}% + ${dragOffset}px))` }} >
          {slides.map((slide) => (
            <article key={slide.title} className="trip-carousel-card">
              <p className="trip-carousel-accent">{slide.subtitle}</p>
              <h4>{slide.title}</h4>
              <p>{slide.body}</p>
            </article>
          ))}
        </div>
      </div>

      {slides.length > 1 && (
        <div className="trip-carousel-dots" aria-label="Carousel dots">
          {slides.map((slide, index) => (
            <button
              key={slide.title}
              type="button"
              className={index === activeIndex ? 'active' : ''}
              onClick={() => goToSlide(index)}
              aria-label={`Go to slide ${index + 1}`}
            />
          ))}
        </div>
      )}
    </section>
  );
}

function buildSlides(data?: Itinerary | null): Slide[] {
  const slides: Slide[] = [];

  if (!data) {
    return slides;
  }

  if (data.tripSummary) {
    slides.push({
      title: 'Trip Summary',
      subtitle: data.trip.destination ?? 'Overview',
      body: data.tripSummary,
    });
  }

  if (data.generalTips.length) {
    slides.push({
      title: 'Helpful Tips',
      subtitle: 'Planning Notes',
      body: data.generalTips.join(' • '),
    });
  }

  data.itinerary.forEach((day, index) => {
    slides.push({
      title: `Day ${day.dayNumber}`,
      subtitle: day.weather ?? 'Day Plan',
      body: `${day.summary}${day.tips.length ? `\n\nTips: ${day.tips.join(' • ')}` : ''}`,
      dayIndex: index,
    });
  });

  return slides;
}