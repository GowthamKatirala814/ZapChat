import { Search, UserPlus } from "lucide-react";
import { useMemo, useState } from "react";
import { EmptyState, ErrorState, Skeleton } from "../../components/feedback";
import { Avatar, Badge, Input, Modal } from "../../components/ui";
import { useCurrentUser } from "../../app/providers";
import { useDebounced } from "../../lib/hooks";
import { useDirectory } from "./usePrivateChat";

/**
 * Starting a conversation.
 *
 * The directory is the platform's public view of its users: an anonymous name, a
 * department and a branch, and nothing else. There is no email and no real name here
 * because the endpoint does not return them — the anonymity guarantee is enforced by the
 * shape of the data, not by the UI choosing what to display.
 */
export function NewConversationDialog({
  open,
  onClose,
  onSelect,
  isStarting,
}: {
  open: boolean;
  onClose: () => void;
  onSelect: (userId: string) => void;
  isStarting: boolean;
}) {
  const me = useCurrentUser();
  const directory = useDirectory();

  const [search, setSearch] = useState("");
  const query = useDebounced(search.trim().toLowerCase(), 150);

  const people = useMemo(() => {
    const all = (directory.data ?? []).filter(
      // Yourself and deleted accounts are not valid recipients; the server rejects both.
      (user) => user.id !== me.userId && !user.isDeleted,
    );

    if (!query) return all;

    return all.filter(
      (user) =>
        user.anonymousName.toLowerCase().includes(query) ||
        user.department.toLowerCase().includes(query) ||
        user.branch.toLowerCase().includes(query),
    );
  }, [directory.data, me.userId, query]);

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="New conversation"
      description="Pick someone to message. You will both appear under your anonymous names."
      width={480}
    >
      <div className="flex flex-col gap-3">
        <div className="relative">
          <Search
            size={15}
            className="absolute left-2.5 top-1/2 -translate-y-1/2 text-faint pointer-events-none"
          />
          <Input
            autoFocus
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name, department or office"
            aria-label="Search people"
            className="pl-8"
          />
        </div>

        <div className="max-h-[340px] overflow-y-auto -mx-1 px-1">
          {directory.isLoading ? (
            <div className="flex flex-col gap-2">
              <Skeleton className="h-12 rounded-[--radius-DEFAULT]" count={5} />
            </div>
          ) : directory.error ? (
            <ErrorState error={directory.error} onRetry={() => void directory.refetch()} compact />
          ) : people.length === 0 ? (
            <EmptyState
              icon={<UserPlus size={20} />}
              title={search ? "Nobody matches" : "No one else is here yet"}
              description={
                search ? "Try a different search term." : "You are the only registered user."
              }
            />
          ) : (
            <ul className="flex flex-col gap-0.5">
              {people.map((person) => (
                <li key={person.id}>
                  <button
                    type="button"
                    disabled={isStarting}
                    onClick={() => onSelect(person.id)}
                    className="w-full flex items-center gap-3 p-2 rounded-[--radius-DEFAULT] hover:bg-surface-2 transition-colors text-left disabled:opacity-60"
                  >
                    <Avatar name={person.anonymousName} size={34} />
                    <span className="min-w-0 flex-1">
                      <span className="block text-[13.5px] font-medium text-body truncate">
                        {person.anonymousName}
                      </span>
                      <span className="block text-[12px] text-faint truncate">
                        {person.department}
                      </span>
                    </span>
                    <Badge>{person.branch}</Badge>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </Modal>
  );
}
