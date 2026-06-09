export interface PollOption {
    id: string;
    optionText: string;
    voteCount: number;
}

export interface Poll {
    id: string;
    question: string;
    createdAt: string;
    creatorId: string | null;
    upvotes: number;
    downvotes: number;
    userVoteOptionId: string | null;
    userReaction: boolean | null;
    options: PollOption[];
}
