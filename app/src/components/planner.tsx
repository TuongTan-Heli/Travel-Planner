import { useEffect, useState } from "react";
import Select from "react-select";

export interface PlannerRequest {
  destination: string;
  budget: number;
  currency: string;
  startDate?: string;
  endDate?: string;
  days?: number;
  travelers?: number;
  rating?: number;
  interests: string[];
  frequency?: number;
  haveTimeRange: boolean;
}

interface PlannerProps {
  onSubmit: (request: PlannerRequest) => void;
}

export const INTEREST_OPTIONS = [
  { value: "food", label: "🍜 Food & Restaurants" },
  { value: "culture", label: "🏯 Culture & History" },
  { value: "nature", label: "🌲 Nature & Hiking" },
  { value: "shopping", label: "🛍 Shopping" },
  { value: "nightlife", label: "🍸 Nightlife" },
  { value: "beaches", label: "🏖 Beaches" },
  { value: "adventure", label: "🧗 Adventure" },
  { value: "photography", label: "📸 Photography" },
  { value: "family", label: "👨‍👩‍👧 Family Activities" },
  { value: "relaxation", label: "🧘 Relaxation" }
];

interface Currency {
  code: string;
  name: string;
}

export default function Planner({ onSubmit }: PlannerProps) {
  const [destination, setDestination] = useState("");
  const [budget, setBudget] = useState<number>(0);
  const [currency, setCurrency] = useState("AUD");
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [haveTimeRange, setHaveTimeRange] = useState(false);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [days, setDays] = useState(1);
  const [rating, setRating] = useState(0);
  const [interests, setInterests] = useState<{ value: string; label: string }[]>([]);

  useEffect(() => {
    loadCurrencies();
  }, []);

  async function loadCurrencies() {
    try {
      await fetch("/api/currency/currencies")
        .then(r => r.json())
        .then(setCurrencies);


    } catch (error) {
      console.error("Failed loading currencies", error);
    }
  }

  const validateForm = () => {
    if (!budget) {
      alert("Budget is required");
      return false;
    }

    if (haveTimeRange) {
      if (!startDate || !endDate) {
        alert(
          "Please select date range"
        );
        return false;
      }
    }

    else {
      if (!days) {
        alert(
          "Number of days required"
        );
        return false;
      }
    }
    return true;
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!destination.trim()) {
      alert("Destination is required");
      return;
    }

    var isValid = validateForm();
    if (!isValid) return;

    const request: PlannerRequest = {
      destination,
      budget,
      currency,
      haveTimeRange,
      startDate: haveTimeRange ? startDate : undefined,
      endDate: haveTimeRange ? endDate : undefined,
      days: !haveTimeRange ? Number(days) : undefined,
      rating,
      interests: interests.map(x => x.value)
    };
    onSubmit(request);
  };

  return (
    <section className="w-full max-w-md p-6 rounded-2xl bg-gradient-to-br from-white to-slate-50 shadow-xl">
      <div className="mb-4">
        <h3 className="text-xl font-semibold">Create your trip</h3>
        <p className="text-sm text-gray-500">Tell us what kind of adventure you want</p>
      </div>

      <form onSubmit={handleSubmit}>
        <div className="flex flex-col gap-3 mb-4">
          <label className="font-semibold text-slate-700">Destination *</label>
          <input
            className="w-full p-3 rounded-lg border border-gray-300 text-base"
            placeholder="Destination"
            value={destination}
            onChange={e => setDestination(e.target.value)} />
        </div>

        <div className="grid grid-cols-2 gap-4 mb-4">
          <div className="flex flex-col gap-3">
            <label className="font-semibold text-slate-700">Budget *</label>
            <input
              className="p-3 rounded-lg border border-gray-300"
              type="number"
              placeholder="Budget"
              value={budget}
              onChange={e => setBudget(Number(e.target.value))}
            />
          </div>
        </div>

        <div className="flex flex-col gap-3 mb-4">
          <label className="font-semibold text-slate-700">Currency</label>
          <select className="p-3 rounded-lg border border-gray-300" value={currency} onChange={e => setCurrency(e.target.value)}>
            <option value="">Select currency</option>
            {currencies.map(c => (
              <option key={c.code} value={c.code}>
                {c.code} - {c.name}
              </option>
            ))}
          </select>
        </div>

        <div className="flex gap-3 mb-4">
          <button
            type="button"
            className={`flex-1 py-2 rounded-lg font-semibold ${!haveTimeRange ? 'bg-gradient-to-r from-indigo-600 to-cyan-500 text-white' : 'bg-gray-200 text-slate-700'}`}
            onClick={() => setHaveTimeRange(false)}>
            Number of days
          </button>
          <button
            type="button"
            className={`flex-1 py-2 rounded-lg font-semibold ${haveTimeRange ? 'bg-gradient-to-r from-indigo-600 to-cyan-500 text-white' : 'bg-gray-200 text-slate-700'}`}
            onClick={() => setHaveTimeRange(true)}>
            Date range
          </button>
        </div>

        {haveTimeRange ? (
          <div className="grid grid-cols-2 gap-4 mb-4">
            <div className="flex flex-col">
              <label className="font-semibold text-slate-700">Start date *</label>
              <input className="p-2 rounded-lg border border-gray-300" type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
            </div>
            <div className="flex flex-col">
              <label className="font-semibold text-slate-700">End date *</label>
              <input className="p-2 rounded-lg border border-gray-300" type="date" value={endDate} onChange={e => setEndDate(e.target.value)} />
            </div>
          </div>
        ) : (
          <div className="flex flex-col gap-3 mb-4">
            <label className="font-semibold text-slate-700">Number of days *</label>
            <input className="p-3 rounded-lg border border-gray-300" type="number" min={1} value={days} onChange={e => setDays(Number(e.target.value))} />
          </div>
        )}

        <label className="block mb-2 font-semibold text-slate-700">Interests</label>
        <Select
          isMulti
          options={INTEREST_OPTIONS}
          value={interests}
          onChange={(value) => setInterests([...value])}
          placeholder="Select your interests..."
          className="mb-4"
        />

        <div className="flex flex-col gap-3 mb-4">
          <label className="font-semibold text-slate-700">Minimum rating</label>
          <input className="p-3 rounded-lg border border-gray-300" type="number" min={0} max={5} step={0.5} value={rating} onChange={e => setRating(Number(e.target.value))} />
        </div>

        <button type="submit" className="w-full py-3 rounded-xl font-bold text-white bg-gradient-to-r from-blue-600 to-purple-600">Generate Trip</button>
      </form>
    </section>
  );
}