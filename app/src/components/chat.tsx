import { FormEvent, useEffect, useRef, useState } from 'react';
import '../styles/chat.css';
import TypeWriter from './TypeWriter';
import { useAppDispatch } from '../store/hooks';
import { setPresentationData } from '../store/itinerarySlice';
import { Itinerary } from '../models/itinerary';

// Simple UUID generator
const Guid = {
  NewGuid: () =>
    'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    })
};

interface ChatMessage {
  id: string;
  text: string;
  type: 'incoming' | 'outgoing';
  sender?: string;
  timestamp?: string;
  thinking?: boolean;
}

interface OutgoingMessage {
  id: string;
  text: string;
}

export default function Chat() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputValue, setInputValue] = useState('');
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const socketRef = useRef<WebSocket | null>(null);
  const dispatch = useAppDispatch();

  useEffect(() => {
    const protocol = window.location.protocol === 'https:' ? 'wss' : 'ws';
    const websocketUrl = `${protocol}://localhost:5223/ws/chat`;
    const socket = new WebSocket(websocketUrl);

    socket.onopen = () => {
      setConnected(true);
      setError(null);
    };

    socket.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data) as ChatMessage;
        if (message.id === "error") {
          setError(message.text);
          return;
        }

        if (message.id === "Presentation") {
          let parsed: Itinerary;
          try {
            parsed = JSON.parse(message.text) as Itinerary;
          } catch {
            console.error("Invalid presentation JSON");
            return;
          }
          dispatch(setPresentationData(parsed));
          return;
        }

        setMessages((prev) => {
          const index = prev.findIndex((m) => m.id === message.id);
          if (index !== -1) {
            const next = [...prev];
            next[index] = message;
            return next;
          }
          return [...prev, message];
        });
      } catch (error) {
        console.error('Failed to parse message:', error);
      }
    };

    socket.onclose = () => {
      setConnected(false);
    };

    socket.onerror = () => {
      setError('WebSocket connection error.');
    };

    socketRef.current = socket;

    return () => {
      socket.close();
    };
  }, []);

  const sendMessage = (text: string) => {
    const socket = socketRef.current;
    if (!socket || socket.readyState !== WebSocket.OPEN) {
      setError('Unable to send message. Socket is not connected.');
      return;
    }

    const messageId = Guid.NewGuid();
    const localMessage: ChatMessage = {
      id: messageId,
      text: text,
      type: 'outgoing',
      sender: 'You',
      timestamp: new Date().toISOString()
    };
    setMessages((prev) => [...prev, localMessage]);

    const outgoing: OutgoingMessage = { id: messageId, text };
    socket.send(JSON.stringify(outgoing));
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmed = inputValue.trim();
    if (!trimmed) {
      return;
    }

    sendMessage(trimmed);
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
