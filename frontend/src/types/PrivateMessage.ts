export interface PrivateMessageReaction {
    senderName: string;
    reaction: string;
}

export interface PrivateMessage {
    id?: string;
    conversationId: string;
    senderId: string;
    senderName: string;
    content: string;
    sentAt: string;
    isRead: boolean;
    parentMessageId?: string;
    reactions?: PrivateMessageReaction[];
    attachmentUrl?: string;
    fileName?: string;
}