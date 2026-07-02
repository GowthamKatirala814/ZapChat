// getAnonymousName still reads from localStorage (non-sensitive profile data)
export const getAnonymousName = () => {
    return localStorage.getItem("anonymousName") ?? "Anonymous";
};

// Logout: calls the backend to delete the refresh token from DB and clear cookies,
// then clears Redux state + localStorage profile data and redirects.
export const logout = async () => {
    try {
        // Server-side: delete refresh token from DB and clear HttpOnly cookies
        await fetch("https://localhost:5000/api/auth/logout", {
            method: "POST",
            credentials: "include",
        });
    } catch {
        // Non-fatal: even if the network request fails, clear client state
    } finally {
        // Clear non-sensitive profile data from localStorage
        localStorage.removeItem("userId");
        localStorage.removeItem("anonymousName");
        localStorage.removeItem("email");
        localStorage.removeItem("role");

        window.location.href = "/";
    }
};