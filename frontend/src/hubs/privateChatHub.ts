import * as signalR
    from "@microsoft/signalr";

export const privateChatConnection =
    new signalR.HubConnectionBuilder()
        .withUrl(
            "https://localhost:7279/privateChatHub",
            {
                accessTokenFactory: () =>
                    localStorage.getItem(
                        "token"
                    ) ?? ""
            }
        )
        .withAutomaticReconnect()
        .configureLogging(
            signalR.LogLevel.Information
        )
        .build();