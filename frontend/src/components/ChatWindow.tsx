import {
    useEffect,
    useState
} from "react";

import { connection }
    from "../hubs/chatHub";

import MessageBubble
    from "./MessageBubble";

import type { Message }
    from "../types/Message";

import {
    getAnonymousName
}
    from "../utils/auth";
import {
    useRef
}
    from "react";

interface Props {
    roomName: string;
}

export default function ChatWindow({
    roomName
}: Props) {

    const [messages, setMessages] =
        useState<Message[]>([]);

    const [message, setMessage] =
        useState("");

    const [currentRoom,
        setCurrentRoom] =
        useState("");

    const [typingUser,
        setTypingUser] =
        useState("");
    const bottomRef =
        useRef<HTMLDivElement>(
            null
        );

    useEffect(() => {

        const startConnection =
            async () => {

                try {

                    if (
                        connection.state ===
                        "Disconnected"
                    ) {

                        await connection.start();

                        connection.on(
                            "ReceiveMessage",
                            data => {

                                setMessages(
                                    prev => [
                                        ...prev,
                                        data
                                    ]
                                );
                            });

                        connection.on(
                            "UserTyping",
                            (
                                anonymousName
                            ) => {

                                setTypingUser(
                                    anonymousName
                                );
                            });

                        connection.on(
                            "UserStoppedTyping",
                            () => {

                                setTypingUser("");
                            });
                    }

                    await connection.invoke(
                        "JoinRoom",
                        roomName
                    );

                    setCurrentRoom(
                        roomName
                    );
                }
                catch (error) {

                    console.error(
                        error
                    );
                }
            };

        startConnection();

    }, []);

    useEffect(() => {

        const switchRoom =
            async () => {

                try {

                    if (
                        connection.state !==
                        "Connected"
                    )
                        return;

                    if (
                        currentRoom &&
                        currentRoom !== roomName
                    ) {

                        await connection.invoke(
                            "LeaveRoom",
                            currentRoom
                        );
                    }

                    await connection.invoke(
                        "JoinRoom",
                        roomName
                    );

                    setCurrentRoom(
                        roomName
                    );

                    setMessages([]);
                }
                catch (error) {

                    console.error(
                        error
                    );
                }
            };

        switchRoom();

    }, [roomName]);
    useEffect(() => {

        bottomRef.current
            ?.scrollIntoView({
                behavior: "smooth"
            });

    }, [messages]);

    const handleTyping =
        async (
            value: string
        ) => {

            setMessage(value);

            try {

                if (
                    value.trim()
                ) {

                    await connection.invoke(
                        "Typing",
                        roomName,
                        getAnonymousName()
                    );
                }
                else {

                    await connection.invoke(
                        "StopTyping",
                        roomName,
                        getAnonymousName()
                    );
                }
            }
            catch { }
        };

    const sendMessage =
        async () => {

            if (
                !message.trim()
            )
                return;

            try {

                await connection.invoke(
                    "SendMessage",
                    roomName,
                    getAnonymousName(),
                    message
                );

                await connection.invoke(
                    "StopTyping",
                    roomName,
                    getAnonymousName()
                );

                setMessage("");
            }
            catch (error) {

                console.error(
                    error
                );
            }
        };

    return (
        <div
            className="
            h-full
            flex
            flex-col"
        >

            <div
                className="
                flex-1
                p-6
                overflow-y-auto"
            >

                {
                    typingUser &&
                    typingUser !==
                    getAnonymousName() && (

                        <div
                            className="
                            text-sm
                            text-slate-400
                            mb-3"
                        >
                            {typingUser}
                            {" "}
                            is typing...
                        </div>
                    )
                }

                {
                    messages.length === 0 && (

                        <div
                            className="
                            bg-slate-900
                            p-4
                            rounded-xl"
                        >
                            Welcome to {roomName}
                        </div>
                    )
                }

                {
                    messages.map(
                        (m, i) => (
                            <MessageBubble
                                key={i}
                                message={m}
                            />
                        )
                    )
                }
                <div ref={bottomRef} />

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
                                handleTyping(
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
                        placeholder={`Message ${roomName}`}
                        className="
                        flex-1
                        bg-slate-900
                        p-3
                        rounded-lg
                        outline-none"
                    />

                    <button
                        onClick={
                            sendMessage
                        }
                        className="
                        px-5
                        rounded-lg
                        bg-blue-600
                        hover:bg-blue-700"
                    >
                        Send
                    </button>

                </div>

            </div>

        </div>
    );
}