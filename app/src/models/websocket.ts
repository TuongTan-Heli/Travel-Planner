export type WebSocketMessage =
    | ChatWebSocketMessage
    | SystemStateWebSocketMessage
    | ErrorWebSocketMessage;


export interface BaseWebSocketMessage {
    kind: "Chat" | "State" | "Error";
}

export enum ChatType {
    Incoming = 0,
    Outgoing = 1
}


export interface ChatWebSocketMessage extends BaseWebSocketMessage {
    kind: "Chat";
    id: string;
    text: string;
    chatType: "Incoming" | "Outgoing";
    sender?: string;
    timestamp?: string;
    thinking?: boolean;
}


export interface SystemStateWebSocketMessage extends BaseWebSocketMessage {
    kind: "State";
    message: string;
    processing: boolean;
}


export interface ErrorWebSocketMessage extends BaseWebSocketMessage {
    kind: "Error";
    message: string;
}

export interface OutgoingMessage {
  id: string;
  text: string;
}

export interface ChatMessage {
    id: string;
    text: string;
    type: "incoming" | "outgoing";
    sender?: string;
    timestamp?: string;
    thinking?: boolean;
}