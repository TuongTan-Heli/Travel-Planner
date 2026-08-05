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
    <section className="flex flex-col gap-3 mt-3 flex-1">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs font-extrabold uppercase tracking-wider text-indigo-600">Itinerary flow</p>
          <h3 className="text-lg font-semibold text-slate-900">Explore your plan</h3>
        </div>
        {slides.length > 1 && (
          <div className="flex gap-2" aria-label="Carousel controls">
            <button type="button" onClick={prevSlide} aria-label="Previous slide" className="w-9 h-9 rounded-full bg-white text-slate-700 shadow">←</button>
            <button type="button" onClick={nextSlide} aria-label="Next slide" className="w-9 h-9 rounded-full bg-white text-slate-700 shadow">→</button>
          </div>
        )}
      </div>

      <div
        className={`overflow-hidden rounded-lg touch-pan-y select-none ${dragging ? 'cursor-grabbing' : ''}`}
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerUp}
        onPointerCancel={handlePointerUp}>
        <div
          className="flex transition-transform duration-300 ease-in-out will-change-transform"
          style={{ transform: `translateX(calc(${-activeIndex * 100}% + ${dragOffset}px))` }} >
          {slides.map((slide) => (
            <article key={slide.title} className="flex-0 min-h-[170px] w-full p-4 rounded-lg bg-gradient-to-br from-white to-violet-50 shadow-inner flex flex-col justify-center gap-2">
              <p className="text-xs font-bold uppercase tracking-wide text-violet-600">{slide.subtitle}</p>
              <h4 className="text-base font-semibold text-slate-900">{slide.title}</h4>
              <p className="text-sm text-slate-600 whitespace-pre-line">{slide.body}</p>
            </article>
          ))}
        </div>
      </div>

      {slides.length > 1 && (
        <div className="flex justify-center gap-2" aria-label="Carousel dots">
          {slides.map((slide, index) => (
            <button
              key={slide.title}
              type="button"
              className={`${index === activeIndex ? 'bg-indigo-600 scale-105' : 'bg-slate-300'} w-2.5 h-2.5 rounded-full transition-transform`}
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