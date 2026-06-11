import { FormEvent, useEffect, useRef, useState } from 'react';
import '../styles/chat.css';

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

    // Add immediate local echo so user sees their message instantly
    const messageId = Guid.NewGuid();
    const localMessage: ChatMessage = {
      id: messageId,
      text: text,
      type: 'outgoing',
      sender: 'You',
      timestamp: new Date().toISOString()
    };
    setMessages((prev) => [...prev, localMessage]);

    // Send to server with the same ID so the server echo updates the same message
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
              return (
                <div key={message.id} className="chat-message incoming">
                  {message.text}
                </div>
              );
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
