import { ArchiveRestore, Hash, Pencil, Plus, Trash2 } from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";
import { EmptyState, ErrorState, Skeleton } from "../../components/feedback";
import {
  Badge, Button, Card, Field, Input, Modal, Select, Textarea,
} from "../../components/ui";
import { formatCount, formatDate } from "../../lib/format";
import { errorMessage } from "../../services/api";
import type { Room, RoomType } from "../../types/api";
import { BRANCHES } from "../auth/constants";
import { roomAccent } from "../../lib/messages";
import { useAdminRooms, useRoomMutations } from "./useAdmin";

/**
 * Channel administration.
 *
 * Channels are archived rather than deleted: the messages are retained, the channel
 * becomes read-only, and it can be restored. That is the only removal the backend offers,
 * so it is the only one offered here.
 */
export function RoomsPage() {
  const [includeArchived, setIncludeArchived] = useState(true);
  const rooms = useAdminRooms(includeArchived);
  const { create, update, archive, restore } = useRoomMutations();

  const [editing, setEditing] = useState<Room | null>(null);
  const [creating, setCreating] = useState(false);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <label className="flex items-center gap-2 text-[13px] text-muted cursor-pointer">
          <input
            type="checkbox"
            checked={includeArchived}
            onChange={(e) => setIncludeArchived(e.target.checked)}
            className="w-4 h-4 accent-[var(--zc-accent)]"
          />
          Show archived channels
        </label>

        <Button size="sm" icon={<Plus size={15} />} onClick={() => setCreating(true)}>
          New channel
        </Button>
      </div>

      {rooms.isLoading ? (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-[72px] rounded-[--radius-DEFAULT]" count={4} />
        </div>
      ) : rooms.error ? (
        <ErrorState error={rooms.error} onRetry={() => void rooms.refetch()} />
      ) : (rooms.data?.length ?? 0) === 0 ? (
        <Card>
          <EmptyState
            icon={<Hash size={20} />}
            title="No channels"
            description="Create one to give people somewhere to talk."
            action={
              <Button size="sm" variant="secondary" onClick={() => setCreating(true)}>
                New channel
              </Button>
            }
          />
        </Card>
      ) : (
        <div className="flex flex-col gap-2">
          {rooms.data!.map((room) => (
            <RoomRow
              key={room.id}
              room={room}
              onEdit={() => setEditing(room)}
              onArchive={() => {
                if (
                  !window.confirm(
                    `Archive "${room.name}"? People can still read it, but nobody can post. Messages are kept.`,
                  )
                )
                  return;

                archive.mutate(room.id, {
                  onSuccess: () => toast.success("Channel archived."),
                  onError: (error) => toast.error(errorMessage(error)),
                });
              }}
              onRestore={() =>
                restore.mutate(room.id, {
                  onSuccess: () => toast.success("Channel restored."),
                  onError: (error) => toast.error(errorMessage(error)),
                })
              }
            />
          ))}
        </div>
      )}

      {creating && (
        <RoomDialog
          title="New channel"
          isPending={create.isPending}
          error={create.error}
          onClose={() => setCreating(false)}
          onSubmit={(values) =>
            create.mutate(values, {
              onSuccess: () => {
                toast.success("Channel created.");
                setCreating(false);
              },
            })
          }
        />
      )}

      {editing && (
        <RoomDialog
          title={`Edit ${editing.name}`}
          initial={editing}
          isPending={update.isPending}
          error={update.error}
          onClose={() => setEditing(null)}
          onSubmit={({ name, description }) =>
            update.mutate(
              { roomId: editing.id, name, description },
              {
                onSuccess: () => {
                  toast.success("Channel updated.");
                  setEditing(null);
                },
              },
            )
          }
        />
      )}
    </div>
  );
}

