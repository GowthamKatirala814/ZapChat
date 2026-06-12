export interface MessageReaction {
    anonymousName: string;
    reaction: string;
}

export interface Message {
    id?: string;
    anonymousName: string;
    message: string;
    sentAt: string;
    userId?: string;
    parentMessageId?: string;
    reactions?: MessageReaction[];
    // File attachment fields
    attachmentUrl?: string;
    fileName?: string;
    // User self-deletion (separate from admin moderation IsRemoved)
    isDeleted?: boolean;
    deletedAt?: string;
}