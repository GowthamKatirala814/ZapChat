import * as signalR from "@microsoft/signalr";

export const connection =
    new signalR.HubConnectionBuilder()
        .withUrl(
            "https://localhost:7195/chatHub",
            {
                accessTokenFactory: () =>
                    localStorage.getItem("token") ?? ""
            }
        )
        .withAutomaticReconnect()
        .build();