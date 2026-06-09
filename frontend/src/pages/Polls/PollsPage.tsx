import { useEffect, useRef, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { BarChart3, Plus, X } from "lucide-react";
import { useNavigate } from "react-router-dom";
import type { RootState, AppDispatch } from "../../store/store";
import {
    setPolls,
    addPoll,
    updatePoll,
    setUserVote,
    setUserReaction
} from "../../store/pollSlice";
import { getAllPolls, createPoll, voteOnPoll, reactToPoll } from "../../api/pollApi";
import { getPollConnection } from "../../hubs/pollHub";
import type { Poll } from "../../types/Poll";
import PollCard from "../../components/PollCard";

export default function PollsPage() {
    const dispatch = useDispatch<AppDispatch>();
    const navigate = useNavigate();
    const { polls, loading } = useSelector(
        (s: RootState) => s.polls
    );

    const [showCreate, setShowCreate] = useState(false);
    const [question, setQuestion] = useState("");
    const [options, setOptions] = useState(["", ""]);
    const [creating, setCreating] = useState(false);
    const [createError, setCreateError] = useState("");
    const userId = localStorage.getItem("userId") ?? "";
    const connRef = useRef(getPollConnection());

    // Load polls and start SignalR
    useEffect(() => {
        getAllPolls(userId).then(data => dispatch(setPolls(data)));

        const conn = connRef.current;

        conn.off("PollCreated");
        conn.off("PollUpdated");

        conn.on("PollCreated", (poll: Poll) => {
            dispatch(addPoll(poll));
        });

        conn.on("PollUpdated", (poll: Poll) => {
            dispatch(updatePoll(poll));
        });

        if (conn.state === "Disconnected") {
            conn.start().catch(console.error);
        }

        return () => {
            conn.off("PollCreated");
            conn.off("PollUpdated");
        };
    }, [dispatch]);

    const handleAddOption = () => {
        if (options.length >= 6) return;
        setOptions([...options, ""]);
    };

    const handleRemoveOption = (idx: number) => {
        if (options.length <= 2) return;
        setOptions(options.filter((_, i) => i !== idx));
    };

    const handleOptionChange = (idx: number, val: string) => {
        const next = [...options];
        next[idx] = val;
        setOptions(next);
    };

    const handleCreate = async () => {
        const filtered = options.filter(o => o.trim() !== "");
        if (!question.trim()) {
            setCreateError("Enter a question.");
            return;
        }
        if (filtered.length < 2) {
            setCreateError("Need at least 2 options.");
            return;
        }
        setCreating(true);
        setCreateError("");
        try {
            await createPoll(question.trim(), filtered, userId);
            setQuestion("");
            setOptions(["", ""]);
            setShowCreate(false);
        } catch {
            setCreateError("Failed to create poll.");
        } finally {
            setCreating(false);
        }
    };

    const handleVote = async (pollId: string, optionId: string | null) => {
        await voteOnPoll(pollId, userId, optionId);
        dispatch(setUserVote({ pollId, optionId }));
    };

    const handleReact = async (pollId: string, isUpvote: boolean | null) => {
        await reactToPoll(pollId, userId, isUpvote);
        dispatch(setUserReaction({ pollId, isUpvote }));
    };

    return (
        <div className="
            min-h-screen bg-slate-950 text-white
            flex flex-col">

            {/* Header */}
            <div className="
                border-b border-slate-800 px-6 py-4
                flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <button
                        onClick={() => navigate("/dashboard")}
                        className="text-slate-400 hover:text-white text-sm">
                        ← Dashboard
                    </button>
                    <div className="flex items-center gap-2">
                        <BarChart3
                            size={20}
                            className="text-blue-400"
                        />
                        <h1 className="text-xl font-bold">Polls</h1>
                    </div>
                </div>
                <button
                    onClick={() => setShowCreate(true)}
                    className="
                        flex items-center gap-2 text-sm
                        bg-blue-600 hover:bg-blue-700
                        px-4 py-2 rounded-lg
                        transition-colors">
                    <Plus size={16} />
                    New Poll
                </button>
            </div>

            {/* Create Poll Modal */}
            {showCreate && (
                <div className="
                    fixed inset-0 bg-black/60
                    flex items-center justify-center z-50">
                    <div className="
                        bg-slate-900 border border-slate-700
                        rounded-2xl p-6 w-full max-w-md
                        space-y-4">
                        <div className="flex items-center justify-between">
                            <h2 className="text-lg font-semibold">
                                Create Poll
                            </h2>
                            <button
                                onClick={() => setShowCreate(false)}
                                className="
                                    text-slate-400 hover:text-white">
                                <X size={20} />
                            </button>
                        </div>

                        <input
                            value={question}
                            onChange={e => setQuestion(e.target.value)}
                            placeholder="Ask a question..."
                            className="
                                w-full bg-slate-800 rounded-lg
                                px-4 py-3 text-sm outline-none
                                border border-slate-700
                                focus:border-blue-500
                                transition-colors"
                        />

                        <div className="space-y-2">
                            <p className="text-xs text-slate-400 uppercase">
                                Options
                            </p>
                            {options.map((opt, idx) => (
                                <div
                                    key={idx}
                                    className="flex items-center gap-2">
                                    <input
                                        value={opt}
                                        onChange={e =>
                                            handleOptionChange(
                                                idx,
                                                e.target.value
                                            )
                                        }
                                        placeholder={`Option ${idx + 1}`}
                                        className="
                                            flex-1 bg-slate-800
                                            rounded-lg px-4 py-2.5
                                            text-sm outline-none
                                            border border-slate-700
                                            focus:border-blue-500
                                            transition-colors"
                                    />
                                    {options.length > 2 && (
                                        <button
                                            onClick={() =>
                                                handleRemoveOption(idx)
                                            }
                                            className="
                                                text-slate-500
                                                hover:text-red-400">
                                            <X size={16} />
                                        </button>
                                    )}
                                </div>
                            ))}
                            {options.length < 6 && (
                                <button
                                    onClick={handleAddOption}
                                    className="
                                        text-xs text-blue-400
                                        hover:text-blue-300
                                        flex items-center gap-1">
                                    <Plus size={12} />
                                    Add option
                                </button>
                            )}
                        </div>

                        {createError && (
                            <p className="text-xs text-red-400">
                                {createError}
                            </p>
                        )}

                        <div className="flex gap-3 pt-2">
                            <button
                                onClick={() => setShowCreate(false)}
                                className="
                                    flex-1 py-2.5 rounded-lg
                                    border border-slate-700
                                    text-sm text-slate-400
                                    hover:bg-slate-800
                                    transition-colors">
                                Cancel
                            </button>
                            <button
                                onClick={handleCreate}
                                disabled={creating}
                                className="
                                    flex-1 py-2.5 rounded-lg
                                    bg-blue-600 hover:bg-blue-700
                                    text-sm font-medium
                                    transition-colors
                                    disabled:opacity-50">
                                {creating ? "Creating..." : "Create"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Poll list */}
            <div className="
                flex-1 max-w-2xl w-full mx-auto
                px-4 py-6 space-y-4">
                {loading ? (
                    <div className="
                        text-center text-slate-500 py-20">
                        Loading polls...
                    </div>
                ) : polls.length === 0 ? (
                    <div className="
                        text-center text-slate-500
                        py-20 space-y-3">
                        <BarChart3
                            size={48}
                            className="mx-auto opacity-30"
                        />
                        <p className="text-lg">No polls yet</p>
                        <p className="text-sm">
                            Create the first poll!
                        </p>
                    </div>
                ) : (
                    polls.map(poll => (
                        <PollCard
                            key={poll.id}
                            poll={poll}
                            onVote={handleVote}
                            onReact={handleReact}
                        />
                    ))
                )}
            </div>
        </div>
    );
}
