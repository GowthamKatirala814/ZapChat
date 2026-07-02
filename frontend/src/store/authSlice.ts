import { createSlice, type PayloadAction } from "@reduxjs/toolkit";

// Token is NO LONGER stored in Redux or localStorage.
// It lives exclusively in an HttpOnly cookie set by the Auth Service.
// Only non-sensitive profile data is kept in Redux (and localStorage for page-refresh persistence).

export interface AuthPayload {
    userId?: string;
    anonymousName?: string;
    email?: string;
    role?: "user" | "admin";
}

interface AuthState {
    userId: string | null;
    anonymousName: string | null;
    email: string | null;
    role: "user" | "admin" | null;
    isAuthenticated: boolean;
}

const initialState: AuthState = {
    // Rehydrate from localStorage on page refresh.
    // If userId is present, the user was previously logged in.
    // The first authenticated API call will verify the cookie is still valid.
    // If the cookie has expired, the 401 → refresh → logout flow handles it.
    userId: localStorage.getItem("userId"),
    anonymousName: localStorage.getItem("anonymousName"),
    email: localStorage.getItem("email"),
    role: (localStorage.getItem("role") as "user" | "admin" | null),
    isAuthenticated: !!localStorage.getItem("userId"),
};

const authSlice = createSlice({
    name: "auth",
    initialState,
    reducers: {
        loginSuccess: (state, action: PayloadAction<AuthPayload>) => {
            const { userId, anonymousName, email, role } = action.payload;
            state.userId = userId ?? null;
            state.anonymousName = anonymousName ?? null;
            state.email = email ?? null;
            state.role = role ?? "user";
            state.isAuthenticated = true;

            // Persist non-sensitive profile data only (no token)
            if (userId) localStorage.setItem("userId", userId);
            if (anonymousName) localStorage.setItem("anonymousName", anonymousName);
            if (email) localStorage.setItem("email", email);
            localStorage.setItem("role", role ?? "user");
        },

        logout: (state) => {
            state.userId = null;
            state.anonymousName = null;
            state.email = null;
            state.role = null;
            state.isAuthenticated = false;

            // Clear profile data from localStorage (no token to remove)
            localStorage.removeItem("userId");
            localStorage.removeItem("anonymousName");
            localStorage.removeItem("email");
            localStorage.removeItem("role");
        },
    },
});

export const { loginSuccess, logout } = authSlice.actions;
export default authSlice.reducer;