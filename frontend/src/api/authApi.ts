import { api } from "./client";
import type { User } from "../types/User";

export interface LoginRequest {
    email: string;
    password: string;
    role?: "user" | "admin";
}

export interface RegisterRequest {
    fullName: string;
    email: string;
    password: string;
    department: string;
    branch: string;
}

export interface AuthResponse {
    token: string;
    userId: string;
    anonymousName: string;
    email?: string;
    role?: "user" | "admin";
}

export const login = async (request: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post("/api/auth/login", request);
    return response.data as AuthResponse;
};

export const register = async (request: RegisterRequest): Promise<AuthResponse> => {
    const response = await api.post("/api/auth/register", request);
    return response.data as AuthResponse;
};

export const getMe = async () => {
    const response = await api.get("/api/auth/me");
    return response.data;
};

export const getUsers = async (): Promise<User[]> => {
    const response = await api.get("/api/auth/users");
    return response.data as User[];
};

export const getUserById = async (userId: string): Promise<User> => {
    const response = await api.get(`/api/auth/users/${userId}`);
    return response.data as User;
};