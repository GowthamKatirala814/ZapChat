import * as signalR from "@microsoft/signalr";
import fetch from "node-fetch";

async function run() {
    console.log("Registering test user 1...");
    const req1 = await fetch("http://localhost:5111/api/auth/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            fullName: "Test User 1",
            email: "test1" + Date.now() + "@test.com",
            password: "Password123!",
            department: "IT",
            branch: "NY"
        })
    });
    const authData1 = await req1.json();
    const token1 = authData1.token;
    const userId1 = authData1.userId;
    console.log("User 1:", userId1);

    console.log("Registering test user 2...");
    const req2 = await fetch("http://localhost:5111/api/auth/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            fullName: "Test User 2",
            email: "test2" + Date.now() + "@test.com",
            password: "Password123!",
            department: "IT",
            branch: "NY"
        })
    });
    const authData2 = await req2.json();
    const userId2 = authData2.userId;
    console.log("User 2:", userId2);

    console.log("Creating conversation...");
    const convReq = await fetch(`http://localhost:5172/api/PrivateChat/conversation?user1Id=${userId1}&user2Id=${userId2}`, {
        method: "POST",
        headers: { "Authorization": "Bearer " + token1 }
    });
    const conv = await convReq.json();
    console.log("Conversation ID:", conv.id);

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5172/privateChatHub", {
            accessTokenFactory: () => token1
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

    await connection.start();
    console.log("SignalR connected!");

    console.log("Sending private message...");
    try {
        await connection.invoke("SendPrivateMessage", conv.id, userId2, "hello", null);
        console.log("Message sent successfully!");
    } catch (err) {
        console.error("Invoke failed:", err);
    }

    await connection.stop();
}

run().catch(console.error);
