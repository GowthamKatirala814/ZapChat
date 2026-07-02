import { useEffect, useState } from "react";
import { getGeminiStats, type GeminiStats } from "../../api/adminApi";
import {
    Activity,
    ShieldAlert,
    ShieldCheck,
    AlertTriangle,
    Clock,
    RefreshCw,
    XCircle,
    CheckCircle2
} from "lucide-react";

export default function AdminAiHealthPage() {
    const [stats, setStats] = useState<GeminiStats | null>(null);
    const [loading, setLoading] = useState(true);

    const loadStats = async () => {
        try {
            setLoading(true);
            const data = await getGeminiStats();
            setStats(data);
        } catch (err) {
            console.error("Failed to load AI health stats", err);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadStats();
        const interval = setInterval(loadStats, 10000); // Auto refresh every 10s
        return () => clearInterval(interval);
    }, []);

    if (loading && !stats) {
        return (
            <div className="p-8 flex items-center justify-center">
                <div className="w-8 h-8 border-4 border-sky-500 border-t-transparent rounded-full animate-spin"></div>
            </div>
        );
    }

    if (!stats) return <div className="p-8 text-slate-400">Failed to load AI Health data.</div>;

    const getStatusColor = (status: string) => {
        switch (status) {
            case "Healthy": return "text-emerald-400 bg-emerald-400/10 border-emerald-500/20";
            case "Rate Limited": return "text-orange-400 bg-orange-400/10 border-orange-500/20";
            case "Offline": return "text-red-400 bg-red-400/10 border-red-500/20";
            case "Degraded": return "text-yellow-400 bg-yellow-400/10 border-yellow-500/20";
            default: return "text-slate-400 bg-slate-400/10 border-slate-500/20";
        }
    };

    const getStatusIcon = (status: string) => {
        switch (status) {
            case "Healthy": return <CheckCircle2 size={24} className="text-emerald-400" />;
            case "Rate Limited": return <AlertTriangle size={24} className="text-orange-400" />;
            case "Offline": return <XCircle size={24} className="text-red-400" />;
            case "Degraded": return <Activity size={24} className="text-yellow-400" />;
            default: return <Activity size={24} className="text-slate-400" />;
        }
    };

    return (
        <div className="p-8 max-w-7xl mx-auto space-y-8">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-white flex items-center gap-3">
                        <Activity className="text-sky-400" />
                        AI Moderation & Health
                    </h1>
                    <p className="text-slate-400 mt-1">Real-time monitoring of Gemini AI integration</p>
                </div>
                <button
                    onClick={loadStats}
                    className="flex items-center gap-2 px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg transition-colors border border-slate-700"
                >
                    <RefreshCw size={16} className={loading ? "animate-spin" : ""} />
                    Refresh
                </button>
            </div>

            {/* Current Status Banner */}
            <div className={`p-6 rounded-2xl border flex items-center justify-between ${getStatusColor(stats.currentStatus || "Unknown")}`}>
                <div className="flex items-center gap-4">
                    {getStatusIcon(stats.currentStatus || "Unknown")}
                    <div>
                        <h2 className="text-lg font-semibold">Current Status: {stats.currentStatus || "Unknown"}</h2>
                        <p className="text-sm opacity-80 mt-1">
                            {stats.currentStatus === "Healthy" 
                                ? "AI Moderation is fully operational"
                                : stats.lastErrorMessage || "Service is currently experiencing issues"}
                        </p>
                    </div>
                </div>
                <div className="text-right">
                    <div className="text-3xl font-bold">{stats.uptimePercentage ?? 100}%</div>
                    <div className="text-sm opacity-80">Today's Uptime</div>
                </div>
            </div>

            {/* Metrics Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <MetricCard 
                    title="Total Requests" 
                    value={stats.requestsToday} 
                    icon={<Activity className="text-blue-400" />} 
                />
                <MetricCard 
                    title="Successful Requests" 
                    value={stats.successfulRequests} 
                    icon={<CheckCircle2 className="text-emerald-400" />} 
                />
                <MetricCard 
                    title="Safe Messages" 
                    value={stats.safeMessages} 
                    icon={<ShieldCheck className="text-emerald-400" />} 
                />
                <MetricCard 
                    title="Blocked Messages" 
                    value={stats.blockedMessages} 
                    icon={<ShieldAlert className="text-orange-400" />} 
                />
                <MetricCard 
                    title="Failed Requests" 
                    value={stats.failedRequests} 
                    icon={<XCircle className="text-red-400" />} 
                />
                <MetricCard 
                    title="Rate Limited (429)" 
                    value={stats.error429s} 
                    icon={<AlertTriangle className="text-orange-400" />} 
                />
                <MetricCard 
                    title="Timeouts" 
                    value={stats.timeoutErrors} 
                    icon={<Clock className="text-yellow-400" />} 
                />
                <MetricCard 
                    title="Quota Usage" 
                    value={`${stats.usagePercentage.toFixed(1)}%`} 
                    subtitle={`${stats.requestsToday} / ${stats.estimatedQuota}`}
                    icon={<Activity className="text-sky-400" />} 
                />
                <MetricCard 
                    title="Config Errors" 
                    value={stats.configurationErrors} 
                    icon={<AlertTriangle className="text-red-400" />} 
                />
                <MetricCard 
                    title="Auth Errors" 
                    value={stats.authenticationErrors} 
                    icon={<ShieldAlert className="text-red-400" />} 
                />
                <MetricCard 
                    title="Server Errors" 
                    value={stats.serverErrors} 
                    icon={<XCircle className="text-red-400" />} 
                />
                <MetricCard 
                    title="Parse Errors" 
                    value={stats.invalidResponses} 
                    icon={<AlertTriangle className="text-yellow-400" />} 
                />
            </div>

            {/* Timeline */}
            <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
                <h3 className="text-lg font-semibold text-white mb-6 flex items-center gap-2">
                    <Clock size={20} className="text-slate-400" />
                    Today's Event Timeline
                </h3>
                
                {!stats.events || stats.events.length === 0 ? (
                    <div className="text-center py-8 text-slate-500">
                        No health events recorded today. The system is stable.
                    </div>
                ) : (
                    <div className="relative border-l border-slate-700 ml-4 space-y-8 pb-4">
                        {stats.events.map((evt, idx) => (
                            <div key={evt.id || idx} className="relative pl-6">
                                {/* Timeline Node */}
                                <div className={`absolute -left-2 top-1 w-4 h-4 rounded-full border-2 border-slate-900 ${
                                    evt.newStatus === "Healthy" ? "bg-emerald-500" :
                                    evt.newStatus === "Rate Limited" ? "bg-orange-500" :
                                    "bg-red-500"
                                }`}></div>
                                
                                <div className="flex flex-col sm:flex-row sm:items-baseline gap-2">
                                    <span className="text-sm font-medium text-white">{evt.newStatus}</span>
                                    <span className="text-xs text-slate-500">
                                        {new Date(evt.timestamp).toLocaleTimeString()}
                                    </span>
                                </div>
                                <p className="text-sm text-slate-400 mt-1">{evt.message}</p>
                                <p className="text-xs text-slate-500 mt-1">
                                    State changed from <span className="font-medium text-slate-400">{evt.previousStatus}</span>
                                </p>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}

function MetricCard({ title, value, icon, subtitle }: { title: string, value: string | number, icon: React.ReactNode, subtitle?: string }) {
    return (
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5 hover:bg-slate-800/50 transition-colors">
            <div className="flex items-start justify-between">
                <div>
                    <p className="text-sm font-medium text-slate-400">{title}</p>
                    <h3 className="text-2xl font-bold text-white mt-1">{value}</h3>
                    {subtitle && <p className="text-xs text-slate-500 mt-1">{subtitle}</p>}
                </div>
                <div className="p-2 bg-slate-800 rounded-lg">
                    {icon}
                </div>
            </div>
        </div>
    );
}
