import axios from "axios";
import type { InternalAxiosRequestConfig } from "axios";
import { store } from "../store/store";
import { logout } from "../store/authSlice";

const BASE_URL = import.meta.env.VITE_API_BASE_URL || "https://localhost:5000";

// ─── In-memory token cache ────────────────────────────────────────────────────
// The JWT lives in an HttpOnly cookie. Downstream services (non-Auth) need it
// as an Authorization: Bearer header. We fetch it once from the Auth Service's
// /api/auth/token echo endpoint and cache it in memory for 14 minutes.
// On page refresh the cache is cleared and re-fetched automatically.
let _cachedToken: string | null = null;
let _cacheExpiry = 0;
let _isRefreshing = false;
let _refreshSubscribers: Array<(token: string | null) => void> = [];

function subscribeTokenRefresh(cb: (token: string | null) => void) {
    _refreshSubscribers.push(cb);
}

function onTokenRefreshed(token: string | null) {
    _refreshSubscribers.forEach((cb) => cb(token));
    _refreshSubscribers = [];
}

export function clearTokenCache() {
    _cachedToken = null;
    _cacheExpiry = 0;
}

async function fetchAccessToken(): Promise<string | null> {
    if (_cachedToken && Date.now() < _cacheExpiry) return _cachedToken;

    try {
        // Use plain fetch with credentials to avoid Axios interceptor loops
        const res = await fetch(`${BASE_URL}/api/auth/token`, {
            credentials: "include",
        });

        if (!res.ok) {
            _cachedToken = null;
            return null;
        }

        const token = await res.text();
        _cachedToken = token;
        _cacheExpiry = Date.now() + 14 * 60 * 1000; // 14-minute cache (token lives 15 min)
        return token;
    } catch {
        _cachedToken = null;
        return null;
    }
}

async function tryRefreshToken(): Promise<boolean> {
    try {
        const res = await fetch(`${BASE_URL}/api/auth/refresh`, {
            method: "POST",
            credentials: "include",
        });
        return res.ok;
    } catch {
        return false;
    }
}

function performLogout() {
    clearTokenCache();
    store.dispatch(logout());
    window.location.href = "/login";
}

// ─── Auth Service client (withCredentials for cookie operations) ──────────────
export const api = axios.create({
    baseURL: BASE_URL,
    headers: { "Content-Type": "application/json" },
    withCredentials: true, // Needed to send/receive HttpOnly cookies
});

// ─── Downstream Service clients ───────────────────────────────────────────────
export const chatApiClient = axios.create({
    baseURL: BASE_URL,
    headers: { "Content-Type": "application/json" },
    withCredentials: true,
});

export const privateChatApiClient = axios.create({
    baseURL: BASE_URL,
    headers: { "Content-Type": "application/json" },
    withCredentials: true,
});

export const notificationApiClient = axios.create({
    baseURL: BASE_URL,
    headers: { "Content-Type": "application/json" },
    withCredentials: true,
});

export const pollApiClient = axios.create({
    baseURL: BASE_URL,
    headers: { "Content-Type": "application/json" },
    withCredentials: true,
});

export const adminApiClient = axios.create({
    baseURL: BASE_URL,
    headers: { "Content-Type": "application/json" },
    withCredentials: true,
});

// ─── Bearer Token Interceptor (downstream services) ───────────────────────────
// Attaches the JWT as Authorization: Bearer before every request.
// On 401: silently calls /refresh, then retries the original request once.
const addBearerInterceptor = (
    client: ReturnType<typeof axios.create>
) => {
    // Request: attach token
    client.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
        const token = await fetchAccessToken();
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    });

    // Response: handle 401
    client.interceptors.response.use(
        (response) => response,
        async (error) => {
            const originalConfig = error.config as InternalAxiosRequestConfig & { _bearerRetried?: boolean };

            if (error.response?.status === 401 && !originalConfig._bearerRetried) {
                originalConfig._bearerRetried = true;

                if (_isRefreshing) {
                    // Another request already triggered a refresh — wait for it
                    return new Promise((resolve, reject) => {
                        subscribeTokenRefresh((newToken) => {
                            if (newToken) {
                                originalConfig.headers.Authorization = `Bearer ${newToken}`;
                                resolve(client(originalConfig));
                            } else {
                                reject(error);
                            }
                        });
                    });
                }

                _isRefreshing = true;

                const refreshed = await tryRefreshToken();
                _isRefreshing = false;

                if (refreshed) {
                    clearTokenCache();
                    const newToken = await fetchAccessToken();
                    onTokenRefreshed(newToken);

                    if (newToken) {
                        originalConfig.headers.Authorization = `Bearer ${newToken}`;
                        return client(originalConfig);
                    }
                }

                // Refresh failed — log the user out
                onTokenRefreshed(null);
                performLogout();
            }

            return Promise.reject(error);
        }
    );
};

addBearerInterceptor(chatApiClient);
addBearerInterceptor(privateChatApiClient);
addBearerInterceptor(notificationApiClient);
addBearerInterceptor(pollApiClient);
addBearerInterceptor(adminApiClient);

// ─── Auth client response interceptor ────────────────────────────────────────
// Handles 401 from the Auth Service itself (e.g. /api/auth/me with expired cookie).
// Does NOT intercept /refresh or /token endpoints to avoid loops.
api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const url: string = error.config?.url ?? "";
        const isAuthSpecial =
            url.includes("/refresh") ||
            url.includes("/token") ||
            url.includes("/logout") ||
            url.includes("/login") ||
            url.includes("/register");

        if (error.response?.status === 401 && !isAuthSpecial) {
            const refreshed = await tryRefreshToken();
            if (refreshed) {
                clearTokenCache();
                // Retry original request — cookie is now refreshed
                return api(error.config);
            }
            performLogout();
        }

        return Promise.reject(error);
    }
);

// ─── Network Retry Strategy ───────────────────────────────────────────────────
// Retries transient failures (network errors, 503 Service Unavailable) up to
// 2 times with exponential backoff. Does NOT retry 4xx client errors.
const RETRY_DELAY_MS = 400;
const MAX_RETRIES = 2;

function isRetryableError(error: unknown): boolean {
    if (!axios.isAxiosError(error)) return false;
    if (!error.response) return true; // Network error (no response at all)
    const status = error.response.status;
    return status === 503 || status === 502 || status === 504;
}

function addRetryInterceptor(client: ReturnType<typeof axios.create>) {
    client.interceptors.response.use(undefined, async (error) => {
        const config = error.config as InternalAxiosRequestConfig & { _retryCount?: number };
        if (!config) return Promise.reject(error);

        config._retryCount = config._retryCount ?? 0;

        if (isRetryableError(error) && config._retryCount < MAX_RETRIES) {
            config._retryCount++;
            const delay = RETRY_DELAY_MS * Math.pow(2, config._retryCount - 1);
            await new Promise((resolve) => setTimeout(resolve, delay));
            return client(config);
        }

        return Promise.reject(error);
    });
}

addRetryInterceptor(api);
addRetryInterceptor(chatApiClient);
addRetryInterceptor(privateChatApiClient);
addRetryInterceptor(notificationApiClient);
addRetryInterceptor(pollApiClient);
addRetryInterceptor(adminApiClient);