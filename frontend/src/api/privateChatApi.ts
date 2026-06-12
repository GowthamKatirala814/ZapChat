import { privateChatApiClient }
    from "./client";

export interface Conversation {
    id: string;
    user1Id: string;
    user2Id: string;
    otherUserId: string;
    lastMessage: {
        id: string;
        content: string;
        sentAt: string;
        senderId: string;
        senderName: string;
        isRead: boolean;
    } | null;
    unreadCount: number;
    lastActivity: string | null;
}

export const createConversation =
    async (
        user1Id: string,
        user2Id: string
    ) => {

        const response =
            await privateChatApiClient.post(
                `/api/privatechat/conversation?user1Id=${user1Id}&user2Id=${user2Id}`
            );

        return response.data;
    };

export const getConversation =
    async (
        conversationId: string
    ) => {

        const response =
            await privateChatApiClient.get(
                `/api/privatechat/conversation/${conversationId}`
            );

        return response.data;
    };

export const getConversations =
    async (userId: string): Promise<Conversation[]> => {
        const response =
            await privateChatApiClient.get(
                `/api/privatechat/conversations?userId=${userId}`
            );
        return response.data;
    };

export const markAsRead =
    async (messageId: string): Promise<void> => {
        await privateChatApiClient.put(
            `/api/privatechat/read/${messageId}`
        );
    };

export const deletePrivateMessage =
    async (messageId: string): Promise<void> => {
        await privateChatApiClient.delete(
            `/api/privatemessages/${messageId}`
        );
    };