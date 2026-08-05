interface ErrorProps {
  message: string;
}

export default function ErrorMessage({ message }: ErrorProps) {
  return (
    <div className="p-4 rounded-lg border border-rose-200 bg-rose-50 text-rose-700">
      <strong className="block">Unable to load data</strong>
      <p className="mt-1 text-sm">{message}</p>
    </div>
  );
}
