import { useEffect, useState } from "react";
import '../styles/planner.css';
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
    <section className="planner-form">
      <div className="planner-header">
        <h3>Create your trip</h3>
        <p> Tell us what kind of adventure you want </p>
      </div>

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>
            Destination *
          </label>
          <input
            placeholder="Destination"
            value={destination}
            onChange={e => setDestination(e.target.value)} />
        </div>
        <div className="form-row">
          <div className="form-group">
            <label>
              Budget *
            </label>
            <input
              type="number"
              placeholder="Budget"
              value={budget}
              onChange={e => setBudget(Number(e.target.value))}
            />
          </div>
        </div>
        <div className="form-group">

          <label>
            Currency
          </label>
          <select
            value={currency}
            onChange={e => setCurrency(e.target.value)}>
            <option value="">Select currency</option>
            {currencies.map(c => (
              <option key={c.code} value={c.code}>
                {c.code} - {c.name}
              </option>
            ))
            }
          </select>
        </div>
        <div className="duration-toggle">
          <button
            type="button"
            className={
              !haveTimeRange
                ? "active" : ""
            }
            onClick={() =>
              setHaveTimeRange(false)
            }
          >
            Number of days
          </button>
          <button
            type="button"
            className={
              haveTimeRange
                ? "active" : ""
            }
            onClick={() =>
              setHaveTimeRange(true)
            }
          >
            Date range
          </button>
        </div>


        {
          haveTimeRange ? (
            <div className="form-row">
              <div className="form-group"></div>
              <label>
                Start date *
              </label>
              <input
                type="date"
                value={startDate}
                onChange={e => setStartDate(e.target.value)} />

              <label>
                End date *
              </label>
              <input
                type="date"
                value={endDate}
                onChange={e => setEndDate(e.target.value)} />
            </div>
          ) : (
            <div className="form-group">
              <label>
                Number of days *
              </label>
              <input
                type="number"
                min={1}
                value={days}
                onChange={e => setDays(Number(e.target.value))}
              />
            </div>
          )
        }

        <label>Interests</label>
        <Select
          isMulti
          options={INTEREST_OPTIONS}
          value={interests}
          onChange={(value) => setInterests([...value])}
          placeholder="Select your interests..."
          className="interest-select" />


        <div className="form-group">
          <label>Minimum rating</label>
          <input
            type="number"
            min={0}
            max={5}
            step={0.5}
            value={rating}
            onChange={e => setRating(Number(e.target.value))}
          />
        </div>

        <button type="submit" className="generate-button">
          Generate Trip
        </button>
      </form>
    </section>
  );
}