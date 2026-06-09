import { notificationApiClient } from "./client";
import type { Notification } from "../types/Notification";

export const getNotifications = async (
    userId: string
): Promise<Notification[]> => {
    const response = await notificationApiClient.get(
        `/api/notification/${userId}`
    );
    return response.data;
};

export const markAsRead = async (id: string): Promise<void> => {
    await notificationApiClient.put(
        `/api/notification/read/${id}`
    );
};

export const markAllAsRead = async (userId: string): Promise<void> => {
    await notificationApiClient.put(
        `/api/notification/read-all/${userId}`
    );
};

export const deleteNotification = async (id: string): Promise<void> => {
    await notificationApiClient.delete(
        `/api/notification/${id}`
    );
};
