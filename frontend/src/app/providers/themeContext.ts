import { createContext, useContext } from "react";

/**
 * Theme selection.
 *
 * Three states, not two: "system" means *follow the OS*, and it must keep following it
 * when the OS changes at sunset — so it is stored as its own value rather than being
 * resolved once into light or dark at startup.
 */
export type ThemePreference = "light" | "dark" | "system";

export interface ThemeState {
  preference: ThemePreference;
  /** What is actually on screen once "system" is resolved. */
  resolved: "light" | "dark";
  setPreference: (preference: ThemePreference) => void;
}

export const ThemeContext = createContext<ThemeState | null>(null);

export function useTheme(): ThemeState {
  const context = useContext(ThemeContext);
  if (!context) throw new Error("useTheme must be used inside ThemeProvider");
  return context;
}
