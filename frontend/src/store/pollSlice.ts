import { createSlice, type PayloadAction } from "@reduxjs/toolkit";
import type { Poll } from "../types/Poll";

interface PollState {
    polls: Poll[];
    loading: boolean;
}

const initialState: PollState = {
    polls: [],
    loading: false
};

const pollSlice = createSlice({
    name: "polls",
    initialState,
    reducers: {
        setPolls: (state, action: PayloadAction<Poll[]>) => {
            state.polls = action.payload;
        },

        addPoll: (state, action: PayloadAction<Poll>) => {
            state.polls.unshift(action.payload);
        },

        updatePoll: (state, action: PayloadAction<Poll>) => {
            const idx = state.polls.findIndex(
                p => p.id === action.payload.id
            );
            if (idx !== -1) {
                const existing = state.polls[idx];
                state.polls[idx] = {
                    ...action.payload,
                    userVoteOptionId: existing.userVoteOptionId,
                    userReaction: existing.userReaction
                };
            }
        },

        setUserVote: (state, action: PayloadAction<{ pollId: string; optionId: string | null }>) => {
            const p = state.polls.find(p => p.id === action.payload.pollId);
            if (p) p.userVoteOptionId = action.payload.optionId;
        },

        setUserReaction: (state, action: PayloadAction<{ pollId: string; isUpvote: boolean | null }>) => {
            const p = state.polls.find(p => p.id === action.payload.pollId);
            if (p) p.userReaction = action.payload.isUpvote;
        },

        setLoading: (state, action: PayloadAction<boolean>) => {
            state.loading = action.payload;
        }
    }
});

export const {
    setPolls,
    addPoll,
    updatePoll,
    setUserVote,
    setUserReaction,
    setLoading
} = pollSlice.actions;

export default pollSlice.reducer;
