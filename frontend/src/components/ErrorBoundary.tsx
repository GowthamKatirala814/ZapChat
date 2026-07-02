import { Component, type ErrorInfo, type ReactNode } from "react";

interface Props {
    children: ReactNode;
    fallback?: ReactNode;
}

interface State {
    hasError: boolean;
    error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
    constructor(props: Props) {
        super(props);
        this.state = { hasError: false, error: null };
    }

    static getDerivedStateFromError(error: Error): State {
        return { hasError: true, error };
    }

    componentDidCatch(error: Error, info: ErrorInfo) {
        console.error("[ErrorBoundary] Uncaught error:", error, info.componentStack);
    }

    handleReset = () => {
        this.setState({ hasError: false, error: null });
    };

    render() {
        if (this.state.hasError) {
            if (this.props.fallback) return this.props.fallback;

            return (
                <div
                    style={{
                        minHeight: "100vh",
                        display: "flex",
                        flexDirection: "column",
                        alignItems: "center",
                        justifyContent: "center",
                        background: "#0f0f1a",
                        color: "#e2e8f0",
                        fontFamily: "Inter, system-ui, sans-serif",
                        padding: "2rem",
                        textAlign: "center",
                    }}
                >
                    <div
                        style={{
                            background: "rgba(239,68,68,0.1)",
                            border: "1px solid rgba(239,68,68,0.3)",
                            borderRadius: "12px",
                            padding: "2rem 3rem",
                            maxWidth: "480px",
                        }}
                    >
                        <div style={{ fontSize: "3rem", marginBottom: "1rem" }}>⚠️</div>
                        <h1
                            style={{
                                fontSize: "1.5rem",
                                fontWeight: 700,
                                color: "#f87171",
                                marginBottom: "0.5rem",
                            }}
                        >
                            Something went wrong
                        </h1>
                        <p style={{ color: "#94a3b8", marginBottom: "1.5rem", lineHeight: 1.6 }}>
                            An unexpected error occurred. Please try refreshing the page. If the
                            problem persists, contact support.
                        </p>
                        {import.meta.env.DEV && this.state.error && (
                            <pre
                                style={{
                                    background: "rgba(0,0,0,0.4)",
                                    borderRadius: "6px",
                                    padding: "0.75rem",
                                    fontSize: "0.75rem",
                                    color: "#f87171",
                                    textAlign: "left",
                                    overflowX: "auto",
                                    marginBottom: "1.5rem",
                                    whiteSpace: "pre-wrap",
                                }}
                            >
                                {this.state.error.message}
                            </pre>
                        )}
                        <button
                            onClick={this.handleReset}
                            style={{
                                background: "linear-gradient(135deg, #6366f1, #8b5cf6)",
                                border: "none",
                                borderRadius: "8px",
                                color: "#fff",
                                cursor: "pointer",
                                fontSize: "0.875rem",
                                fontWeight: 600,
                                padding: "0.625rem 1.5rem",
                            }}
                        >
                            Try again
                        </button>
                        <button
                            onClick={() => (window.location.href = "/login")}
                            style={{
                                background: "transparent",
                                border: "1px solid rgba(148,163,184,0.3)",
                                borderRadius: "8px",
                                color: "#94a3b8",
                                cursor: "pointer",
                                fontSize: "0.875rem",
                                fontWeight: 600,
                                marginLeft: "0.75rem",
                                padding: "0.625rem 1.5rem",
                            }}
                        >
                            Go to Login
                        </button>
                    </div>
                </div>
            );
        }

        return this.props.children;
    }
}
