import React from "react";
import ReactDOM from "react-dom/client";

import { Provider } from "react-redux";

import { store } from "./store/store";

import AppRoutes from "./routes/AppRoutes";

import { ErrorBoundary } from "./components/ErrorBoundary";
import { ThemeProvider } from "./context/ThemeContext";

import "./index.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
        <ErrorBoundary>
            <Provider store={store}>
                <ThemeProvider>
                    <AppRoutes />
                </ThemeProvider>
            </Provider>
        </ErrorBoundary>
    </React.StrictMode>
);