import { Zap } from "lucide-react";

interface Props {
    className?: string;
}

export default function AppFooter({ className = "" }: Props) {
    return (
        <footer className={`
            h-10 shrink-0 border-t border-slate-800
            bg-slate-900 flex items-center justify-between
            px-5 ${className}`}>
            <div className="flex items-center gap-1.5 text-xs text-slate-600">
                <Zap size={12} className="text-sky-500" />
                <span>ZapChat</span>
                <span className="mx-1">·</span>
                <span>Anonymous Enterprise Messaging</span>
            </div>
            <div className="flex items-center gap-3 text-xs text-slate-600">
                <span className="flex items-center gap-1">
                    <span className="w-1.5 h-1.5 rounded-full bg-green-500 inline-block" />
                    All systems operational
                </span>
                <span>v1.0.0</span>
            </div>
        </footer>
    );
}
