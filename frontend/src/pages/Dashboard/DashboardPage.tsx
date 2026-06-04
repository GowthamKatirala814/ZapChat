import { useState } from "react";

import Sidebar from "../../components/Sidebar";
import Header from "../../components/Header";
import OnlineUsers from "../../components/OnlineUsers";
import ChatWindow from "../../components/ChatWindow";

export default function DashboardPage() {

    const [selectedRoom, setSelectedRoom] =
        useState("General Chat");

    return (
        <div className="h-screen bg-slate-950 text-white flex">

            <div className="w-72 border-r border-slate-800">
                <Sidebar
                    selectedRoom={selectedRoom}
                    setSelectedRoom={setSelectedRoom}
                />
            </div>

            <div className="flex-1 flex flex-col">
                <Header roomName={selectedRoom} />

                <div className="flex-1">
                    <ChatWindow roomName={selectedRoom} />
                </div>
            </div>

            <div className="w-72 border-l border-slate-800">
                <OnlineUsers />
            </div>

        </div>
    );
}