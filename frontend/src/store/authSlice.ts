import { createSlice, type PayloadAction } from "@reduxjs/toolkit";

export interface AuthPayload {
    token: string;
    userId?: string;
    anonymousName?: string;
    email?: string;
    role?: "user" | "admin";
}

interface AuthState {
    token: string | null;
    userId: string | null;
    anonymousName: string | null;
    email: string | null;
    role: "user" | "admin" | null;
    isAuthenticated: boolean;
}

const initialState: AuthState = {
    token: localStorage.getItem("token"),
    userId: localStorage.getItem("userId"),
    anonymousName: localStorage.getItem("anonymousName"),
    email: localStorage.getItem("email"),
    role: (localStorage.getItem("role") as "user" | "admin" | null),
    isAuthenticated: !!localStorage.getItem("token"),
};

const authSlice = createSlice({
    name: "auth",
    initialState,
    reducers: {
        loginSuccess: (state, action: PayloadAction<AuthPayload>) => {
            const { token, userId, anonymousName, email, role } = action.payload;
            state.token = token;
            state.userId = userId ?? null;
            state.anonymousName = anonymousName ?? null;
            state.email = email ?? null;
            state.role = role ?? "user";
            state.isAuthenticated = true;

            localStorage.setItem("token", token);
            if (userId) localStorage.setItem("userId", userId);
            if (anonymousName) localStorage.setItem("anonymousName", anonymousName);
            if (email) localStorage.setItem("email", email);
            localStorage.setItem("role", role ?? "user");
        },

        logout: (state) => {
            state.token = null;
            state.userId = null;
            state.anonymousName = null;
            state.email = null;
            state.role = null;
            state.isAuthenticated = false;

            localStorage.removeItem("token");
            localStorage.removeItem("userId");
            localStorage.removeItem("anonymousName");
            localStorage.removeItem("email");
            localStorage.removeItem("role");
        },
    },
});

export const { loginSuccess, logout } = authSlice.actions;
export default authSlice.reducer;