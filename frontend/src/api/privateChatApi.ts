import { api }
    from "./client";

export const createConversation =
    async (
        user1Id: string,
        user2Id: string
    ) => {

        const response =
            await api.post(
                `/api/privatechat/conversation?user1Id=${user1Id}&user2Id=${user2Id}`
            );

        return response.data;
    };

export const getConversation =
    async (
        conversationId: string
    ) => {

        const response =
            await api.get(
                `/api/privatechat/conversation/${conversationId}`
            );

        return response.data;
    };