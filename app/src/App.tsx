import Footer from './components/footer';
import TripPlanner from './components/tripPlanner';

function App() {
  return (
    <div className="app-shell">
      <header className="app-header hero-deck">
        <div className="hero-content">
          <h1>Travel Planner</h1>
          <p>Small SPA dashboard with backend-powered weather and itinerary features.</p>
        </div>
      </header>

      <main className="app-main">
        <TripPlanner />
      </main>

      <Footer />
    </div>
  );
}

export default App;
