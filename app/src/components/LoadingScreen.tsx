import '../styles/LoadingScreen.css';

interface LoadingProps {
  message?: string;
}

export default function LoadingScreen({ message = 'Loading...' }: LoadingProps) {
  return (
    <div className="loading-screen">
      <span className="loading-dot" />
      <span>{message}</span>
    </div>
  );
}
