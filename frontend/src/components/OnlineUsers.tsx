import {
    useEffect,
    useState
} from "react";

import { useNavigate }
    from "react-router-dom";

import { getUsers }
    from "../api/authApi";

import type { User }
    from "../types/User";

export default function OnlineUsers() {

    const [users,
        setUsers] =
        useState<User[]>([]);

    const navigate =
        useNavigate();

    const currentUserId =
        localStorage.getItem(
            "userId"
        );

    useEffect(() => {

        const loadUsers =
            async () => {

                const result =
                    await getUsers();

                setUsers(result);
            };

        loadUsers();

    }, []);

    return (

        <div
            className="
            h-full
            bg-slate-900
            p-5"
        >

            <h2
                className="
                font-semibold
                mb-5"
            >
                Users
            </h2>

            <div
                className="
                space-y-3"
            >

                {
                    users
                        .filter(
                            x =>
                                x.id !==
                                currentUserId
                        )
                        .map(
                            user => (

                                <div
                                    key={user.id}
                                    onClick={() =>
                                        navigate(
                                            `/dm/${user.id}`
                                        )
                                    }
                                    className="
                                    flex
                                    items-center
                                    gap-2
                                    cursor-pointer
                                    p-2
                                    rounded-lg
                                    hover:bg-slate-800"
                                >

                                    <span>
                                        🟢
                                    </span>

                                    <div>

                                        <div>
                                            {
                                                user.fullName
                                            }
                                        </div>

                                        <div
                                            className="
                                            text-xs
                                            text-slate-400"
                                        >
                                            {
                                                user.email
                                            }
                                        </div>

                                    </div>

                                </div>
                            )
                        )
                }

            </div>

        </div>
    );
}