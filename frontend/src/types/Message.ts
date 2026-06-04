export interface Message {
    id?: string;
    anonymousName: string;
    message: string;
    sentAt: string;
    userEmail?: string;
    userId?: string;
    parentMessageId?: string;
}