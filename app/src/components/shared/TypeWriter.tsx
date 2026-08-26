import { useEffect, useState } from "react";

interface TypeWriterProps {
  text: string;
  speed?: number;
  loop?: boolean;
  cursor?: boolean;
  hideCursorOnComplete?: boolean;
  className?: string;
}

export default function TypeWriter({
  text,
  speed = 50,
  loop = false,
  cursor = true,
  hideCursorOnComplete = true,
  className = "",
}: TypeWriterProps) {
  const [displayedText, setDisplayedText] = useState("");
  const [isComplete, setIsComplete] = useState(false);

  useEffect(() => {
    let timeout: NodeJS.Timeout;
    let index = 0;

    setDisplayedText("");
    setIsComplete(false);

    const type = () => {
      if (index <= text.length) {
        setDisplayedText(text.slice(0, index));
        index++;

        timeout = setTimeout(type, speed);
      } else {
        setIsComplete(true);

        if (loop) {
          timeout = setTimeout(() => {
            index = 0;
            setDisplayedText("");
            setIsComplete(false);
            type();
          }, 1000);
        }
      }
    };

    type();

    return () => clearTimeout(timeout);
  }, [text, speed, loop]);

  const showCursor =
    cursor &&
    (!hideCursorOnComplete || !isComplete || loop);

  return (
    <span className={className}>
      {displayedText}
      {showCursor && <BlinkingCursor />}
    </span>
  );
}

function BlinkingCursor() {
  const [visible, setVisible] = useState(true);

  useEffect(() => {
    const interval = setInterval(() => {
      setVisible(v => !v);
    }, 500);

    return () => clearInterval(interval);
  }, []);

  return <span>{visible ? "|" : " "}</span>;
}