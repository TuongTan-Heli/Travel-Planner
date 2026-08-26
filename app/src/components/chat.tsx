import { FormEvent, useState } from 'react';
import TypeWriter from './shared/typeWriter';
import { ChatMessage } from '../models/Websocket';

interface ChatProps {
  messages: ChatMessage[];
  connected: boolean;

  onSend: (text: string) => void;
}

export default function Chat({
  messages,
  connected,
  onSend,
}: ChatProps) {
  const [inputValue, setInputValue] = useState('');

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmed = inputValue.trim();
    if (!trimmed) {
      return;
    }

    onSend(trimmed);
    setInputValue('');
  };

  return (
    <section className="flex flex-col h-full">
      <h3 className="text-lg font-semibold">Chat with AI</h3>
      <div className="bg-white rounded-lg p-4 shadow-sm">
        <div className="text-sm text-gray-500 flex items-center gap-3">
          {connected ? 'Connected' : 'Disconnected'}
        </div>
        <div className="flex flex-col gap-3  p-2 flex-1">
          {messages.map((message) => {
            if (message.type === 'incoming') {
              if (message.thinking) {
                return (
                  <div key={message.id} className="self-start border border-gray-200 rounded-lg p-4 bg-white max-h-[400px] overflow-y-auto">
                    {message.text}
                  </div>
                );
              } else {
                return (
                  <div key={message.id} className="self-start border border-gray-200 rounded-lg p-4 bg-white max-h-[400px]">
                    <TypeWriter text={message.text} speed={4} />
                  </div>
                );
              }
            }

            if (message.type === 'outgoing') {
              return (
                <div key={message.id} className="self-end border border-gray-200 rounded-lg p-4 bg-indigo-50 max-h-[400px]">
                  {message.text}
                </div>
              );
            }
            return null;
          })}
        </div>
        <form className="mt-4 flex gap-3 items-center" onSubmit={handleSubmit}>
          <input
            className="flex-1 border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-400"
            type="text"
            placeholder="Type your message..."
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
          />
          <button className="button font-semibold shadow text-sm p-3" type="submit">Send</button>
        </form>
      </div>
    </section>
  );
}
