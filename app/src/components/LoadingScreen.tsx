interface LoadingProps {
  message?: string;
}

export default function LoadingScreen({ message = 'Loading...' }: LoadingProps) {
  return (
    <div className="inline-flex items-center gap-3 bg-slate-50 rounded-lg p-3">
      <span className="w-3 h-3 rounded-full bg-blue-600 animate-pulse" />
      <span className="text-sm text-slate-700">{message}</span>
    </div>
  );
}
