import { configureStore } from "@reduxjs/toolkit";
import authReducer from "./authSlice";
import notificationReducer from "./notificationSlice";
import pollReducer from "./pollSlice";

export const store = configureStore({
    reducer: {
        auth: authReducer,
        notifications: notificationReducer,
        polls: pollReducer
    }
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;