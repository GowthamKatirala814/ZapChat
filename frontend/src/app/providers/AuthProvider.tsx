import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { authApi } from "../../services/api";
import { ApiError, setSessionLostHandler } from "../../services/api/client";
import { stopAllHubs } from "../../services/realtime/connection";
import type { MyProfile } from "../../types/api";
import { AuthContext, type AuthState } from "./authContext";

/**
 * Session state.
 *
 * There is no token in here, and none in localStorage. Access and refresh tokens are
 * HttpOnly cookies the browser attaches automatically; the only thing the client holds is
 * the profile the server returns for the current session. That is also why "am I signed
 * in?" is answered by calling `/api/auth/me` rather than by reading a stored flag — the
 * client cannot inspect the cookie, and a stale flag was how the old app rendered a
 * signed-in shell around a dead session.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<MyProfile | null | undefined>(undefined);
  const [probeError, setProbeError] = useState<unknown>(null);
  const [probeNonce, setProbeNonce] = useState(0);
  const queryClient = useQueryClient();

  /** Wipes every trace of the previous session from memory. */
  const clearSession = useCallback(() => {
    setUser(null);
    void stopAllHubs();
    queryClient.clear();
  }, [queryClient]);

  // A 401 that survives a refresh attempt means the session is genuinely over. The
  // handler is registered once and reads the current callback through a ref, so the
  // interceptor never holds a stale closure.
  const clearRef = useRef(clearSession);

  useEffect(() => {
    clearRef.current = clearSession;
  }, [clearSession]);

  useEffect(() => {
    setSessionLostHandler(() => clearRef.current());
    return () => setSessionLostHandler(null);
  }, []);

  // Initial probe. A 401 is a normal answer here ("not signed in"), not an error state —
  // anything else means the request itself failed and the user should see a retry rather
  // than being silently bounced to the login screen.
  useEffect(() => {
    let cancelled = false;

    authApi
      .me()
      .then((profile) => {
        if (cancelled) return;

        setUser(profile);
        setProbeError(null);
      })
      .catch((error: unknown) => {
        if (cancelled) return;

        const api = ApiError.from(error);

        setUser(null);
        setProbeError(api && !api.isUnauthorized ? error : null);
      });

    return () => {
      cancelled = true;
    };
  }, [probeNonce]);

  const signIn = useCallback(async (email: string, password: string) => {
    await authApi.login(email, password);

    // Login returns identity, but the full profile (department, branch, roles) comes
    // from /me — one source of truth for the session rather than two shapes.
    const profile = await authApi.me();
    setUser(profile);
    return profile;
  }, []);

  const signOut = useCallback(async () => {
    // Server-side revocation first; the local session is cleared either way, so a network
    // failure cannot strand the user in a shell they can no longer use.
    await authApi.logout().catch(() => undefined);
    clearSession();
  }, [clearSession]);

  const refreshProfile = useCallback(async () => {
    setUser(await authApi.me());
  }, []);

  const retryProbe = useCallback(() => {
    setUser(undefined);
    setProbeError(null);
    setProbeNonce((nonce) => nonce + 1);
  }, []);

  const value = useMemo<AuthState>(
    () => ({
      user,
      isLoading: user === undefined,
      isAuthenticated: Boolean(user),
      // Roles are compared case-insensitively: the token carries "Admin", the profile
      // carries whatever was stored, and a casing mismatch must not silently un-admin.
      isAdmin: Boolean(user?.roles?.some((role) => role.toLowerCase() === "admin")),
      probeError,
      signIn,
      signOut,
      refreshProfile,
      retryProbe,
    }),
    [user, probeError, signIn, signOut, refreshProfile, retryProbe],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
