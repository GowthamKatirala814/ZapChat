import type {
    PrivateMessage
}
    from "../types/PrivateMessage";

interface Props {
    message: PrivateMessage;
}

export default function PrivateMessageBubble({
    message
}: Props) {

    return (
        <div
            className="
            bg-slate-900
            p-3
            rounded-xl
            mb-3"
        >

            <div>
                {message.content}
            </div>

        </div>
    );
}