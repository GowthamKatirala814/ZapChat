import { Plus, Vote } from "lucide-react";
import { useMemo, useState } from "react";
import toast from "react-hot-toast";
import {
  ConnectionBanner, EmptyState, ErrorState, Skeleton,
} from "../../components/feedback";
import { Page, PageBody, PageHeader } from "../../components/layout/ListDetail";
import { Button } from "../../components/ui";
import { errorMessage } from "../../services/api";
import { CreatePollDialog } from "./CreatePollDialog";
import { PollCard } from "./PollCard";
import { usePollMutations, usePolls, usePollsRealtime } from "./usePolls";

/**
 * Polls.
 *
 * Polls are platform-wide rather than per-room, which is why this is a page of its own
 * rather than a panel inside a channel — the hub broadcasts poll events to every
 * connected client, not to a room group.
 */
export function PollsPage() {
  const polls = usePolls();
  const connection = usePollsRealtime();
  const { create, vote, react, close, remove } = usePollMutations();

  const [showCreate, setShowCreate] = useState(false);
  const [filter, setFilter] = useState<"open" | "closed">("open");

  const { open, closed } = useMemo(() => {
    const all = polls.data ?? [];
    return {
      open: all.filter((poll) => poll.status === "Open"),
      closed: all.filter((poll) => poll.status !== "Open"),
    };
  }, [polls.data]);

  const visible = filter === "open" ? open : closed;
  const busy = vote.isPending || react.isPending || close.isPending || remove.isPending;

  return (
    <Page>
      <PageHeader
        title="Polls"
        description="Ask the company a question. Votes are anonymous."
        action={
          <Button size="sm" icon={<Plus size={15} />} onClick={() => setShowCreate(true)}>
            New poll
          </Button>
        }
      />

      {connection !== "connected" && connection !== "idle" && (
        <ConnectionBanner
          state={
            connection === "reconnecting"
              ? "reconnecting"
              : connection === "connecting"
                ? "connecting"
                : "offline"
          }
        />
      )}

      <PageBody width="narrow">
        <div className="flex items-center gap-1 mb-5 p-1 bg-surface-2 rounded-[--radius-DEFAULT] self-start w-fit">
          <FilterTab active={filter === "open"} onClick={() => setFilter("open")}>
            Open ({open.length})
          </FilterTab>
          <FilterTab active={filter === "closed"} onClick={() => setFilter("closed")}>
            Closed ({closed.length})
          </FilterTab>
        </div>

        {polls.isLoading ? (
          <div className="flex flex-col gap-4">
            <Skeleton className="h-52 rounded-[--radius-lg]" count={3} />
          </div>
        ) : polls.error ? (
          <ErrorState error={polls.error} onRetry={() => void polls.refetch()} />
        ) : visible.length === 0 ? (
          <EmptyState
            icon={<Vote size={20} />}
            title={filter === "open" ? "No open polls" : "No closed polls"}
            description={
              filter === "open"
                ? "Create one to ask the company something. Nobody can see who voted for what."
                : "Polls appear here once they are closed to voting."
            }
            action={
              filter === "open" && (
                <Button size="sm" variant="secondary" onClick={() => setShowCreate(true)}>
                  Create a poll
                </Button>
              )
            }
          />
        ) : (
          <div className="flex flex-col gap-4">
            {visible.map((poll) => (
              <PollCard
                key={poll.id}
                poll={poll}
                isBusy={busy}
                onVote={(optionId) =>
                  vote.mutate(
                    { pollId: poll.id, optionId },
                    { onError: (error) => toast.error(errorMessage(error, "Your vote was not saved.")) },
                  )
                }
                onReact={(isUpvote) =>
                  react.mutate(
                    { pollId: poll.id, isUpvote },
                    { onError: (error) => toast.error(errorMessage(error)) },
                  )
                }
                onClose={() => {
                  if (!window.confirm("Close this poll? Nobody will be able to vote after that."))
                    return;

                  close.mutate(poll.id, {
                    onSuccess: () => toast.success("Poll closed."),
                    onError: (error) => toast.error(errorMessage(error)),
                  });
                }}
                onRemove={() => {
                  if (!window.confirm("Remove this poll entirely? This cannot be undone.")) return;

                  remove.mutate(poll.id, {
                    onSuccess: () => toast.success("Poll removed."),
                    onError: (error) => toast.error(errorMessage(error)),
                  });
                }}
              />
            ))}
          </div>
        )}
      </PageBody>

      <CreatePollDialog
        open={showCreate}
        onClose={() => setShowCreate(false)}
        isPending={create.isPending}
        error={create.error}
        onCreate={(question, options) =>
          create.mutate(
            { question, options },
            {
              onSuccess: () => {
                setShowCreate(false);
                setFilter("open");
                toast.success("Poll created.");
              },
            },
          )
        }
      />
    </Page>
  );
}

function FilterTab({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={
        active
          ? "px-3 h-7 rounded-[--radius-sm] bg-surface text-body text-[12.5px] font-medium shadow-sm"
          : "px-3 h-7 rounded-[--radius-sm] text-muted text-[12.5px] hover:text-body transition-colors"
      }
    >
      {children}
    </button>
  );
}
