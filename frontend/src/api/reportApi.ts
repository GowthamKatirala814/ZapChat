import { adminApiClient } from "./client";

export type MessageType = 0 | 1; // 0 = Room, 1 = Private

export interface SubmitReportPayload {
    messageId: string;
    messageType: MessageType;
    reportedByUserId: string;
    reason: string;
}

export async function submitReport(payload: SubmitReportPayload): Promise<void> {
    await adminApiClient.post("/api/reports", payload);
}
