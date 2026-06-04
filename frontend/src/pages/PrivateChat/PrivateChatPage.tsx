import {
    useEffect,
    useState
} from "react";

import {
    useParams
} from "react-router-dom";

import {
    getUserById
} from "../../api/authApi";

import {
    createConversation,
    getConversation
} from "../../api/privateChatApi";

import {
    privateChatConnection
} from "../../hubs/privateChatHub";

import type { User }
    from "../../types/User";

export default function PrivateChatPage() {

    const { userId } =
        useParams();

    const [receiver,
        setReceiver] =
        useState<User | null>(
            null
        );

    const [conversationId,
        setConversationId] =
        useState("");

    const [messages,
        setMessages] =
        useState<any[]>([]);

    const [message,
        setMessage] =
        useState("");

    useEffect(() => {

        const loadUser =
            async () => {

                if (!userId)
                    return;

                const result =
                    await getUserById(
                        userId
                    );

                setReceiver(
                    result
                );
            };

        loadUser();

    }, [userId]);

    useEffect(() => {

        const createDm =
            async () => {

                if (!userId)
                    return;

                const currentUserId =
                    localStorage.getItem(
                        "userId"
                    );

                if (!currentUserId)
                    return;

                const conversation =
                    await createConversation(
                        currentUserId,
                        userId
                    );

                setConversationId(
                    conversation.id
                );

                const history =
                    await getConversation(
                        conversation.id
                    );

                setMessages(
                    history
                );
            };

        createDm();

    }, [userId]);

    useEffect(() => {

        const startConnection =
            async () => {

                try {

                    console.log(
                        "Initial State:",
                        privateChatConnection.state
                    );

                    if (
                        privateChatConnection.state ===
                        "Disconnected"
                    ) {

                        console.log(
                            "Before Start:",
                            privateChatConnection.state
                        );

                        await privateChatConnection.start();

                        console.log(
                            "After Start:",
                            privateChatConnection.state
                        );
                    }

                    privateChatConnection.on(
                        "ReceivePrivateMessage",
                        (
                            senderName,
                            message,
                            sentAt
                        ) => {

                            setMessages(
                                prev => [
                                    ...prev,
                                    {
                                        content: message,
                                        sentAt
                                    }
                                ]
                            );
                        });
                }
                catch (error) {

                    console.error(
                        "SignalR Error:",
                        error
                    );
                }
            };

        startConnection();

    }, []);

    const sendMessage =
        async () => {

            if (
                !message.trim()
            )
                return;

            try {

                console.log(
                    "Connection State:",
                    privateChatConnection.state
                );

                if (
                    privateChatConnection.state !==
                    "Connected"
                ) {

                    await privateChatConnection.start();
                }

                const senderId =
                    localStorage.getItem(
                        "userId"
                    );

                const senderName =
                    localStorage.getItem(
                        "anonymousName"
                    );

                await privateChatConnection.invoke(
                    "SendPrivateMessage",
                    conversationId,
                    senderId,
                    userId,
                    senderName,
                    message
                );
                console.log("conversationId:", conversationId);
                console.log("senderId:", senderId);
                console.log("receiverId:", userId);
                console.log("senderName:", senderName);
                console.log("message:", message);

                setMessages(
                    prev => [
                        ...prev,
                        {
                            content: message,
                            sentAt: new Date()
                        }
                    ]
                );

                setMessage("");
            }
            catch (
            error
            ) {

                console.error(
                    error
                );
            }
        };

    return (

        <div
            className="
            h-screen
            bg-slate-950
            text-white
            flex
            flex-col"
        >

            <div
                className="
                p-5
                border-b
                border-slate-800"
            >

                <div
                    className="
                    text-lg
                    font-semibold"
                >
                    {receiver?.fullName}
                </div>

                <div
                    className="
                    text-sm
                    text-slate-400"
                >
                    {receiver?.email}
                </div>

            </div>

            <div
                className="
                flex-1
                p-5
                overflow-y-auto"
            >

                {
                    messages.map(
                        (
                            m,
                            index
                        ) => (

                            <div
                                key={index}
                                className="
                                bg-slate-900
                                p-3
                                rounded-xl
                                mb-3"
                            >

                                <div>
                                    {m.content}
                                </div>

                                <div
                                    className="
                                    text-xs
                                    text-slate-500
                                    mt-1"
                                >
                                    {
                                        new Date(
                                            m.sentAt
                                        ).toLocaleTimeString()
                                    }
                                </div>

                            </div>
                        )
                    )
                }

            </div>

            <div
                className="
                border-t
                border-slate-800
                p-4"
            >

                <div
                    className="
                    flex
                    gap-3"
                >

                    <input
                        value={message}
                        onChange={
                            e =>
                                setMessage(
                                    e.target.value
                                )
                        }
                        onKeyDown={
                            e => {

                                if (
                                    e.key ===
                                    "Enter"
                                ) {

                                    sendMessage();
                                }
                            }
                        }
                        placeholder="Type a message..."
                        className="
                        flex-1
                        bg-slate-900
                        p-3
                        rounded-lg"
                    />

                    <button
                        onClick={sendMessage}
                        className="
    bg-blue-600
    px-5
    rounded-lg
    cursor-pointer
    hover:bg-blue-700"
                    >
                        Send
                    </button>

                </div>

            </div>

        </div>
    );
}