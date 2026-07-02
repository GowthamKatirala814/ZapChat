import {
    BrowserRouter,
    Routes,
    Route,
    Navigate,
} from "react-router-dom";

import LoginPage from "../pages/Login/LoginPage";
import RegisterPage from "../pages/Register/RegisterPage";
import DashboardPage from "../pages/Dashboard/DashboardPage";
import PrivateChatPage from "../pages/PrivateChat/PrivateChatPage";
import NotificationsPage from "../pages/Notifications/NotificationsPage";
import PollsPage from "../pages/Polls/PollsPage";
import ProfilePage from "../pages/Profile/ProfilePage";
import ProtectedRoute from "./ProtectedRoute";
import AdminLayout from "../pages/Admin/AdminLayout";
import AdminDashboardPage from "../pages/Admin/AdminDashboardPage";
import AdminUsersPage from "../pages/Admin/AdminUsersPage";
import AdminModerationPage from "../pages/Admin/AdminModerationPage";
import AdminAnalyticsPage from "../pages/Admin/AdminAnalyticsPage";
import AdminAiHealthPage from "../pages/Admin/AdminAiHealthPage";
import AdminRoomsPage from "../pages/Admin/AdminRoomsPage";
import AdminAuditLogsPage from "../pages/Admin/AdminAuditLogsPage";
import ForgotPasswordPage from "../pages/ForgotPassword/ForgotPasswordPage";
import VerifyOtpPage from "../pages/VerifyOtp/VerifyOtpPage";
import ResetPasswordPage from "../pages/ResetPassword/ResetPasswordPage";

export default function AppRoutes() {
    return (
        <BrowserRouter>
            <Routes>

                {/* Auth routes */}
                <Route path="/" element={<LoginPage />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />

                {/* Password reset (public) */}
                <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                <Route path="/verify-otp" element={<VerifyOtpPage />} />
                <Route path="/reset-password" element={<ResetPasswordPage />} />

                {/* Protected routes */}
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

                <Route
                    path="/notifications"
                    element={
                        <ProtectedRoute>
                            <NotificationsPage />
                        </ProtectedRoute>
                    }
                />

                <Route
                    path="/polls"
                    element={
                        <ProtectedRoute>
                            <PollsPage />
                        </ProtectedRoute>
                    }
                />

                <Route
                    path="/profile"
                    element={
                        <ProtectedRoute>
                            <ProfilePage />
                        </ProtectedRoute>
                    }
                />

                {/* Admin routes */}
                <Route
                    path="/admin"
                    element={
                        <ProtectedRoute>
                            <AdminLayout />
                        </ProtectedRoute>
                    }
                >
                    <Route index element={<AdminDashboardPage />} />
                    <Route path="users" element={<AdminUsersPage />} />
                    <Route path="reports" element={<AdminModerationPage />} />
                    <Route path="ai-health" element={<AdminAiHealthPage />} />
                    <Route path="analytics" element={<AdminAnalyticsPage />} />
                    <Route path="rooms" element={<AdminRoomsPage />} />
                    <Route path="audit-logs" element={<AdminAuditLogsPage />} />
                </Route>

                {/* Catch-all → login */}
                <Route path="*" element={<Navigate to="/" replace />} />

            </Routes>
        </BrowserRouter>
    );
}