import { chatApiClient } from "./client";
import type { Message } from "../types/Message";

export interface Room {
    id: string;
    name: string;
    roomType: string;
    createdAt: string;
}

export const getRooms = async (): Promise<Room[]> => {
    const response = await chatApiClient.get("/api/admin/rooms");
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
