import { useState } from "react";
import { BarChart3, ThumbsUp, ThumbsDown } from "lucide-react";
import type { Poll } from "../types/Poll";

interface Props {
    poll: Poll;
    onVote: (pollId: string, optionId: string | null) => Promise<void>;
    onReact: (pollId: string, isUpvote: boolean | null) => Promise<void>;
}

export default function PollCard({
    poll,
    onVote,
    onReact
}: Props) {
    const [voting, setVoting] = useState(false);
    const [reacting, setReacting] = useState(false);
    const [error, setError] = useState("");

    const totalVotes = poll.options.reduce(
        (sum, o) => sum + o.voteCount,
        0
    );

    const handleVote = async (optionId: string) => {
        if (voting) return;
        setVoting(true);
        setError("");
        try {
            // If they click the same option they already voted for, remove the vote
            const newOptionId = poll.userVoteOptionId === optionId ? null : optionId;
            await onVote(poll.id, newOptionId);
        } catch {
            setError("Failed to cast vote. Try again.");
        } finally {
            setVoting(false);
        }
    };

    const handleReact = async (isUpvote: boolean) => {
        if (reacting) return;
        setReacting(true);
        setError("");
        try {
            // If they click the same reaction, remove it
            const newReaction = poll.userReaction === isUpvote ? null : isUpvote;
            await onReact(poll.id, newReaction);
        } catch {
            setError("Failed to record reaction.");
        } finally {
            setReacting(false);
        }
    };

    return (
        <div className="
            bg-slate-900 border border-slate-700
            rounded-xl p-5 space-y-4">

            {/* Question */}
            <div className="flex items-start gap-3">
                <BarChart3
                    size={18}
                    className="text-blue-400 mt-0.5 shrink-0"
                />
                <div>
                    <p className="font-semibold text-white">
                        {poll.question}
                    </p>
                    <p className="text-xs text-slate-500 mt-0.5">
                        {totalVotes} vote{totalVotes !== 1 ? "s" : ""} ·{" "}
                        {new Date(poll.createdAt).toLocaleDateString()}
                    </p>
                </div>
            </div>

            {/* Options */}
            <div className="space-y-2">
                {poll.options.map(option => {
                    const pct =
                        totalVotes > 0
                            ? Math.round(
                                  (option.voteCount / totalVotes) * 100
                              )
                            : 0;
                    
                    const isSelected = poll.userVoteOptionId === option.id;

                    return (
                        <button
                            key={option.id}
                            onClick={() => handleVote(option.id)}
                            disabled={voting}
                            className={`
                                relative w-full text-left
                                rounded-lg overflow-hidden
                                border transition-colors
                                ${isSelected
                                    ? "border-blue-500 ring-1 ring-blue-500 cursor-pointer"
                                    : "border-slate-700 hover:border-slate-500 cursor-pointer"
                                }
                            `}
                        >
                            {/* Progress bar background */}
                            <div
                                className="
                                    absolute inset-0
                                    bg-blue-600/20
                                    transition-all duration-500"
                                style={{ width: `${pct}%` }}
                            />

                            {/* Label row */}
                            <div className="
                                relative flex items-center
                                justify-between
                                px-4 py-2.5 text-sm">
                                <span className={isSelected ? "text-blue-300 font-medium" : "text-white"}>
                                    {option.optionText}
                                </span>
                                <span className="
                                    text-slate-300 font-mono text-xs ml-4">
                                    {pct}% ({option.voteCount})
                                </span>
                            </div>
                        </button>
                    );
                })}
            </div>

            {/* Reactions & Footer */}
            <div className="flex items-center justify-between pt-2 border-t border-slate-800">
                <div className="flex items-center gap-4">
                    <button
                        onClick={() => handleReact(true)}
                        disabled={reacting}
                        className={`flex items-center gap-1.5 text-xs transition-colors ${
                            poll.userReaction === true
                                ? "text-green-400"
                                : "text-slate-400 hover:text-green-400"
                        }`}
                    >
                        <ThumbsUp size={14} className={poll.userReaction === true ? "fill-current" : ""} />
                        <span>{poll.upvotes}</span>
                    </button>
                    <button
                        onClick={() => handleReact(false)}
                        disabled={reacting}
                        className={`flex items-center gap-1.5 text-xs transition-colors ${
                            poll.userReaction === false
                                ? "text-red-400"
                                : "text-slate-400 hover:text-red-400"
                        }`}
                    >
                        <ThumbsDown size={14} className={poll.userReaction === false ? "fill-current" : ""} />
                        <span>{poll.downvotes}</span>
                    </button>
                </div>
                {poll.userVoteOptionId && (
                    <p className="text-xs text-blue-400">
                        ✓ Vote recorded
                    </p>
                )}
            </div>

            {error && (
                <p className="text-xs text-red-400">{error}</p>
            )}
        </div>
    );
}
