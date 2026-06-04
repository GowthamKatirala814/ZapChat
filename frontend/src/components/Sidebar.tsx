import type { Dispatch, SetStateAction } from "react";
import {
    Bell,
    Users,
    BarChart3
} from "lucide-react";

interface Props {
    selectedRoom: string;
    setSelectedRoom:
    Dispatch<SetStateAction<string>>;
}

export default function Sidebar({
    selectedRoom,
    setSelectedRoom
}: Props) {

    const rooms = [
        "General Chat",
        "HR Issues",
        "Hyderabad",
        "Bangalore"
    ];

    return (
        <div className="h-full bg-slate-900 p-5">

            <h1 className="text-3xl font-bold mb-8">
                ZapPulse
            </h1>

            <div className="text-xs uppercase text-slate-400 mb-4">
                Rooms
            </div>

            <div className="space-y-2">

                {
                    rooms.map(room => (
                        <div
                            key={room}
                            onClick={() =>
                                setSelectedRoom(room)
                            }
                            className={`
                                p-3
                                rounded-lg
                                cursor-pointer
                                ${selectedRoom === room
                                    ? "bg-slate-800"
                                    : "hover:bg-slate-800"
                                }
                            `}
                        >
                            # {room}
                        </div>
                    ))
                }

            </div>

            <div className="mt-10 text-xs uppercase text-slate-400 mb-4">
                Features
            </div>

            <div className="space-y-2">

                <div className="flex gap-3 items-center p-3 hover:bg-slate-800 rounded-lg">
                    <Bell size={18} />
                    Notifications
                </div>

                <div className="mt-10 text-xs uppercase text-slate-400 mb-4">
                    Direct Messages
                </div>

                <div className="space-y-2">

                    <div className="p-3 rounded-lg hover:bg-slate-800 cursor-pointer">
                        ShadowTiger123
                    </div>

                    <div className="p-3 rounded-lg hover:bg-slate-800 cursor-pointer">
                        SilentFox456
                    </div>

                    <div className="p-3 rounded-lg hover:bg-slate-800 cursor-pointer">
                        DarkWolf789
                    </div>

                </div>

                <div className="flex gap-3 items-center p-3 hover:bg-slate-800 rounded-lg">
                    <BarChart3 size={18} />
                    Polls
                </div>

            </div>

        </div>
    );
}