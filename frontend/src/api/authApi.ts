import { api } from "./client";

export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    fullName: string;
    email: string;
    password: string;
    department: string;
    branch: string;
}

export interface User {
    id: string;
    fullName: string;
    email: string;
}

export const login = async (
    request: LoginRequest
) => {
    const response =
        await api.post(
            "/api/auth/login",
            request
        );

    return response.data;
};

export const register = async (
    request: RegisterRequest
) => {
    const response =
        await api.post(
            "/api/auth/register",
            request
        );

    return response.data;
};

export const getMe = async () => {
    const response =
        await api.get(
            "/api/auth/me"
        );

    return response.data;
};

export const getUsers = async () => {
    const response =
        await api.get(
            "/api/auth/users"
        );

    return response.data as User[];
};
export const getUserById =
    async (
        userId: string
    ) => {

        const response =
            await api.get(
                `/api/auth/users/${userId}`
            );

        return response.data;
    };