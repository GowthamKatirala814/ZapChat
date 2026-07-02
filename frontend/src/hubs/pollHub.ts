import * as signalR from "@microsoft/signalr";

const BASE_URL = import.meta.env.VITE_API_BASE_URL || "https://localhost:5000";

let pollConnection: signalR.HubConnection | null = null;

export function getPollConnection(): signalR.HubConnection {
    if (
        pollConnection &&
        pollConnection.state !==
        signalR.HubConnectionState.Disconnected
    ) {
        return pollConnection;
    }

    pollConnection =
        new signalR.HubConnectionBuilder()
            .withUrl(
                `${BASE_URL}/pollHub`,
                {
                    // Reads the HttpOnly access_token cookie via Auth Service echo endpoint
                    accessTokenFactory: async () => {
                        try {
                            const res = await fetch(`${BASE_URL}/api/auth/token`, {
                                credentials: "include",
                            });
                            if (res.ok) return await res.text();
                        } catch {
                            // Ignore
                        }
                        return "";
                    }
                }
            )
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Warning)
            .build();

    return pollConnection;
}
