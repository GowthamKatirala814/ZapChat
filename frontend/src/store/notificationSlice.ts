import { createSlice, type PayloadAction } from "@reduxjs/toolkit";
import type { Notification } from "../types/Notification";

interface NotificationState {
    items: Notification[];
    unreadCount: number;
}

const initialState: NotificationState = {
    items: [],
    unreadCount: 0
};

const notificationSlice = createSlice({
    name: "notifications",
    initialState,
    reducers: {
        setNotifications: (
            state,
            action: PayloadAction<Notification[]>
        ) => {
            state.items = action.payload;
            state.unreadCount = action.payload.filter(
                n => !n.isRead
            ).length;
        },

        addNotification: (
            state,
            action: PayloadAction<Notification>
        ) => {
            // Prepend so newest is first
            state.items.unshift(action.payload);
            if (!action.payload.isRead) {
                state.unreadCount += 1;
            }
        },

        markOneRead: (state, action: PayloadAction<string>) => {
            const n = state.items.find(n => n.id === action.payload);
            if (n && !n.isRead) {
                n.isRead = true;
                state.unreadCount = Math.max(0, state.unreadCount - 1);
            }
        },

        markAllReadLocal: state => {
            state.items.forEach(n => (n.isRead = true));
            state.unreadCount = 0;
        },

        removeNotification: (state, action: PayloadAction<string>) => {
            const removed = state.items.find(
                n => n.id === action.payload
            );
            if (removed && !removed.isRead) {
                state.unreadCount = Math.max(0, state.unreadCount - 1);
            }
            state.items = state.items.filter(
                n => n.id !== action.payload
            );
        }
    }
});

export const {
    setNotifications,
    addNotification,
    markOneRead,
    markAllReadLocal,
    removeNotification
} = notificationSlice.actions;

export default notificationSlice.reducer;
