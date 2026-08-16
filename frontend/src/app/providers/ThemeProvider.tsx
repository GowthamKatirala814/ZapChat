import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { useMediaQuery } from "../../lib/hooks";
import { ThemeContext, type ThemePreference } from "./themeContext";

/**
 * Applies the theme.
 *
 * The stylesheet does the real work: `:root` carries the light palette,
 * `@media (prefers-color-scheme: dark)` carries dark for the un-stamped case, and
 * `[data-theme]` overrides both. This provider only decides which of the three states is
 * in force and stamps the attribute accordingly.
 */

const STORAGE_KEY = "zapchat.theme";

function readStored(): ThemePreference {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === "light" || stored === "dark" || stored === "system") return stored;
  } catch {
    // Private browsing can throw on access; the default is a fine answer.
  }

  return "system";
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [preference, setPreferenceState] = useState<ThemePreference>(readStored);

  // Keeps following the OS for as long as the preference is "system".
  const prefersDark = useMediaQuery("(prefers-color-scheme: dark)");

  const resolved = preference === "system" ? (prefersDark ? "dark" : "light") : preference;

  useEffect(() => {
    const root = document.documentElement;

    if (preference === "system") {
      // Removing the attribute hands control back to the media query, rather than
      // stamping a value that would then stop tracking the OS.
      root.removeAttribute("data-theme");
    } else {
      root.setAttribute("data-theme", preference);
    }

    // Native controls (scrollbars, date pickers, form widgets) follow this.
    root.style.colorScheme = resolved;
  }, [preference, resolved]);

  const setPreference = useCallback((next: ThemePreference) => {
    setPreferenceState(next);

    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // The preference simply will not persist; the session still works.
    }
  }, []);

  const value = useMemo(
    () => ({ preference, resolved, setPreference }),
    [preference, resolved, setPreference],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}
