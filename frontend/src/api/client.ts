import axios from "axios";

// ── Auth Service (HTTP port 5111) ──
export const api = axios.create({
    baseURL: "http://localhost:5111",
    headers: {
        "Content-Type": "application/json"
    }
});

// ── Chat Service (HTTP port 5139) ──
export const chatApiClient = axios.create({
    baseURL: "http://localhost:5139",
    headers: {
        "Content-Type": "application/json"
    }
});

// ── PrivateChat Service (HTTP port 5172) ──
export const privateChatApiClient = axios.create({
    baseURL: "http://localhost:5172",
    headers: {
        "Content-Type": "application/json"
    }
});

// ── Notification Service (HTTP port 5262) ──
export const notificationApiClient = axios.create({
    baseURL: "http://localhost:5262",
    headers: {
        "Content-Type": "application/json"
    }
});

// ── Poll Service (HTTP port 5292) ──
export const pollApiClient = axios.create({
    baseURL: "http://localhost:5292",
    headers: {
        "Content-Type": "application/json"
    }
});

// ── Admin Service (HTTP port 5145) ──
export const adminApiClient = axios.create({
    baseURL: "http://localhost:5145",
    headers: {
        "Content-Type": "application/json"
    }
});

// Shared JWT interceptor for all clients
const addAuthInterceptor = (
    client: ReturnType<typeof axios.create>
) => {
    client.interceptors.request.use(config => {
        const token = localStorage.getItem("token");
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    });

    client.interceptors.response.use(
        response => response,
        error => {
            if (error.response?.status === 401) {
                // Token is invalid or expired
                localStorage.removeItem("token");
                localStorage.removeItem("userId");
                localStorage.removeItem("anonymousName");
                localStorage.removeItem("email");
                localStorage.removeItem("role");
                window.location.href = "/login";
            }
            return Promise.reject(error);
        }
    );
};

addAuthInterceptor(api);
addAuthInterceptor(chatApiClient);
addAuthInterceptor(privateChatApiClient);
addAuthInterceptor(notificationApiClient);
addAuthInterceptor(pollApiClient);
addAuthInterceptor(adminApiClient);