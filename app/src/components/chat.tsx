import { FormEvent, useState } from 'react';
import '../styles/chat.css';
import TypeWriter from './TypeWriter';
import { ChatMessage } from '../models/websocket';

interface ChatProps {
  messages: ChatMessage[];
  connected: boolean;
  error: string | null;

  onSend: (text: string) => void;
}

export default function Chat({
  messages,
  connected,
  error,
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
    <section className="chat-panel">
      <h3>Chat</h3>
      <div className="chat-box">
        <div className="chat-status">
          {connected ? 'Connected' : 'Disconnected'}
          {error && <span className="chat-error">{error}</span>}
        </div>
        <div className="chat-messages">
          {messages.map((message) => {
            if (message.type === 'incoming') {
              if (message.thinking) {
                return (
                  <div key={message.id} className="chat-message incoming">
                    {message.text}
                  </div>
                );
              }
              else {

                return (
                  <div key={message.id} className="chat-message incoming">
                    <TypeWriter
                      text={message.text}
                      speed={4} />
                  </div>);
              }
            }

            if (message.type === 'outgoing') {
              return (
                <div key={message.id} className="chat-message outgoing">
                  {message.text}
                </div>

              );
            }
          })}
        </div>
        <form className="chat-input" onSubmit={handleSubmit}>
          <input
            type="text"
            placeholder="Type your message..."
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
          />
          <button type="submit">Send</button>
        </form>
      </div>
    </section>
  );
}
