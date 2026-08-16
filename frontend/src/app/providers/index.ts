/**
 * Barrel for the provider layer.
 *
 * The component and the hooks live in separate modules — a file that exports both a
 * component and a plain function defeats Fast Refresh — and this re-exports them under
 * one import path.
 */
export { AppProviders } from "./AppProviders";
export { useAuth, useCurrentUser, type AuthState } from "./authContext";
export { useTheme, type ThemePreference, type ThemeState } from "./themeContext";
