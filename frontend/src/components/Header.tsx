import {
    Bell,
    Search,
    LogOut
}
    from "lucide-react";

import {
    getAnonymousName,
    logout
}
    from "../utils/auth";

interface Props {
    roomName: string;
}

export default function Header({
    roomName
}: Props) {

    return (
        <div
            className="
            h-16
            border-b
            border-slate-800
            px-6
            flex
            items-center
            justify-between
            bg-slate-950"
        >

            <div>

                <h2
                    className="
                    text-lg
                    font-semibold"
                >
                    {roomName}
                </h2>

                <p
                    className="
                    text-xs
                    text-slate-500"
                >
                    Room Discussion
                </p>

            </div>

            <div
                className="
                flex
                items-center
                gap-5"
            >

                <button
                    className="
                    text-slate-400
                    hover:text-white"
                >
                    <Search size={20} />
                </button>

                <button
                    className="
                    text-slate-400
                    hover:text-white"
                >
                    <Bell size={20} />
                </button>

                <button
                    onClick={logout}
                    className="
                    text-slate-400
                    hover:text-red-500"
                >
                    <LogOut size={20} />
                </button>

                <div
                    className="
                    w-10
                    h-10
                    rounded-full
                    bg-blue-600
                    flex
                    items-center
                    justify-center
                    font-semibold"
                >
                    {
                        getAnonymousName()
                            .charAt(0)
                            .toUpperCase()
                    }
                </div>

            </div>

        </div>
    );
}