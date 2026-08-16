import { Route, Routes } from "react-router-dom";
import { NotFoundPage } from "../misc/NotFoundPage";
import { AdminLayout } from "./AdminLayout";
import { AnalyticsPage } from "./AnalyticsPage";
import { AuditPage } from "./AuditPage";
import { DashboardPage } from "./DashboardPage";
import { ModerationPage } from "./ModerationPage";
import { RoomsPage } from "./RoomsPage";
import { UsersPage } from "./UsersPage";

/**
 * The admin console, code-split as one chunk.
 *
 * It is the only part of the app that pulls in the charting library, and most users will
 * never open it — so it is loaded on demand rather than shipped to everyone.
 */
export default function AdminRoutes() {
  return (
    <Routes>
      <Route element={<AdminLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="moderation" element={<ModerationPage />} />
        <Route path="analytics" element={<AnalyticsPage />} />
        <Route path="rooms" element={<RoomsPage />} />
        <Route path="users" element={<UsersPage />} />
        <Route path="audit" element={<AuditPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
