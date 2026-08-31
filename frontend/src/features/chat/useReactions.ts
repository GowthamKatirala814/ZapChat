import { useQuery } from "@tanstack/react-query";
import { http, unwrap } from "../../services/api/client";
import { useAuth } from "../../app/providers";

/**
 * The reactions the server accepts, fetched from the server.
 *
 * The picker used to hold its own hardcoded list, which had drifted from the two
 * server-side copies: it offered two emoji the API rejected with "That is not an
 * available reaction" — so two of six buttons silently failed — and omitted four the API
 * did accept. Reading the list from `GET /api/rooms/reaction-options` makes that class of
 * mismatch impossible: there is one definition, in ZapChat.Shared, and the UI renders it.
 */

export interface ReactionOption {
  emoji: string;
  /** Stable ASCII id — safe to log, assert on, and use as a React key. */
  name: string;
  /** Human label for the tooltip and screen readers. */
  label: string;
}

/**
 * Fallback used only if the request fails.
 *
 * Deliberately a strict SUBSET of the server list, never a superset: if this is ever
 * wrong it can only under-offer, which is a missing button. Over-offering would put back
 * exactly the broken-button bug this replaces.
 */
const MINIMAL_FALLBACK: ReactionOption[] = [
  { emoji: "\u{1F44D}", name: "thumbs_up", label: "Thumbs up" },
  { emoji: "\u2764\uFE0F", name: "heart", label: "Heart" },
];

export function useReactionOptions() {
  const { isAuthenticated } = useAuth();

  const query = useQuery({
    queryKey: ["reaction-options"],
    queryFn: () => unwrap<ReactionOption[]>(http.get("/api/rooms/reaction-options")),
    enabled: isAuthenticated,
    // Static per deployment; the server sends a one-hour cache header to match.
    staleTime: 60 * 60_000,
    gcTime: 24 * 60 * 60_000,
  });

  return {
    options: query.data ?? MINIMAL_FALLBACK,
    isLoading: query.isLoading,
    /** True when the server list could not be read and the subset is in use. */
    isDegraded: Boolean(query.error),
  };
}
