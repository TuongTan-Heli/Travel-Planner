import Chat from './chat';
import InteractiveMap from './interactiveMap';
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
  const [activeBottomPanel, setActiveBottomPanel] = useState<'chat' | 'planner'>('chat');
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
    <section className="grid gap-6 w-full">
      <div className="grid gap-6 grid-cols-1 md:grid-cols-2 items-stretch">
        <div className="relative">
          {systemState.processing && connected && (
            <div className="fixed inset-0 z-10 flex items-center justify-center bg-gradient-to-br from-slate-800/60 to-indigo-700/60 backdrop-blur-sm">
              <div className="flex flex-col items-center gap-4 text-white text-center p-6">
                <div className="w-20 h-20 rounded-full flex items-center justify-center bg-gradient-to-r from-sky-400 to-violet-500 shadow-lg animate-pulse">
                  <span className="w-8 h-8 border-4 border-white border-t-transparent rounded-full animate-spin" />
                </div>
                <h2 className="text-2xl font-extrabold bg-clip-text text-transparent bg-gradient-to-r from-sky-300 via-indigo-300 to-purple-300">Planning your trip</h2>
                <p className="text-sm text-white/80">{systemState.message}</p>
              </div>
            </div>
          )}

          <div className="p-4 bg-white rounded-xl p-6 shadow-md h-full">
            <h2 className="text-lg font-semibold border-blue-500">Itinerary Preview</h2>
            {itinerary ? (
              <>
                <div className={selectedStop ? "hidden" : "block"}>
                  //prevent use !selectedStop && to avoid unnecessary re-rendering 
                  <TripCarousel data={itinerary} />
                </div>

                {selectedStop && (
                  <StopCard
                    onStopSelect={setSelectedStop}
                    selectedStop={selectedStop}
                    onGoBack={() => setSelectedStop(null)}
                  />
                )}
              </>
            ) : (
              <p className="text-sm text-slate-500">No itinerary data yet.</p>
            )}
          </div>
        </div>

        <div className="bg-white rounded-xl p-6 shadow-md h-[420px] md:h-full">
          <InteractiveMap selectedStop={selectedStop} onStopSelect={setSelectedStop} />
        </div>
      </div>

      <div className="grid gap-6">
        <div className="flex gap-2 mb-4">
          <button
            type="button"
            className={`button p-4 ${activeBottomPanel === 'chat' ? "active" : ""}`}
            onClick={() => setActiveBottomPanel('chat')}
          >
            Chat
          </button>
          <button
            type="button"
            className={`button p-4 ${activeBottomPanel === 'planner' ? "active" : ""}`}
            onClick={() => setActiveBottomPanel('planner')}
          >
            Planner
          </button>
        </div>

        <div className="flex flex-col">
          {activeBottomPanel === 'chat' ? (
            <div className="bg-white rounded-xl p-4 shadow-sm h-[420px] overflow-y-auto">
              <Chat messages={messages} connected={connected} error={error} onSend={sendMessage} />
            </div>
          ) : (
            <div className="bg-white rounded-xl p-4 shadow-sm">
              <Planner onSubmit={sendPlannerRequest} />
            </div>
          )}
        </div>
      </div>
    </section>
  );
}
