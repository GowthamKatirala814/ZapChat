import { Ban, Building2, Search, Trash2, Users } from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";
import { EmptyState, ErrorState, Skeleton } from "../../components/feedback";
import {
  Avatar, Badge, Button, Card, Field, Input, Modal, Select, Textarea,
} from "../../components/ui";
import { formatCount, formatDate } from "../../lib/format";
import { useDebounced } from "../../lib/hooks";
import { errorMessage } from "../../services/api";
import type { AdminUser } from "../../types/api";
import { BRANCHES } from "../auth/constants";
import { useAdminUsers, useModerationMutations, useUserMutations } from "./useAdmin";

/**
 * People.
 *
 * Even here an administrator sees anonymous names rather than real ones: the admin user
 * DTO carries no email and no full name. Administration is possible without breaking the
 * anonymity guarantee, because every action is keyed on the user id.
 */
export function UsersPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [branch, setBranch] = useState("");

  const debouncedSearch = useDebounced(search.trim(), 300);

  const users = useAdminUsers({
    page,
    pageSize: 25,
    search: debouncedSearch || undefined,
    status: status || undefined,
    branch: branch || undefined,
    sortBy: "createdAt",
    sortDesc: true,
  });

  const [deleting, setDeleting] = useState<AdminUser | null>(null);
  const [blocking, setBlocking] = useState<AdminUser | null>(null);
  const [movingBranch, setMovingBranch] = useState<AdminUser | null>(null);

  const items = users.data?.items ?? [];

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-end gap-2 flex-wrap">
        <div className="relative flex-1 min-w-[200px]">
          <Search
            size={15}
            className="absolute left-2.5 top-1/2 -translate-y-1/2 text-faint pointer-events-none"
          />
          <Input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder="Search by anonymous name or department"
            aria-label="Search people"
            className="pl-8"
          />
        </div>

        <Select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value);
            setPage(1);
          }}
          aria-label="Filter by status"
          className="w-auto"
        >
          <option value="">All statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Disabled</option>
          <option value="deleted">Deleted</option>
        </Select>

        <Select
          value={branch}
          onChange={(e) => {
            setBranch(e.target.value);
            setPage(1);
          }}
          aria-label="Filter by office"
          className="w-auto"
        >
          <option value="">All offices</option>
          {BRANCHES.map((item) => (
            <option key={item} value={item}>
              {item}
            </option>
          ))}
        </Select>
      </div>

      {users.isLoading ? (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-16 rounded-[--radius-DEFAULT]" count={6} />
        </div>
      ) : users.error ? (
        <ErrorState error={users.error} onRetry={() => void users.refetch()} />
      ) : items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<Users size={20} />}
            title="Nobody matches"
            description="Try a different search or filter."
          />
        </Card>
      ) : (
        <>
          <div className="flex flex-col gap-2">
            {items.map((user) => (
              <UserRow
                key={user.id}
                user={user}
                onDelete={() => setDeleting(user)}
                onBlock={() => setBlocking(user)}
                onMoveBranch={() => setMovingBranch(user)}
              />
            ))}
          </div>

          {(users.data?.totalPages ?? 1) > 1 && (
            <div className="flex items-center justify-between">
              <span className="text-[12.5px] text-faint">
                Page {users.data!.page} of {users.data!.totalPages} ·{" "}
                {formatCount(users.data!.totalCount)} people
              </span>
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant="secondary"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                >
                  Previous
                </Button>
                <Button
                  size="sm"
                  variant="secondary"
                  disabled={page >= (users.data?.totalPages ?? 1)}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      {deleting && <DeleteDialog user={deleting} onClose={() => setDeleting(null)} />}
      {blocking && <BlockDialog user={blocking} onClose={() => setBlocking(null)} />}
      {movingBranch && <BranchDialog user={movingBranch} onClose={() => setMovingBranch(null)} />}
    </div>
  );
}

function UserRow({
  user,
  onDelete,
  onBlock,
  onMoveBranch,
}: {
  user: AdminUser;
  onDelete: () => void;
  onBlock: () => void;
  onMoveBranch: () => void;
}) {
  return (
    <Card padded={false} className="p-3 flex items-center gap-3">
      <Avatar name={user.anonymousName} size={36} />

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="text-[14px] font-medium text-body truncate">{user.anonymousName}</span>

          {user.isDeleted && <Badge tone="danger">Deleted</Badge>}
          {!user.isDeleted && !user.isActive && <Badge tone="warning">Disabled</Badge>}
          {user.isLockedOut && <Badge tone="warning">Locked out</Badge>}
          {user.roles.some((role) => role.toLowerCase() === "admin") && (
            <Badge tone="accent">Admin</Badge>
          )}
        </div>

        <p className="text-[12.5px] text-faint mt-0.5 truncate">
          {user.department} · {user.branch} · joined {formatDate(user.createdAt)}
        </p>

        {user.isDeleted && user.deletionReason && (
          <p className="text-[12px] text-danger mt-0.5 truncate">
            Deleted{user.deletedAt && ` ${formatDate(user.deletedAt)}`}: {user.deletionReason}
          </p>
        )}
      </div>

      {/* Nothing can be done to an already-deleted account. */}
      {!user.isDeleted && (
        <div className="flex items-center gap-1 shrink-0">
          <Button
            size="icon"
            variant="ghost"
            onClick={onMoveBranch}
            aria-label="Change office"
            title="Change office — decides which branch channel they can read"
          >
            <Building2 size={15} />
          </Button>
          <Button
            size="icon"
            variant="ghost"
            onClick={onBlock}
            aria-label="Block account"
            title="Block account"
          >
            <Ban size={15} />
          </Button>
          <Button
            size="icon"
            variant="ghost"
            onClick={onDelete}
            aria-label="Delete account"
            title="Delete account"
          >
            <Trash2 size={15} />
          </Button>
        </div>
      )}
    </Card>
  );
}

function DeleteDialog({ user, onClose }: { user: AdminUser; onClose: () => void }) {
  const { deleteUser } = useUserMutations();
  const [reason, setReason] = useState("");

  return (
    <Modal
      open
      onClose={onClose}
      title={`Delete ${user.anonymousName}`}
      description="A soft delete: the account can no longer sign in, and their messages remain but are attributed to a deleted user. The reason is recorded in the audit log."
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>
            Cancel
          </Button>
          <Button
            variant="danger"
            size="sm"
            loading={deleteUser.isPending}
            disabled={reason.trim().length < 3}
            onClick={() =>
              deleteUser.mutate(
                { userId: user.id, reason: reason.trim() },
                {
                  onSuccess: () => {
                    toast.success("Account deleted.");
                    onClose();
                  },
                  onError: (error) => toast.error(errorMessage(error)),
                },
              )
            }
          >
            Delete account
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        {deleteUser.error != null && <ErrorState error={deleteUser.error} compact />}

        <Field label="Reason" htmlFor="deleteReason" required>
          <Textarea
            id="deleteReason"
            rows={3}
            autoFocus
            maxLength={500}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Why is this account being deleted?"
          />
        </Field>
      </div>
    </Modal>
  );
}

function BlockDialog({ user, onClose }: { user: AdminUser; onClose: () => void }) {
  const { blockUser } = useModerationMutations();
  const [reason, setReason] = useState("");

  return (
    <Modal
      open
      onClose={onClose}
      title={`Block ${user.anonymousName}`}
      description="Blocking prevents this account from signing in. It can be undone from the moderation page."
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>
            Cancel
          </Button>
          <Button
            variant="danger"
            size="sm"
            loading={blockUser.isPending}
            disabled={reason.trim().length < 3}
            onClick={() =>
              blockUser.mutate(
                { userId: user.id, reason: reason.trim() },
                {
                  onSuccess: () => {
                    toast.success("Account blocked.");
                    onClose();
                  },
                  onError: (error) => toast.error(errorMessage(error)),
                },
              )
            }
          >
            Block account
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        {blockUser.error != null && <ErrorState error={blockUser.error} compact />}

        <Field label="Reason" htmlFor="blockReason" required>
          <Textarea
            id="blockReason"
            rows={3}
            autoFocus
            maxLength={500}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
          />
        </Field>
      </div>
    </Modal>
  );
}

function BranchDialog({ user, onClose }: { user: AdminUser; onClose: () => void }) {
  const { setBranch } = useUserMutations();
  const [value, setValue] = useState(user.branch);

  return (
    <Modal
      open
      onClose={onClose}
      title={`Move ${user.anonymousName}`}
      description="A person's office decides which branch channel they can read, which is why it is administered rather than self-selected."
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>
            Cancel
          </Button>
          <Button
            size="sm"
            loading={setBranch.isPending}
            disabled={value === user.branch}
            onClick={() =>
              setBranch.mutate(
                { userId: user.id, branch: value },
                {
                  onSuccess: () => {
                    toast.success("Office updated.");
                    onClose();
                  },
                  onError: (error) => toast.error(errorMessage(error)),
                },
              )
            }
          >
            Save
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        {setBranch.error != null && <ErrorState error={setBranch.error} compact />}

        <Field label="Office" htmlFor="userBranch">
          <Select id="userBranch" value={value} onChange={(e) => setValue(e.target.value)}>
            {/* The stored value may predate this list, so it is always offered. */}
            {!BRANCHES.includes(user.branch as (typeof BRANCHES)[number]) && (
              <option value={user.branch}>{user.branch}</option>
            )}
            {BRANCHES.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </Select>
        </Field>
      </div>
    </Modal>
  );
}
