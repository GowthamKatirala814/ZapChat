import { useEffect, useState } from "react";
import {
    Mail, Building2, MapPin, Shield,
    Calendar, Pencil, X, Check, Loader2, ChevronDown,
} from "lucide-react";
import TopNav from "../../components/TopNav";
import { getMe, updateMe } from "../../api/authApi";
import type { ProfileData } from "../../api/authApi";

// ── Same lists as RegisterPage ──────────────────────────────────────────────
const DEPARTMENTS = [
    "Engineering",
    "Product",
    "Design",
    "Marketing",
    "Sales",
    "HR",
    "Finance",
    "Operations",
    "Legal",
    "Other",
];

const BRANCHES = [
    "Headquarters",
    "North Branch",
    "South Branch",
    "East Branch",
    "West Branch",
    "Remote",
    "International",
];

// ── Styled select component ─────────────────────────────────────────────────
function StyledSelect({
    label,
    value,
    options,
    onChange,
    disabled,
}: {
    label: string;
    value: string;
    options: string[];
    onChange: (v: string) => void;
    disabled?: boolean;
}) {
    // If the current value isn't in the list (legacy data), add it temporarily
    const allOptions = options.includes(value) || value === ""
        ? options
        : [value, ...options];

    return (
        <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1.5">
                {label}
            </label>
            <div className="relative">
                <select
                    value={value}
                    onChange={e => onChange(e.target.value)}
                    disabled={disabled}
                    className="w-full appearance-none px-4 py-2.5 pr-10 rounded-xl text-sm outline-none transition-all cursor-pointer"
                    style={{
                        background: disabled ? "#F8FAFC" : "#FFFFFF",
                        border: "1px solid #E2E8F0",
                        color: value ? "#1E293B" : "#94A3B8",
                    }}
                    onFocus={e => {
                        e.target.style.border = "1px solid #38BDF8";
                        e.target.style.boxShadow = "0 0 0 3px rgba(14,165,233,0.1)";
                    }}
                    onBlur={e => {
                        e.target.style.border = "1px solid #E2E8F0";
                        e.target.style.boxShadow = "none";
                    }}
                >
                    <option value="" disabled>Select…</option>
                    {allOptions.map(opt => (
                        <option key={opt} value={opt}>{opt}</option>
                    ))}
                </select>
                <ChevronDown
                    size={14}
                    className="absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none"
                    style={{ color: "#94A3B8" }}
                />
            </div>
        </div>
    );
}

