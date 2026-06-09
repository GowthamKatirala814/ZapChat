import * as signalR from "@microsoft/signalr";

let notificationConnection: signalR.HubConnection | null = null;

export function getNotificationConnection(): signalR.HubConnection {
    if (
        notificationConnection &&
        notificationConnection.state !==
        signalR.HubConnectionState.Disconnected
    ) {
        return notificationConnection;
    }

    notificationConnection =
        new signalR.HubConnectionBuilder()
            .withUrl(
                "http://localhost:5262/notificationHub",
                {
                    accessTokenFactory: () =>
                        localStorage.getItem("token") ?? ""
                }
            )
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Warning)
            .build();

    return notificationConnection;
}
