import * as signalR from "@microsoft/signalr";

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
                "http://localhost:5292/pollHub",
                {
                    accessTokenFactory: () =>
                        localStorage.getItem("token") ?? ""
                }
            )
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Warning)
            .build();

    return pollConnection;
}
