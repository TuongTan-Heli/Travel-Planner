import '../styles/footer.css';

export default function Footer() {
  return (
    <footer className="footer">
      <p>Travel Planner @ {new Date().getFullYear()}</p>
    </footer>
  );
}
