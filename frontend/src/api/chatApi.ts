import { chatApiClient } from "./client";
import type { Message } from "../types/Message";

export const getRoomMessages = async (
    roomName: string
): Promise<Message[]> => {
    const response = await chatApiClient.get(
        `/api/chat/messages?roomName=${encodeURIComponent(roomName)}`
    );
    return response.data;
};
