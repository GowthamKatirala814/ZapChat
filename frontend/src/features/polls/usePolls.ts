import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { keys } from "../../app/queryKeys";
import { useAuth } from "../../app/providers";
import { pollsApi } from "../../services/api";
import { HubEvent } from "../../services/realtime/events";
import type { PollClosedOrDeleted } from "../../services/realtime/events";
import { useHubConnection, useHubEvent, useHubReconnect } from "../../services/realtime/hooks";
import type { Poll } from "../../types/api";

/**
 * Polls.
 *
 * All poll state is the server's. There is no client-side tallying anywhere in this
 * feature: a vote is a POST that returns the recomputed poll, and the percentages shown
 * are the ones the server calculated. The old UI incremented counters locally and drifted
 * from the database the moment two people voted at once.
 */

export function usePolls() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: keys.polls.list(),
    queryFn: () => pollsApi.list(),
    enabled: isAuthenticated,
  });
}

/**
 * Merges a poll that arrived over the wire into the cached list.
 *
 * Poll broadcasts go to every connected client, so they are viewer-neutral: `isMine`,
 * `myVoteOptionId` and `myReaction` are blank in them by construction. Overwriting the
 * cached poll wholesale would therefore erase the reader's own vote from their screen the
 * instant anybody else voted — so the caller-specific fields are carried across.
 */
function mergePoll(existing: Poll | undefined, incoming: Poll): Poll {
  if (!existing) return incoming;

  return {
    ...incoming,
    isMine: existing.isMine,
    myVoteOptionId: existing.myVoteOptionId,
    myReaction: existing.myReaction,
  };
}

export function usePollMutations() {
  const queryClient = useQueryClient();

  /** Replaces one poll with the authoritative copy returned by a mutation. */
  const replace = (poll: Poll) => {
    queryClient.setQueryData<Poll[]>(keys.polls.list(), (polls) =>
      polls?.map((existing) => (existing.id === poll.id ? poll : existing)),
    );
  };

  return {
    create: useMutation({
      mutationFn: ({ question, options }: { question: string; options: string[] }) =>
        pollsApi.create(question, options),
      onSuccess: (poll) => {
        // The creator's own copy has isMine=true; the broadcast that follows does not,
        // and is deduped against this one by id.
        queryClient.setQueryData<Poll[]>(keys.polls.list(), (polls) =>
          polls ? [poll, ...polls.filter((p) => p.id !== poll.id)] : [poll],
        );
      },
    }),

    /** `null` withdraws the vote — the server treats that as a first-class action. */
    vote: useMutation({
      mutationFn: ({ pollId, optionId }: { pollId: string; optionId: string | null }) =>
        pollsApi.vote(pollId, optionId),
      onSuccess: replace,
    }),

    react: useMutation({
      mutationFn: ({ pollId, isUpvote }: { pollId: string; isUpvote: boolean | null }) =>
        pollsApi.react(pollId, isUpvote),
      onSuccess: replace,
    }),

    close: useMutation({
      mutationFn: (pollId: string) => pollsApi.close(pollId),
      onSuccess: () => {
        void queryClient.invalidateQueries({ queryKey: keys.polls.all });
      },
    }),

    remove: useMutation({
      mutationFn: (pollId: string) => pollsApi.remove(pollId),
      onSuccess: (_, pollId) => {
        queryClient.setQueryData<Poll[]>(keys.polls.list(), (polls) =>
          polls?.filter((poll) => poll.id !== pollId),
        );
      },
    }),
  };
}

/**
 * Live poll updates.
 *
 * The poll hub has no client-callable methods at all — voting used to be a second,
 * divergent implementation there that took the voter's id from the client. This is a
 * listen-only connection.
 */
export function usePollsRealtime() {
  const queryClient = useQueryClient();
  const { isAuthenticated } = useAuth();

  const status = useHubConnection("polls", isAuthenticated);

  useHubEvent(
    "polls",
    HubEvent.PollCreated,
    (poll: Poll) => {
      queryClient.setQueryData<Poll[]>(keys.polls.list(), (polls) => {
        if (!polls) return polls;
        if (polls.some((existing) => existing.id === poll.id)) return polls;

        return [poll, ...polls];
      });
    },
    isAuthenticated,
  );

  useHubEvent(
    "polls",
    HubEvent.PollUpdated,
    (poll: Poll) => {
      queryClient.setQueryData<Poll[]>(keys.polls.list(), (polls) =>
        polls?.map((existing) => (existing.id === poll.id ? mergePoll(existing, poll) : existing)),
      );
    },
    isAuthenticated,
  );

  useHubEvent(
    "polls",
    HubEvent.PollClosed,
    (event: PollClosedOrDeleted) => {
      queryClient.setQueryData<Poll[]>(keys.polls.list(), (polls) =>
        polls?.map((poll) => (poll.id === event.pollId ? { ...poll, status: "Closed" } : poll)),
      );
    },
    isAuthenticated,
  );

  useHubEvent(
    "polls",
    HubEvent.PollDeleted,
    (event: PollClosedOrDeleted) => {
      queryClient.setQueryData<Poll[]>(keys.polls.list(), (polls) =>
        polls?.filter((poll) => poll.id !== event.pollId),
      );
    },
    isAuthenticated,
  );

  useHubReconnect(
    "polls",
    () => {
      void queryClient.invalidateQueries({ queryKey: keys.polls.all });
    },
    isAuthenticated,
  );

  return status;
}
