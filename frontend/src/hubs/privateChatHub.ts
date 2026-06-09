import * as signalR from "@microsoft/signalr";

let privateChatConnection: signalR.HubConnection | null = null;

export function getPrivateChatConnection(): signalR.HubConnection {
    if (
        privateChatConnection &&
        privateChatConnection.state !==
        signalR.HubConnectionState.Disconnected
    ) {
        return privateChatConnection;
    }

    privateChatConnection =
        new signalR.HubConnectionBuilder()
            .withUrl(
                "http://localhost:5172/privateChatHub",
                {
                    accessTokenFactory: () =>
                        localStorage.getItem("token") ?? ""
                }
            )
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Warning)
            .build();

    return privateChatConnection;
}

export { privateChatConnection };