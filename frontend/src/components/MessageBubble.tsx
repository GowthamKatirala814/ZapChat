import type { Message }
    from "../types/Message";

interface Props {
    message: Message;
}

export default function MessageBubble({
    message
}: Props) {

    const formattedTime =
        message.sentAt
            ? new Date(
                message.sentAt
            ).toLocaleTimeString(
                [],
                {
                    hour: "2-digit",
                    minute: "2-digit"
                }
            )
            : "";

    return (
        <div
            className="
            mb-4
            flex
            gap-3"
        >

            <div
                className="
                w-10
                h-10
                rounded-full
                bg-blue-600
                flex
                items-center
                justify-center
                font-semibold
                shrink-0"
            >
                {
                    message.anonymousName
                        ?.charAt(0)
                        .toUpperCase()
                }
            </div>

            <div
                className="
                flex-1"
            >

                <div
                    className="
                    flex
                    items-center
                    gap-3"
                >

                    <span
                        className="
                        text-blue-400
                        font-semibold"
                    >
                        {message.anonymousName}
                    </span>

                    <span
                        className="
                        text-xs
                        text-slate-500"
                    >
                        {formattedTime}
                    </span>

                </div>

                <div
                    className="
                    mt-1
                    bg-slate-900
                    p-3
                    rounded-xl
                    break-words"
                >
                    {message.message}
                </div>

            </div>

        </div>
    );
}
