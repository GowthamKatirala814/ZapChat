import { lazy, Suspense } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { LoadingState } from "../components/feedback";
import { AppShell } from "../components/layout/AppShell";
import { paths } from "../config";
import { RedirectIfAuthenticated, RequireAdmin, RequireAuth } from "./guards";

/**
 * Routing.
 *
 * The admin console is code-split: it pulls in the charting library, which is the single
 * largest dependency in the bundle and is irrelevant to the majority of users who will
 * never open it.
 */

import { LoginPage } from "../features/auth/LoginPage";
import { RegisterPage } from "../features/auth/RegisterPage";
import { ForgotPasswordPage } from "../features/auth/ForgotPasswordPage";
import { ChatPage } from "../features/chat/ChatPage";
import { PrivateChatPage } from "../features/private-chat/PrivateChatPage";
import { PollsPage } from "../features/polls/PollsPage";
import { NotificationsPage } from "../features/notifications/NotificationsPage";
import { ProfilePage } from "../features/profile/ProfilePage";
import { NotFoundPage } from "../features/misc/NotFoundPage";

const AdminRoutes = lazy(() => import("../features/admin/AdminRoutes"));

export function AppRouter() {
  return (
    <Routes>
      {/* Public */}
      <Route
        path={paths.login}
        element={
          <RedirectIfAuthenticated>
            <LoginPage />
          </RedirectIfAuthenticated>
        }
      />
      <Route
        path={paths.register}
        element={
          <RedirectIfAuthenticated>
            <RegisterPage />
          </RedirectIfAuthenticated>
        }
      />
      <Route
        path={paths.forgotPassword}
        element={
          <RedirectIfAuthenticated>
            <ForgotPasswordPage />
          </RedirectIfAuthenticated>
        }
      />

      {/* Authenticated */}
      <Route
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route path={paths.chat} element={<ChatPage />} />
        <Route path="/chat/:roomId" element={<ChatPage />} />

        <Route path={paths.messages} element={<PrivateChatPage />} />
        <Route path="/messages/:conversationId" element={<PrivateChatPage />} />

        <Route path={paths.polls} element={<PollsPage />} />
        <Route path={paths.notifications} element={<NotificationsPage />} />
        <Route path={paths.profile} element={<ProfilePage />} />

        <Route
          path="/admin/*"
          element={
            <RequireAdmin>
              <Suspense fallback={<LoadingState label="Loading the admin console…" />}>
                <AdminRoutes />
              </Suspense>
            </RequireAdmin>
          }
        />

        <Route path="/" element={<Navigate to={paths.chat} replace />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
