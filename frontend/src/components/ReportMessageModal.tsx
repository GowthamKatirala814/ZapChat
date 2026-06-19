import { useState } from "react";
import { X, Flag, Loader2, CheckCircle, AlertCircle } from "lucide-react";
import { submitReport } from "../api/reportApi";
import type { MessageType } from "../api/reportApi";

const REASONS = [
    "Spam",
    "Harassment",
    "Offensive Content",
    "Threats",
    "Other",
] as const;

type ToastState = { type: "success" | "error"; message: string } | null;

interface Props {
    messageId: string;
    messageType: MessageType;
    onClose: () => void;
}

export default function ReportMessageModal({ messageId, messageType, onClose }: Props) {
    const [reason, setReason] = useState("");
    const [description, setDescription] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [toast, setToast] = useState<ToastState>(null);

    const userId = localStorage.getItem("userId") ?? "";

    const handleSubmit = async () => {
        if (!reason) return;
        setSubmitting(true);
        setToast(null);
        try {
            await submitReport({
                messageId,
                messageType,
                reportedByUserId: userId,
                reason: description.trim() ? `${reason}: ${description.trim()}` : reason,
            });
            setToast({ type: "success", message: "Report submitted. Thank you." });
            setTimeout(() => {
                setToast(null);
                onClose();
            }, 1800);
        } catch (error: any) {
            if (error.response?.status === 409) {
                setToast({ type: "error", message: "You have already reported this message. A message can only be reported once by the same user." });
            } else {
                setToast({ type: "error", message: "Failed to submit report. Please try again." });
            }
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div
            className="fixed inset-0 z-50 flex items-center justify-center px-4"
            style={{ background: "rgba(0,0,0,0.65)", backdropFilter: "blur(4px)" }}
            onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
        >
            <div
                className="w-full max-w-md rounded-2xl p-6 space-y-5"
                style={{
                    background: "#0f172a",
                    border: "1px solid rgba(255,255,255,0.08)",
                    boxShadow: "0 24px 60px rgba(0,0,0,0.6)",
                }}
            >
                {/* Header */}
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2.5">
                        <div
                            className="w-8 h-8 rounded-lg flex items-center justify-center shrink-0"
                            style={{ background: "rgba(239,68,68,0.15)" }}
                        >
                            <Flag size={15} style={{ color: "#f87171" }} />
                        </div>
                        <h2 className="text-sm font-bold text-white">Report Message</h2>
                    </div>
                    <button
                        onClick={onClose}
                        className="p-1 rounded-lg transition-colors"
                        style={{ color: "#475569" }}
                        onMouseEnter={(e) => ((e.currentTarget as HTMLElement).style.color = "#94a3b8")}
                        onMouseLeave={(e) => ((e.currentTarget as HTMLElement).style.color = "#475569")}
                    >
                        <X size={16} />
                    </button>
                </div>

                {/* Reason dropdown */}
                <div className="space-y-1.5">
                    <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider">
                        Reason <span style={{ color: "#f87171" }}>*</span>
                    </label>
                    <select
                        value={reason}
                        onChange={(e) => setReason(e.target.value)}
                        disabled={submitting}
                        className="w-full rounded-xl px-4 py-2.5 text-sm outline-none transition-all appearance-none cursor-pointer disabled:opacity-50"
                        style={{
                            background: "rgba(255,255,255,0.05)",
                            border: reason ? "1px solid rgba(239,68,68,0.35)" : "1px solid rgba(255,255,255,0.1)",
                            color: reason ? "#f1f5f9" : "#475569",
                            backgroundImage: "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='8' viewBox='0 0 12 8'%3E%3Cpath d='M1 1l5 5 5-5' stroke='%2364748b' stroke-width='1.5' fill='none' stroke-linecap='round'/%3E%3C/svg%3E\")",
                            backgroundRepeat: "no-repeat",
                            backgroundPosition: "right 14px center",
                            paddingRight: "40px",
                        }}
                        onFocus={(e) => (e.currentTarget.style.border = "1px solid rgba(239,68,68,0.6)")}
                        onBlur={(e) => (e.currentTarget.style.border = reason ? "1px solid rgba(239,68,68,0.35)" : "1px solid rgba(255,255,255,0.1)")}
                    >
                        <option value="" style={{ background: "#0f172a" }}>Select a reason…</option>
                        {REASONS.map((r) => (
                            <option key={r} value={r} style={{ background: "#0f172a" }}>{r}</option>
                        ))}
                    </select>
                </div>

                {/* Optional description */}
                <div className="space-y-1.5">
                    <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider">
                        Additional details <span style={{ color: "#475569" }}>(optional)</span>
                    </label>
                    <textarea
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        disabled={submitting}
                        placeholder="Describe the issue…"
                        rows={3}
                        className="w-full rounded-xl px-4 py-2.5 text-sm outline-none resize-none transition-all disabled:opacity-50"
                        style={{
                            background: "rgba(255,255,255,0.05)",
                            border: "1px solid rgba(255,255,255,0.1)",
                            color: "#f1f5f9",
                        }}
                        onFocus={(e) => (e.currentTarget.style.border = "1px solid rgba(239,68,68,0.4)")}
                        onBlur={(e) => (e.currentTarget.style.border = "1px solid rgba(255,255,255,0.1)")}
                    />
                </div>

                {/* Toast */}
                {toast && (
                    <div
                        className="flex items-center gap-2.5 px-4 py-3 rounded-xl text-sm"
                        style={{
                            background: toast.type === "success"
                                ? "rgba(34,197,94,0.1)"
                                : "rgba(239,68,68,0.1)",
                            border: `1px solid ${toast.type === "success" ? "rgba(34,197,94,0.3)" : "rgba(239,68,68,0.3)"}`,
                            color: toast.type === "success" ? "#4ade80" : "#f87171",
                        }}
                    >
                        {toast.type === "success"
                            ? <CheckCircle size={15} />
                            : <AlertCircle size={15} />
                        }
                        {toast.message}
                    </div>
                )}

                {/* Actions */}
                <div className="flex gap-3 pt-1">
                    <button
                        onClick={onClose}
                        disabled={submitting}
                        className="flex-1 py-2.5 rounded-xl text-sm font-medium transition-colors disabled:opacity-50"
                        style={{
                            background: "rgba(255,255,255,0.04)",
                            border: "1px solid rgba(255,255,255,0.08)",
                            color: "#64748b",
                        }}
                        onMouseEnter={(e) => ((e.currentTarget as HTMLElement).style.color = "#94a3b8")}
                        onMouseLeave={(e) => ((e.currentTarget as HTMLElement).style.color = "#64748b")}
                    >
                        Cancel
                    </button>
                    <button
                        onClick={handleSubmit}
                        disabled={!reason || submitting}
                        className="flex-1 py-2.5 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 transition-all"
                        style={!reason || submitting ? {
                            background: "rgba(239,68,68,0.3)",
                            color: "rgba(248,113,113,0.5)",
                            cursor: "not-allowed",
                        } : {
                            background: "linear-gradient(135deg, #ef4444, #dc2626)",
                            color: "#fff",
                            boxShadow: "0 4px 15px rgba(239,68,68,0.35)",
                        }}
                    >
                        {submitting ? (
                            <>
                                <Loader2 size={14} className="animate-spin" />
                                Submitting…
                            </>
                        ) : (
                            <>
                                <Flag size={14} />
                                Submit Report
                            </>
                        )}
                    </button>
                </div>
            </div>
        </div>
    );
}
