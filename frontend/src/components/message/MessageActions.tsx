import { clsx } from "clsx";
import { Flag, MoreHorizontal, Pencil, Reply, Trash2 } from "lucide-react";
import { useState } from "react";
import { useDismissable } from "../../lib/hooks";
import { ReactionPicker } from "./ReactionPicker";
import type { Reaction } from "../../types/api";

/**
 * The hover toolbar on a message.
 *
 * Which controls appear is decided by the server's `isMine` flag, not by comparing a
 * locally-stored user id — and the server enforces the same rule regardless, so a user
 * who forces the button open still gets a 403.
 *
 * "Report" is deliberately absent on your own messages: reporting yourself is noise in
 * the moderation queue, and the server rejects it anyway.
 */


export function MessageActions({
  isMine,
  canEdit,
  isDeleted,
  reactions,
  onReply,
  onEdit,
  onDelete,
  onReport,
  onReact,
  align = "right",
}: {
  isMine: boolean;
  /** False once the server's 15-minute edit window has closed. */
  canEdit: boolean;
  isDeleted: boolean;
  /** Current reactions, so the picker can render the caller's own as pressed. */
  reactions: Reaction[];
  onReply: () => void;
  onEdit: () => void;
  onDelete: () => void;
  onReport: () => void;
  onReact: (emoji: string) => void;
  align?: "left" | "right";
}) {
  const [showMenu, setShowMenu] = useState(false);

  useDismissable(showMenu, () => setShowMenu(false));

  // Nothing can be done to a removed message: it has no content to quote, react to or
  // report, and it is already gone.
  if (isDeleted) return null;

  return (
    <div
      className={clsx(
        "absolute -top-3.5 flex items-center gap-0.5 p-0.5 rounded-[--radius-DEFAULT]",
        "bg-surface border border-line shadow-sm",
        "opacity-0 group-hover:opacity-100 focus-within:opacity-100 transition-opacity",
        align === "right" ? "right-2" : "left-2",
      )}
    >
      <ReactionPicker
        reactions={reactions}
        onPick={onReact}
        align={align === "right" ? "right" : "left"}
      />

      <IconButton label="Reply" onClick={onReply}>
        <Reply size={15} />
      </IconButton>

      <div className="relative">
        <IconButton
          label="More actions"
          onClick={(e) => {
            e.stopPropagation();
            setShowMenu((v) => !v);
          }}
        >
          <MoreHorizontal size={15} />
        </IconButton>

        {showMenu && (
          <div
            className="absolute bottom-full mb-1 right-0 min-w-[164px] py-1 rounded-[--radius-DEFAULT] bg-surface border border-line shadow-lg zc-enter"
            onClick={(e) => e.stopPropagation()}
            role="menu"
          >
            {isMine ? (
              <>
                {canEdit && (
                  <MenuItem
                    icon={<Pencil size={14} />}
                    onClick={() => {
                      onEdit();
                      setShowMenu(false);
                    }}
                  >
                    Edit message
                  </MenuItem>
                )}
                <MenuItem
                  icon={<Trash2 size={14} />}
                  destructive
                  onClick={() => {
                    onDelete();
                    setShowMenu(false);
                  }}
                >
                  Delete message
                </MenuItem>
              </>
            ) : (
              <MenuItem
                icon={<Flag size={14} />}
                onClick={() => {
                  onReport();
                  setShowMenu(false);
                }}
              >
                Report message
              </MenuItem>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function IconButton({
  label,
  onClick,
  children,
}: {
  label: string;
  onClick: (event: React.MouseEvent) => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={label}
      title={label}
      className="w-7 h-7 flex items-center justify-center rounded-[--radius-sm] text-muted hover:bg-surface-2 hover:text-body transition-colors"
    >
      {children}
    </button>
  );
}

function MenuItem({
  icon,
  children,
  onClick,
  destructive,
}: {
  icon: React.ReactNode;
  children: React.ReactNode;
  onClick: () => void;
  destructive?: boolean;
}) {
  return (
    <button
      type="button"
      role="menuitem"
      onClick={onClick}
      className={clsx(
        "w-full flex items-center gap-2.5 px-3 py-1.5 text-[13px] text-left transition-colors",
        destructive ? "text-danger hover:bg-danger-soft" : "text-body hover:bg-surface-2",
      )}
    >
      {icon}
      {children}
    </button>
  );
}
