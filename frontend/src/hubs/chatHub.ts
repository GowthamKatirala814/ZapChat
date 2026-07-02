import * as signalR from "@microsoft/signalr";

const BASE_URL = import.meta.env.VITE_API_BASE_URL || "https://localhost:5000";

export const connection =
    new signalR.HubConnectionBuilder()
        .withUrl(
            `${BASE_URL}/chatHub`,
            {
                // accessTokenFactory is called by SignalR before connecting and on reconnect.
                // Since the JWT now lives in an HttpOnly cookie (inaccessible to JS), we call
                // the Auth Service's /api/auth/token echo endpoint which reads the cookie
                // server-side and returns the raw JWT string.
                accessTokenFactory: async () => {
                    try {
                        const res = await fetch(`${BASE_URL}/api/auth/token`, {
                            credentials: "include",
                        });
                        if (res.ok) return await res.text();
                    } catch {
                        // Ignore — connection will fail with 401 if no valid token
                    }
                    return "";
                }
            }
        )
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();