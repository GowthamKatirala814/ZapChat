import * as signalR from "@microsoft/signalr";

export const connection =
    new signalR.HubConnectionBuilder()
        .withUrl(
            "http://localhost:5139/chatHub",
            {
                accessTokenFactory: () =>
                    localStorage.getItem("token") ?? ""
            }
        )
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();