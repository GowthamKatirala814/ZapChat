import { QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { Toaster } from "react-hot-toast";
import { BrowserRouter } from "react-router-dom";
import { queryClient } from "../queryClient";
import { AuthProvider } from "./AuthProvider";
import { ThemeProvider } from "./ThemeProvider";

/**
 * Provider composition, in dependency order.
 *
 * QueryClientProvider must wrap AuthProvider: signing out clears the entire query cache,
 * so the auth layer needs the client.
 */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <BrowserRouter>
          <AuthProvider>
            {children}
            <Toaster
              position="bottom-center"
              toastOptions={{
                duration: 4_000,
                // Toasts read their colours from the design tokens, so they follow the
                // theme instead of being permanently light.
                style: {
                  background: "var(--zc-surface-3)",
                  color: "var(--zc-text)",
                  border: "1px solid var(--zc-line)",
                  fontSize: "13.5px",
                  borderRadius: "var(--zc-radius)",
                  boxShadow: "var(--zc-shadow-md)",
                  maxWidth: "min(420px, calc(100vw - 32px))",
                },
                success: { iconTheme: { primary: "var(--zc-success)", secondary: "#fff" } },
                error: { iconTheme: { primary: "var(--zc-danger)", secondary: "#fff" }, duration: 6_000 },
              }}
            />
          </AuthProvider>
        </BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
