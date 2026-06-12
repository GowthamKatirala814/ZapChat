import { api } from "./client";
import type { User } from "../types/User";

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

export interface AuthResponse {
    token: string;
    userId: string;
    anonymousName: string;
    email?: string;
    role?: "user" | "admin";
}

export interface ProfileData {
    userId: string;
    email: string;
    fullName: string;
    department: string;
    branch: string;
    createdAt: string;
    anonymousName: string;
    roles: string[];
}

export interface UpdateProfileRequest {
    department: string;
    branch: string;
}

export const login = async (request: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post("/api/auth/login", request);
    return response.data as AuthResponse;
};

export const register = async (request: RegisterRequest): Promise<AuthResponse> => {
    const response = await api.post("/api/auth/register", request);
    return response.data as AuthResponse;
};

export const getMe = async (): Promise<ProfileData> => {
    const response = await api.get("/api/auth/me");
    return response.data as ProfileData;
};

export const updateMe = async (request: UpdateProfileRequest): Promise<{ department: string; branch: string }> => {
    const response = await api.patch("/api/auth/me", request);
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

// ── Forgot Password / OTP / Reset Password ─────────────────────────────────

export interface ForgotPasswordResponse {
    success: boolean;
    message: string;
}

export interface VerifyOtpResponse {
    success: boolean;
    resetToken?: string;
    message: string;
}

export interface ResetPasswordResponse {
    success: boolean;
    message: string;
}

export const forgotPassword = async (email: string): Promise<ForgotPasswordResponse> => {
    const response = await api.post("/api/auth/forgot-password", { email });
    return response.data as ForgotPasswordResponse;
};

export const verifyOtp = async (email: string, otpCode: string): Promise<VerifyOtpResponse> => {
    const response = await api.post("/api/auth/verify-otp", { email, otpCode });
    return response.data as VerifyOtpResponse;
};

export const resetPassword = async (
    resetToken: string,
    newPassword: string,
    confirmPassword: string
): Promise<ResetPasswordResponse> => {
    const response = await api.post("/api/auth/reset-password", {
        resetToken,
        newPassword,
        confirmPassword,
    });
    return response.data as ResetPasswordResponse;
};

// ── Multi-step Registration ────────────────────────────────────────────────

export interface InitiateRegistrationRequest {
    fullName: string;
    email: string;
    department: string;
    branch: string;
}

export interface InitiateRegistrationResponse {
    success: boolean;
    message: string;
}

export interface VerifyRegistrationOtpRequest {
    email: string;
    otpCode: string;
}

export interface VerifyRegistrationOtpResponse {
    success: boolean;
    message: string;
    verificationToken?: string;
}

export interface CompleteRegistrationRequest {
    verificationToken: string;
    password: string;
    confirmPassword: string;
}

export interface CompleteRegistrationResponse {
    success: boolean;
    message: string;
}

export const initiateRegistration = async (
    data: InitiateRegistrationRequest
): Promise<InitiateRegistrationResponse> => {
    const response = await api.post("/api/auth/register/initiate", data);
    return response.data as InitiateRegistrationResponse;
};

export const verifyRegistrationOtp = async (
    data: VerifyRegistrationOtpRequest
): Promise<VerifyRegistrationOtpResponse> => {
    const response = await api.post("/api/auth/register/verify-otp", data);
    return response.data as VerifyRegistrationOtpResponse;
};

export const completeRegistration = async (
    data: CompleteRegistrationRequest
): Promise<CompleteRegistrationResponse> => {
    const response = await api.post("/api/auth/register/complete", data);
    return response.data as CompleteRegistrationResponse;
};