function RoomRow({
  room,
  onEdit,
  onArchive,
  onRestore,
}: {
  room: Room;
  onEdit: () => void;
  onArchive: () => void;
  onRestore: () => void;
}) {
  return (
    <Card padded={false} className="p-3.5 flex items-center gap-3">
      <span
        className="w-9 h-9 rounded-[--radius-DEFAULT] flex items-center justify-center shrink-0"
        style={{
          background: `color-mix(in srgb, ${roomAccent(room.type)} 16%, transparent)`,
          color: roomAccent(room.type),
        }}
        aria-hidden
      >
        <Hash size={16} />
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="text-[14px] font-medium text-body truncate">{room.name}</span>
          <Badge tone={room.type === "Hr" ? "warning" : room.type === "Branch" ? "accent" : "neutral"}>
            {room.type === "Hr" ? "HR" : room.type}
            {room.branch && ` · ${room.branch}`}
          </Badge>
          {room.isArchived && <Badge tone="neutral">Archived</Badge>}
        </div>

        <p className="text-[12.5px] text-faint truncate mt-0.5">
          {room.description || "No description"}
        </p>

        <p className="text-[11.5px] text-faint mt-1 zc-tabular">
          {formatCount(room.memberCount)} members · {formatCount(room.messageCount)} messages ·
          created {formatDate(room.createdAt)}
        </p>
      </div>

      <div className="flex items-center gap-1 shrink-0">
        {!room.isArchived && (
          <Button size="icon" variant="ghost" onClick={onEdit} aria-label="Edit channel" title="Edit">
            <Pencil size={15} />
          </Button>
        )}

        {room.isArchived ? (
          <Button
            size="sm"
            variant="ghost"
            icon={<ArchiveRestore size={15} />}
            onClick={onRestore}
          >
            Restore
          </Button>
        ) : (
          <Button
            size="icon"
            variant="ghost"
            onClick={onArchive}
            aria-label="Archive channel"
            title="Archive"
          >
            <Trash2 size={15} />
          </Button>
        )}
      </div>
    </Card>
  );
}

function RoomDialog({
  title,
  initial,
  onClose,
  onSubmit,
  isPending,
  error,
}: {
  title: string;
  initial?: Room;
  onClose: () => void;
  onSubmit: (values: {
    name: string;
    description: string;
    type: string;
    branch?: string;
  }) => void;
  isPending: boolean;
  error: unknown;
}) {
  const [name, setName] = useState(initial?.name ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");
  const [type, setType] = useState<RoomType>(initial?.type ?? "General");
  const [branch, setBranch] = useState<string>(initial?.branch ?? BRANCHES[0]);

  // The type of an existing channel decides who can read it, so changing it after the
  // fact would silently re-scope its history. The API accepts name and description only.
  const isEdit = Boolean(initial);
  const valid = name.trim().length >= 2;

  return (
    <Modal
      open
      onClose={onClose}
      title={title}
      width={480}
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>
            Cancel
          </Button>
          <Button
            size="sm"
            loading={isPending}
            disabled={!valid}
            onClick={() =>
              onSubmit({
                name: name.trim(),
                description: description.trim(),
                type,
                branch: type === "Branch" ? branch : undefined,
              })
            }
          >
            {isEdit ? "Save changes" : "Create channel"}
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        {error != null && <ErrorState error={error} compact />}

        <Field label="Name" htmlFor="roomName" required>
          <Input
            id="roomName"
            autoFocus
            required
            minLength={2}
            maxLength={60}
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Engineering"
          />
        </Field>

        <Field label="Description" htmlFor="roomDescription" hint="Shown under the channel name.">
          <Textarea
            id="roomDescription"
            rows={2}
            maxLength={500}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </Field>

        {!isEdit && (
          <>
            <Field
              label="Type"
              htmlFor="roomType"
              hint="Decides who can read it. This cannot be changed later."
              required
            >
              <Select
                id="roomType"
                value={type}
                onChange={(e) => setType(e.target.value as RoomType)}
              >
                <option value="General">General — everyone</option>
                <option value="Branch">Branch — one office only</option>
                <option value="Hr">HR — content checks enforced</option>
                <option value="Custom">Custom — everyone</option>
              </Select>
            </Field>

            {type === "Branch" && (
              <Field label="Office" htmlFor="roomBranch" required>
                <Select id="roomBranch" value={branch} onChange={(e) => setBranch(e.target.value)}>
                  {BRANCHES.map((item) => (
                    <option key={item} value={item}>
                      {item}
                    </option>
                  ))}
                </Select>
              </Field>
            )}
          </>
        )}
      </div>
    </Modal>
  );
}
