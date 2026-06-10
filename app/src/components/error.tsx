import '../styles/error.css';

interface ErrorProps {
  message: string;
}

export default function ErrorMessage({ message }: ErrorProps) {
  return (
    <div className="error-panel">
      <strong>Unable to load data</strong>
      <p>{message}</p>
    </div>
  );
}
