import { chatApiClient } from "./client";
import type { Message } from "../types/Message";

export interface Room {
    id: string;
    name: string;
    roomType: string;
    createdAt: string;
    lastMessageAt?: string;
    lastMessagePreview?: string;
    unreadCount?: number;
}

export const getRooms = async (userId?: string): Promise<Room[]> => {
    const url = userId
        ? `/api/chat/rooms?userId=${encodeURIComponent(userId)}`
        : "/api/chat/rooms";
    const response = await chatApiClient.get(url);
    return response.data;
};

export const getRoomMessages = async (
    roomName: string
): Promise<Message[]> => {
    const response = await chatApiClient.get(
        `/api/chat/messages?roomName=${encodeURIComponent(roomName)}`
    );
    return response.data;
};

export const deleteMessage = async (messageId: string): Promise<void> => {
    await chatApiClient.delete(`/api/messages/${messageId}`);
};

export const markRoomAsRead = async (roomName: string, userId: string): Promise<void> => {
    await chatApiClient.put(`/api/chat/room/${encodeURIComponent(roomName)}/read?userId=${userId}`);
};

export const getMessageSeenBy = async (messageId: string): Promise<any[]> => {
    const response = await chatApiClient.get(`/api/chat/messages/${encodeURIComponent(messageId)}/seen-by`);
    return response.data;
};
