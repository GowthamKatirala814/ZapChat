import { createContext, useContext } from "react";
import type { MyProfile } from "../../types/api";

/**
 * The session context and its hooks.
 *
 * Kept apart from `AuthProvider.tsx` so that file exports only a component — which is
 * what lets Fast Refresh preserve state while the provider is edited.
 */

export interface AuthState {
  /** `undefined` while the initial session probe is running. */
  user: MyProfile | null | undefined;
  isLoading: boolean;
  isAuthenticated: boolean;
  isAdmin: boolean;
  /** Non-fatal failure of the session probe — the server was unreachable, not a 401. */
  probeError: unknown;
  signIn: (email: string, password: string) => Promise<MyProfile>;
  signOut: () => Promise<void>;
  refreshProfile: () => Promise<void>;
  retryProbe: () => void;
}

export const AuthContext = createContext<AuthState | null>(null);

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}

/**
 * The signed-in profile, for components that only render behind a route guard.
 * Throws rather than returning a fake user, so a guard mistake fails loudly.
 */
export function useCurrentUser(): MyProfile {
  const { user } = useAuth();
  if (!user) throw new Error("useCurrentUser used outside an authenticated route");
  return user;
}
