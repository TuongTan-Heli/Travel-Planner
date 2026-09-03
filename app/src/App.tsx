import Footer from './components/shared/Footer';
import Hero from './components/shared/Hero';
import TripPlanner from './components/TripPlanner';

function App() {
  return (
    <div className="app-shell">
      <header className="app-header hero-deck">
        <Hero />
      </header>

      <main className="app-main">
        <TripPlanner />
      </main>

      <Footer />
    </div>
  );
}

export default App;
