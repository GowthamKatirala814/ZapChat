import { clsx } from "clsx";
import { Check, Lock, ThumbsDown, ThumbsUp, Trash2 } from "lucide-react";
import { useAuth } from "../../app/providers";
import { Avatar, Badge, Button, Card } from "../../components/ui";
import { formatCount, formatRelative } from "../../lib/format";
import type { Poll } from "../../types/api";

/**
 * A poll.
 *
 * Every number here is the server's: `voteCount` and `percentage` come from the poll
 * document, and `myVoteOptionId` is what makes the selected option persist across
 * reloads. Nothing is tallied in the browser.
 */
export function PollCard({
  poll,
  onVote,
  onReact,
  onClose,
  onRemove,
  isBusy,
}: {
  poll: Poll;
  onVote: (optionId: string | null) => void;
  onReact: (isUpvote: boolean | null) => void;
  onClose: () => void;
  onRemove: () => void;
  isBusy: boolean;
}) {
  const { isAdmin } = useAuth();

  const closed = poll.status !== "Open";
  const voted = Boolean(poll.myVoteOptionId);

  return (
    <Card className="flex flex-col gap-4">
      <div className="flex items-start gap-3">
        <Avatar name={poll.creatorName} size={34} />

        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="text-[13px] font-medium text-body">{poll.creatorName}</span>
            {poll.isMine && (
              <span className="text-[10.5px] font-medium text-accent-text bg-accent-soft px-1.5 rounded-[--radius-sm]">
                You
              </span>
            )}
            <span className="text-[11.5px] text-faint">{formatRelative(poll.createdAt)}</span>
            {closed && (
              <Badge tone="neutral">
                <Lock size={10} />
                Closed
              </Badge>
            )}
          </div>

          <h3 className="font-display text-[16px] font-semibold text-body mt-1.5 leading-snug">
            {poll.question}
          </h3>
        </div>

        {/* Closing is creator-or-admin; removal is admin-only. Both are enforced by
            the server regardless of what is rendered here. */}
        <div className="flex items-center gap-1 shrink-0">
          {!closed && (poll.isMine || isAdmin) && (
            <Button
              size="sm"
              variant="ghost"
              disabled={isBusy}
              onClick={onClose}
              title="Close this poll to further voting"
            >
              Close
            </Button>
          )}
          {isAdmin && (
            <Button
              size="icon"
              variant="ghost"
              disabled={isBusy}
              onClick={onRemove}
              aria-label="Remove poll"
              title="Remove poll"
            >
              <Trash2 size={15} />
            </Button>
          )}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        {poll.options.map((option) => {
          const chosen = poll.myVoteOptionId === option.id;
          // Results are revealed after voting or closing, so early votes cannot be
          // anchored by the running tally.
          const showResults = voted || closed;

          return (
            <button
              key={option.id}
              type="button"
              disabled={closed || isBusy}
              onClick={() => onVote(chosen ? null : option.id)}
              aria-pressed={chosen}
              className={clsx(
                "relative w-full text-left px-3 py-2.5 rounded-[--radius-DEFAULT] border overflow-hidden",
                "transition-colors disabled:cursor-default",
                chosen
                  ? "border-accent bg-accent-soft"
                  : "border-line hover:border-line-strong bg-surface",
                closed && !chosen && "opacity-80",
              )}
              title={
                closed
                  ? "This poll is closed"
                  : chosen
                    ? "Click to withdraw your vote"
                    : "Click to vote for this option"
              }
            >
              {showResults && (
                <span
                  aria-hidden
                  className="absolute inset-y-0 left-0 transition-[width] duration-500"
                  style={{
                    width: `${option.percentage}%`,
                    background: chosen
                      ? "color-mix(in srgb, var(--zc-accent) 22%, transparent)"
                      : "var(--zc-surface-2)",
                  }}
                />
              )}

              <span className="relative flex items-center gap-2">
                <span
                  className={clsx(
                    "w-4 h-4 rounded-[--radius-full] border-2 shrink-0 flex items-center justify-center",
                    chosen ? "border-accent bg-accent" : "border-line-strong",
                  )}
                  aria-hidden
                >
                  {chosen && <Check size={10} className="text-accent-contrast" strokeWidth={3.5} />}
                </span>

                <span className="flex-1 min-w-0 text-[13.5px] text-body truncate">
                  {option.text}
                </span>

                {showResults && (
                  <span className="text-[12.5px] font-medium text-muted shrink-0 zc-tabular">
                    {option.percentage}%
                    <span className="text-faint font-normal ml-1.5">
                      ({formatCount(option.voteCount)})
                    </span>
                  </span>
                )}
              </span>
            </button>
          );
        })}
      </div>

      <div className="flex items-center justify-between gap-3 pt-1">
        <span className="text-[12px] text-faint zc-tabular">
          {formatCount(poll.totalVotes)} {poll.totalVotes === 1 ? "vote" : "votes"}
          {!voted && !closed && " · your vote is not shown to anyone"}
        </span>

        <div className="flex items-center gap-1">
          <ReactionButton
            active={poll.myReaction === true}
            count={poll.upvotes}
            disabled={isBusy}
            onClick={() => onReact(poll.myReaction === true ? null : true)}
            label="Agree"
          >
            <ThumbsUp size={14} />
          </ReactionButton>

          <ReactionButton
            active={poll.myReaction === false}
            count={poll.downvotes}
            disabled={isBusy}
            onClick={() => onReact(poll.myReaction === false ? null : false)}
            label="Disagree"
          >
            <ThumbsDown size={14} />
          </ReactionButton>
        </div>
      </div>
    </Card>
  );
}

function ReactionButton({
  active,
  count,
  disabled,
  onClick,
  label,
  children,
}: {
  active: boolean;
  count: number;
  disabled: boolean;
  onClick: () => void;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-pressed={active}
      aria-label={`${label} (${count})`}
      title={active ? `Remove your ${label.toLowerCase()}` : label}
      className={clsx(
        "inline-flex items-center gap-1.5 h-7 px-2.5 rounded-[--radius-full] border",
        "text-[12.5px] transition-colors disabled:opacity-60",
        active
          ? "bg-accent-soft border-accent/40 text-accent-text font-medium"
          : "bg-surface border-line text-muted hover:border-line-strong",
      )}
    >
      {children}
      <span className="zc-tabular">{count}</span>
    </button>
  );
}
