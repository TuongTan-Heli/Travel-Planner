import Chat from './chat';
import InteractiveMap from './InteractiveMap';
import '../styles/tripPlanner.css';
import { useAppDispatch, useAppSelector } from '../store/hooks';
import TripCarousel from './tripCarousel';
import type { Itinerary, SelectedStop } from '../models/itinerary';
import StopCard from './stopCard';
import { useCallback, useEffect, useRef, useState } from 'react';
import Planner from './planner';
import { OutgoingMessage, WebSocketMessage } from '../models/websocket';
import { setPresentationData } from '../store/itinerarySlice';
import { setSystemState } from '../store/systemSlice';
import { PlannerRequest } from './planner';

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

export default function TripPlanner() {
  const presentationData = useAppSelector((state) => state.itinerary.presentationData);
  const systemState = useAppSelector(state => state.system);
  const itinerary = presentationData as Itinerary | null;
  const [selectedStop, setSelectedStop] = useState<SelectedStop | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const socketRef = useRef<WebSocket | null>(null);
  const dispatch = useAppDispatch();

  const handleChatMessage = useCallback(
    (message: WebSocketMessage) => {

      if (message.kind !== "Chat") {
        return;
      }
      console.log("CHAT MESSAGE RECEIVED:", message);

      if (message.id === "Presentation") {

        try {
          const parsed = JSON.parse(message.text) as Itinerary;

          dispatch(setPresentationData(parsed));

          return;

        } catch {
          console.error("Invalid presentation JSON");
          return;
        }
      }


      const chatMessage: ChatMessage = {
        id: message.id,
        text: message.text,
        type:
          message.chatType === "Outgoing"
            ? "outgoing"
            : "incoming",
        sender: message.sender,
        timestamp: message.timestamp,
        thinking: message.thinking
      };


      setMessages(prev => {

        const existing = prev.findIndex(
          x => x.id === chatMessage.id
        );

        if (existing !== -1) {
          const updated = [...prev];
          updated[existing] = chatMessage;
          return updated;
        }


        return [...prev, chatMessage];

      });

    },
    []
  );

  useEffect(() => {
    const protocol = window.location.protocol === 'https:' ? 'wss' : 'ws';
    const websocketUrl = `${protocol}://localhost:5223/ws/chat`;
    const socket = new WebSocket(websocketUrl);

    socket.onopen = () => {
      setConnected(true);
      setError(null);
    };

    socket.onmessage = event => {
      const message = JSON.parse(event.data) as WebSocketMessage;

      switch (message.kind) {
        case "State":
          dispatch(
            setSystemState({
              message: message.message,
              processing: message.processing
            })
          );
          break;

        case "Chat":
          handleChatMessage(message);
          break;

        case "Error":
          setError(message.message);
          break;
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
  }, [dispatch, handleChatMessage]);

  const sendPlannerRequest = (trip: PlannerRequest) => {

    const socket = socketRef.current;

    if (!socket || socket.readyState !== WebSocket.OPEN) {
      setError("Unable to send request. Socket is not connected.");
      return;
    }

    socket.send(JSON.stringify({
      id: Guid.NewGuid(),
      type: "Planner",
      data: trip
    }));

  };

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

  return (
    <section className="planner-root">

      <div className="planner-top">
        <div className="planner-panel">
          {systemState.processing && (
            <div className="ai-panel-overlay">
              <div className="ai-overlay-content">
                <div className="ai-orb">
                  <span className="spinner" />
                </div>

                <h2>Planning your trip</h2>
                <p>{systemState.message}</p>
              </div>
            </div>
          )}
          <h2>Itinerary Preview</h2>
          {itinerary ? (
            <>
              <TripCarousel data={itinerary} />

              {selectedStop && (
                <StopCard
                  onStopSelect={setSelectedStop}
                  selectedStop={selectedStop}
                  onGoBack={() => setSelectedStop(null)}
                />
              )}
            </>
          ) : (
            <p>No itinerary data yet.</p>
          )}
        </div>
        <InteractiveMap selectedStop={selectedStop} onStopSelect={setSelectedStop} />
      </div>
      <div className="planner-bottom">
        <Chat messages={messages}
          connected={connected}
          error={error}
          onSend={sendMessage} />
        <Planner onSubmit={sendPlannerRequest} />
      </div>
    </section>
  );
}
