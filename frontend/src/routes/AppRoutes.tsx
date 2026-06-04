import {
    BrowserRouter,
    Routes,
    Route
} from "react-router-dom";
import PrivateChatPage
    from "../pages/PrivateChat/PrivateChatPage";
import LoginPage
    from "../pages/Login/LoginPage";

import RegisterPage
    from "../pages/Register/RegisterPage";

import DashboardPage
    from "../pages/Dashboard/DashboardPage";

import ProtectedRoute
    from "./ProtectedRoute";


export default function AppRoutes() {
    return (
        <BrowserRouter>
            <Routes>

                <Route
                    path="/"
                    element={<LoginPage />}
                />

                <Route
                    path="/register"
                    element={<RegisterPage />}
                />

                <Route
                    path="/dashboard"
                    element={
                        <ProtectedRoute>
                            <DashboardPage />
                        </ProtectedRoute>
                    }
                />
                <Route
                    path="/dm/:userId"
                    element={
                        <ProtectedRoute>
                            <PrivateChatPage />
                        </ProtectedRoute>
                    }
                />

            </Routes>
        </BrowserRouter>
    );
}