// ── Main component ──────────────────────────────────────────────────────────
export default function ProfilePage() {
    const [profile, setProfile]         = useState<ProfileData | null>(null);
    const [loading, setLoading]         = useState(true);
    const [editOpen, setEditOpen]       = useState(false);
    const [saving, setSaving]           = useState(false);
    const [saveError, setSaveError]     = useState<string | null>(null);
    const [saveSuccess, setSaveSuccess] = useState(false);

    // Edit form state — dropdowns only
    const [deptDraft, setDeptDraft]     = useState("");
    const [branchDraft, setBranchDraft] = useState("");

    const myName  = localStorage.getItem("anonymousName") ?? "Anonymous";
    const myEmail = localStorage.getItem("email") ?? "";
    const myRole  = localStorage.getItem("role") ?? "user";
    const initial = myName.charAt(0).toUpperCase();

    useEffect(() => {
        getMe()
            .then((data: ProfileData) => setProfile(data))
            .catch(() => setProfile(null))
            .finally(() => setLoading(false));
    }, []);

    const department = profile?.department?.trim() || "—";
    const branch     = profile?.branch?.trim()     || "—";
    const fullName   = profile?.fullName?.trim()   || "";
    const joinedDate = profile?.createdAt
        ? new Date(profile.createdAt).toLocaleDateString("en-US", {
              day: "numeric", month: "long", year: "numeric",
          })
        : "—";

    const openEdit = () => {
        setDeptDraft(profile?.department ?? "");
        setBranchDraft(profile?.branch ?? "");
        setSaveError(null);
        setSaveSuccess(false);
        setEditOpen(true);
    };

    const closeEdit = () => {
        if (saving) return;
        setEditOpen(false);
        setSaveError(null);
    };

    const handleSave = async () => {
        if (!deptDraft || !branchDraft) {
            setSaveError("Please select both Department and Branch.");
            return;
        }
        setSaving(true);
        setSaveError(null);
        try {
            const updated = await updateMe({ department: deptDraft, branch: branchDraft });
            setProfile(prev =>
                prev ? { ...prev, department: updated.department, branch: updated.branch } : prev
            );
            setSaveSuccess(true);
            setTimeout(() => { setEditOpen(false); setSaveSuccess(false); }, 900);
        } catch {
            setSaveError("Failed to save. Please try again.");
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="min-h-screen flex flex-col" style={{ background: "#F0F7FF" }}>
            <TopNav />

            <div className="flex-1 max-w-2xl w-full mx-auto px-4 py-10">

                {/* ── Profile Card ─────────────────────────────── */}
                <div
                    className="bg-white rounded-2xl overflow-hidden mb-5"
                    style={{ border: "1px solid #E2E8F0", boxShadow: "0 2px 16px rgba(14,165,233,0.08)" }}
                >
                    {/* Banner */}
                    <div
                        className="h-32"
                        style={{ background: "linear-gradient(135deg, #0EA5E9 0%, #38BDF8 60%, #7DD3FC 100%)" }}
                    >
                        <div
                            className="w-full h-full opacity-10"
                            style={{
                                backgroundImage: "radial-gradient(circle, white 1px, transparent 1px)",
                                backgroundSize: "22px 22px",
                            }}
                        />
                    </div>

                    {/* Avatar + buttons row — pulled up below banner */}
                    <div className="px-7 pb-7">
                        <div className="flex items-end justify-between" style={{ marginTop: "-36px" }}>
                            {/* Avatar */}
                            <div
                                className="w-[72px] h-[72px] rounded-2xl flex items-center justify-center text-2xl font-black text-white shrink-0"
                                style={{
                                    background: "linear-gradient(135deg, #0EA5E9, #0284C7)",
                                    border: "3px solid #FFFFFF",
                                    boxShadow: "0 4px 16px rgba(14,165,233,0.3)",
                                }}
                            >
                                {initial}
                            </div>

                            {/* Right badges */}
                            <div className="flex items-center gap-2 mb-1">
                                {myRole === "admin" && (
                                    <span
                                        className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-bold"
                                        style={{ background: "#EFF6FF", color: "#0284C7", border: "1px solid #BAE6FD" }}
                                    >
                                        <Shield size={11} />
                                        Administrator
                                    </span>
                                )}
                                <button
                                    onClick={openEdit}
                                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold transition-all"
                                    style={{ background: "#F0F9FF", color: "#0EA5E9", border: "1px solid #BAE6FD" }}
                                    onMouseEnter={e => ((e.currentTarget as HTMLElement).style.background = "#E0F2FE")}
                                    onMouseLeave={e => ((e.currentTarget as HTMLElement).style.background = "#F0F9FF")}
                                >
                                    <Pencil size={11} />
                                    Edit Profile
                                </button>
                            </div>
                        </div>

                        {/* Name / email / status */}
                        <div className="mt-4">
                            <h1 className="text-xl font-bold text-slate-900 leading-tight">{myName}</h1>
                            <p className="text-sm text-slate-500 mt-0.5">{myEmail}</p>
                            {fullName && (
                                <p className="text-xs text-slate-400 mt-0.5">
                                    Known as <span className="font-medium text-slate-600">{fullName}</span>
                                </p>
                            )}
                            <div className="flex items-center gap-1.5 mt-3">
                                <span className="w-2 h-2 rounded-full" style={{ background: "#22C55E" }} />
                                <span className="text-xs text-slate-500">Active · Anonymous Mode</span>
                            </div>
                        </div>
                    </div>
                </div>

                {/* ── Info Cards ────────────────────────────────── */}
                {loading ? (
                    <div className="flex items-center justify-center py-12 gap-3 text-slate-400">
                        <Loader2 size={18} className="animate-spin" />
                        <span className="text-sm">Loading profile…</span>
                    </div>
                ) : (
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        {([
                            { icon: Mail,      label: "Email Address", value: myEmail || "—", editable: false },
                            { icon: Building2, label: "Department",    value: department,      editable: true  },
                            { icon: MapPin,    label: "Branch",        value: branch,          editable: true  },
                            { icon: Calendar,  label: "Member Since",  value: joinedDate,      editable: false },
                        ] as const).map(({ icon: Icon, label, value, editable }) => (
                            <div
                                key={label}
                                className="bg-white rounded-xl px-5 py-4 flex items-center gap-3"
                                style={{ border: "1px solid #E2E8F0", boxShadow: "0 1px 4px rgba(0,0,0,0.04)" }}
                            >
                                <div
                                    className="w-9 h-9 rounded-xl flex items-center justify-center shrink-0"
                                    style={{ background: "#EFF6FF" }}
                                >
                                    <Icon size={16} style={{ color: "#0EA5E9" }} />
                                </div>
                                <div className="min-w-0 flex-1">
                                    <div className="text-xs text-slate-400 mb-0.5">{label}</div>
                                    <div
                                        className="text-sm font-semibold truncate"
                                        style={{ color: value === "—" ? "#CBD5E1" : "#1E293B" }}
                                    >
                                        {value}
                                    </div>
                                </div>
                                {editable && (
                                    <button
                                        onClick={openEdit}
                                        title={`Edit ${label}`}
                                        className="shrink-0 p-1.5 rounded-lg transition-colors"
                                        style={{ color: "#CBD5E1" }}
                                        onMouseEnter={e => ((e.currentTarget as HTMLElement).style.color = "#0EA5E9")}
                                        onMouseLeave={e => ((e.currentTarget as HTMLElement).style.color = "#CBD5E1")}
                                    >
                                        <Pencil size={13} />
                                    </button>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* ── Edit Profile Modal ──────────────────────────── */}
            {editOpen && (
                <div
                    className="fixed inset-0 z-50 flex items-center justify-center px-4"
                    style={{ background: "rgba(15,23,42,0.45)", backdropFilter: "blur(4px)" }}
                    onClick={e => { if (e.target === e.currentTarget) closeEdit(); }}
                >
                    <div
                        className="w-full max-w-sm rounded-2xl overflow-hidden"
                        style={{
                            background: "#FFFFFF",
                            border: "1px solid #E2E8F0",
                            boxShadow: "0 20px 60px rgba(0,0,0,0.15)",
                        }}
                    >
                        {/* Header */}
                        <div
                            className="flex items-center justify-between px-6 py-4"
                            style={{ borderBottom: "1px solid #F1F5F9" }}
                        >
                            <div>
                                <h2 className="text-base font-bold text-slate-900">Edit Profile</h2>
                                <p className="text-xs text-slate-400 mt-0.5">
                                    Update your department and branch
                                </p>
                            </div>
                            <button
                                onClick={closeEdit}
                                disabled={saving}
                                className="p-1.5 rounded-lg transition-colors"
                                style={{ color: "#94A3B8" }}
                                onMouseEnter={e => ((e.currentTarget as HTMLElement).style.color = "#334155")}
                                onMouseLeave={e => ((e.currentTarget as HTMLElement).style.color = "#94A3B8")}
                            >
                                <X size={16} />
                            </button>
                        </div>

                        {/* Body */}
                        <div className="px-6 py-5 space-y-4">
                            {/* Read-only notice */}
                            <div
                                className="px-4 py-3 rounded-xl text-xs text-slate-400"
                                style={{ background: "#F8FAFC", border: "1px solid #E2E8F0" }}
                            >
                                🔒 Email, anonymous name, and registration date cannot be edited.
                            </div>

                            {/* Department dropdown */}
                            <StyledSelect
                                label="Department"
                                value={deptDraft}
                                options={DEPARTMENTS}
                                onChange={setDeptDraft}
                                disabled={saving}
                            />

                            {/* Branch dropdown */}
                            <StyledSelect
                                label="Branch"
                                value={branchDraft}
                                options={BRANCHES}
                                onChange={setBranchDraft}
                                disabled={saving}
                            />

                            {/* Error */}
                            {saveError && (
                                <div
                                    className="px-4 py-2.5 rounded-xl text-xs text-red-600"
                                    style={{ background: "#FEF2F2", border: "1px solid #FECACA" }}
                                >
                                    {saveError}
                                </div>
                            )}
                        </div>

                        {/* Footer */}
                        <div
                            className="flex items-center justify-end gap-2 px-6 py-4"
                            style={{ borderTop: "1px solid #F1F5F9" }}
                        >
                            <button
                                onClick={closeEdit}
                                disabled={saving}
                                className="px-4 py-2 rounded-xl text-sm font-medium transition-colors"
                                style={{ color: "#64748B", background: "#F1F5F9" }}
                                onMouseEnter={e => ((e.currentTarget as HTMLElement).style.background = "#E2E8F0")}
                                onMouseLeave={e => ((e.currentTarget as HTMLElement).style.background = "#F1F5F9")}
                            >
                                Cancel
                            </button>
                            <button
                                onClick={handleSave}
                                disabled={saving || saveSuccess}
                                className="flex items-center gap-2 px-5 py-2 rounded-xl text-sm font-semibold text-white transition-all"
                                style={{
                                    background: saveSuccess
                                        ? "#22C55E"
                                        : saving
                                        ? "rgba(14,165,233,0.6)"
                                        : "linear-gradient(135deg, #0EA5E9, #0284C7)",
                                    boxShadow: (saving || saveSuccess) ? "none" : "0 4px 14px rgba(14,165,233,0.3)",
                                    cursor: (saving || saveSuccess) ? "not-allowed" : "pointer",
                                }}
                            >
                                {saveSuccess ? (
                                    <><Check size={14} /> Saved!</>
                                ) : saving ? (
                                    <><Loader2 size={14} className="animate-spin" /> Saving…</>
                                ) : (
                                    "Save Changes"
                                )}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
