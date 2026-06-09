import { pollApiClient } from "./client";
import type { Poll } from "../types/Poll";

export const getAllPolls = async (userId: string | null = null): Promise<Poll[]> => {
    const url = userId ? `/api/poll?userId=${userId}` : "/api/poll";
    const response = await pollApiClient.get(url);
    return response.data;
};

export const getPoll = async (pollId: string, userId: string | null = null): Promise<Poll> => {
    const url = userId ? `/api/poll/${pollId}?userId=${userId}` : `/api/poll/${pollId}`;
    const response = await pollApiClient.get(url);
    return response.data;
};

export const createPoll = async (
    question: string,
    options: string[],
    creatorId: string
): Promise<Poll> => {
    const response = await pollApiClient.post("/api/poll", {
        question,
        options,
        creatorId
    });
    return response.data;
};

export const voteOnPoll = async (
    pollId: string,
    userId: string,
    optionId: string | null
): Promise<Poll> => {
    const response = await pollApiClient.post("/api/poll/vote", {
        pollId,
        optionId,
        userId
    });
    return response.data;
};

export const reactToPoll = async (
    pollId: string,
    userId: string,
    isUpvote: boolean | null
): Promise<Poll> => {
    const response = await pollApiClient.post("/api/poll/react", {
        pollId,
        userId,
        isUpvote
    });
    return response.data;
};
