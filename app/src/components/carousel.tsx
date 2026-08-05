import { useState, type PointerEvent } from 'react';

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
    <section className="flex flex-col gap-3 p-4 rounded-xl border border-slate-200 bg-gradient-to-br from-white to-slate-50 shadow-md">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs font-extrabold uppercase tracking-wider text-indigo-600">Trip highlights</p>
          <h3 className="text-lg font-semibold text-slate-900">Explore your plan</h3>
        </div>
        <div className="flex gap-2">
          <button type="button" onClick={prevSlide} aria-label="Previous slide" className="w-9 h-9 rounded-full bg-white text-slate-700 shadow">←</button>
          <button type="button" onClick={nextSlide} aria-label="Next slide" className="w-9 h-9 rounded-full bg-white text-slate-700 shadow">→</button>
        </div>
      </div>

      <div
        className={`overflow-hidden rounded-lg touch-pan-y select-none ${dragging ? 'cursor-grabbing' : ''}`}
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerUp}
      >
        <div
          className="flex transition-transform duration-300 ease-in-out will-change-transform"
          style={{ transform: `translateX(calc(${-activeIndex * 100}% + ${dragOffset}px))` }}
        >
          {cards.map((card) => (
            <article key={card.title} className="flex-0 min-h-[170px] w-full p-4 rounded-lg bg-gradient-to-br from-white to-violet-50 shadow-inner flex flex-col justify-center gap-2">
              <p className="text-xs font-bold uppercase tracking-wide text-violet-600">{card.accent}</p>
              <h4 className="text-base font-semibold text-slate-900">{card.title}</h4>
              <p className="text-sm text-slate-600">{card.description}</p>
            </article>
          ))}
        </div>
      </div>

      <div className="flex justify-center gap-2">
        {cards.map((card, index) => (
          <button
            key={card.title}
            type="button"
            className={`${index === activeIndex ? 'bg-indigo-600 scale-105' : 'bg-slate-300'} w-2.5 h-2.5 rounded-full transition-transform`}
            onClick={() => goToSlide(index)}
            aria-label={`Go to slide ${index + 1}`}
          />
        ))}
      </div>
    </section>
  );
}
