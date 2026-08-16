import { QueryClient } from "@tanstack/react-query";
import { ApiError } from "../services/api/client";

/**
 * Server state lives here, not in Redux.
 *
 * The previous frontend kept messages, rooms and notifications in Redux slices that were
 * hand-synchronised with SignalR events, so a missed event left the store permanently
 * wrong with no way to notice. A query cache makes staleness recoverable: a realtime
 * event is a hint to update, and a refetch is always the fallback.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Realtime pushes most updates, so background polling would be redundant traffic.
      refetchOnWindowFocus: false,
      refetchOnReconnect: true,
      staleTime: 30_000,
      retry: (failureCount, error) => {
        const api = ApiError.from(error);

        // A 4xx will not become a 2xx on retry; retrying a 401 also races the refresh
        // interceptor, which is already handling it.
        if (api && !api.isNetworkError && api.status >= 400 && api.status < 500) return false;

        return failureCount < 2;
      },
    },
    mutations: {
      // A mutation is a user action; silently repeating it can duplicate a message.
      retry: false,
    },
  },
});
