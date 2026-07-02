import { Navigate } from "react-router-dom";
import { useSelector } from "react-redux";
import type { RootState } from "../store/store";

interface Props {
    children: React.ReactNode;
}

export default function ProtectedRoute({ children }: Props) {
    // isAuthenticated is derived from userId presence in Redux (rehydrated from localStorage).
    // If the cookie has expired, the first API call will 401 → refresh → logout.
    const isAuthenticated = useSelector((state: RootState) => state.auth.isAuthenticated);

    if (!isAuthenticated) {
        return <Navigate to="/login" replace />;
    }

    return <>{children}</>;
}