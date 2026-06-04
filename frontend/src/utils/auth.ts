export const getToken = () => {
    return localStorage.getItem(
        "token"
    );
};

export const getAnonymousName = () => {
    return localStorage.getItem(
        "anonymousName"
    ) ?? "Anonymous";
};

export const logout = () => {

    localStorage.clear();

    window.location.href = "/";
};