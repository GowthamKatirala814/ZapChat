import { clsx } from "clsx";
import { ArrowDown } from "lucide-react";
import {
  useCallback, useEffect, useLayoutEffect, useRef, useState, type ReactNode,
} from "react";
import { Spinner } from "../feedback";
import { formatDayLabel } from "../../lib/format";

/**
 * Scroll behaviour for a message log.
 *
 * Three things have to hold at once, and getting any one wrong is immediately obvious:
 *
 *  1. Opening a conversation lands at the newest message.
 *  2. Loading older messages keeps the user's eye on the same message — so the scroll
 *     offset is restored from the *height difference* after the prepend, not reset.
 *  3. A new message auto-scrolls only if the user was already at the bottom. Yanking
 *     someone away from what they are reading is the classic chat bug; when they are
 *     scrolled up they get a "new messages" button instead.
 */
export function MessageScroller({
  children,
  itemCount,
  /** Identifies the conversation; changing it re-anchors to the bottom. */
  scopeKey,
  hasMore,
  isLoadingMore,
  onLoadMore,
}: {
  children: ReactNode;
  itemCount: number;
  scopeKey: string;
  hasMore: boolean;
  isLoadingMore: boolean;
  onLoadMore: () => void;
}) {
  const viewportRef = useRef<HTMLDivElement>(null);
  const sentinelRef = useRef<HTMLDivElement>(null);

  const [atBottom, setAtBottom] = useState(true);
  const [unseen, setUnseen] = useState(0);

  const previousCount = useRef(itemCount);
  const previousScope = useRef(scopeKey);
  /** Scroll height captured immediately before an older page is prepended. */
  const restoreRef = useRef<number | null>(null);

  const scrollToBottom = useCallback((behavior: ScrollBehavior = "auto") => {
    const viewport = viewportRef.current;
    if (!viewport) return;

    viewport.scrollTo({ top: viewport.scrollHeight, behavior });
    setUnseen(0);
  }, []);

  // Track whether the user is pinned to the bottom. The 80px tolerance covers the
  // sub-pixel rounding that otherwise makes "at the bottom" flicker false.
  const handleScroll = useCallback(() => {
    const viewport = viewportRef.current;
    if (!viewport) return;

    const distance = viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight;
    const bottom = distance < 80;

    setAtBottom(bottom);
    if (bottom) setUnseen(0);
  }, []);

  // Infinite scroll upward. The sentinel sits above the first message; `rootMargin`
  // starts the fetch before it is actually visible so the join is seamless.
  useEffect(() => {
    const sentinel = sentinelRef.current;
    const viewport = viewportRef.current;

    if (!sentinel || !viewport || !hasMore) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && !isLoadingMore) {
          restoreRef.current = viewport.scrollHeight;
          onLoadMore();
        }
      },
      { root: viewport, rootMargin: "220px 0px 0px 0px" },
    );

    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [hasMore, isLoadingMore, onLoadMore]);

  useLayoutEffect(() => {
    const viewport = viewportRef.current;
    if (!viewport) return;

    // A different conversation: start at the newest message.
    if (previousScope.current !== scopeKey) {
      previousScope.current = scopeKey;
      previousCount.current = itemCount;
      restoreRef.current = null;

      scrollToBottom();
      setAtBottom(true);
      return;
    }

    const added = itemCount - previousCount.current;
    previousCount.current = itemCount;

    if (added <= 0) return;

    // Older page prepended: hold the viewport still.
    if (restoreRef.current !== null) {
      viewport.scrollTop += viewport.scrollHeight - restoreRef.current;
      restoreRef.current = null;
      return;
    }

    // New message at the bottom.
    if (atBottom) {
      scrollToBottom();
    } else {
      setUnseen((count) => count + added);
    }
  }, [itemCount, scopeKey, atBottom, scrollToBottom]);

  return (
    <div className="relative flex-1 min-h-0">
      <div
        ref={viewportRef}
        onScroll={handleScroll}
        className="h-full overflow-y-auto overscroll-contain"
      >
        <div ref={sentinelRef} aria-hidden />

        {isLoadingMore && (
          <div className="flex items-center justify-center gap-2 py-3 text-faint text-[12.5px]">
            <Spinner size={13} />
            Loading earlier messages…
          </div>
        )}

        {!hasMore && itemCount > 0 && (
          <p className="text-center text-[12px] text-faint py-4 px-4">
            This is the beginning of the conversation.
          </p>
        )}

        <div className="pb-3">{children}</div>
      </div>

      {(unseen > 0 || !atBottom) && (
        <button
          type="button"
          onClick={() => scrollToBottom("smooth")}
          className={clsx(
            "absolute bottom-3 left-1/2 -translate-x-1/2 z-10",
            "inline-flex items-center gap-1.5 h-8 px-3 rounded-[--radius-full]",
            "shadow-lg text-[12.5px] font-medium transition-colors zc-enter",
            unseen > 0
              ? "bg-accent text-accent-contrast hover:bg-accent-hover"
              : "bg-surface border border-line text-muted hover:text-body",
          )}
        >
          <ArrowDown size={14} />
          {unseen > 0 ? `${unseen} new message${unseen === 1 ? "" : "s"}` : "Jump to latest"}
        </button>
      )}
    </div>
  );
}

/** A dated divider between message groups. */
export function DayDivider({ date }: { date: string }) {
  return (
    <div className="flex items-center gap-3 px-4 my-4" role="separator">
      <span className="flex-1 h-px bg-line-subtle" />
      <span className="text-[11.5px] font-medium text-faint whitespace-nowrap">
        {formatDayLabel(date)}
      </span>
      <span className="flex-1 h-px bg-line-subtle" />
    </div>
  );
}
