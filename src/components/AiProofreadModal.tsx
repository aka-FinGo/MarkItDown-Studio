import React from "react";
import { Sparkles, Check, X, Wand2 } from "lucide-react";
import { ThemeType } from "../types";

interface AiProofreadModalProps {
  isOpen: boolean;
  onClose: () => void;
  originalText: string;
  correctedText: string;
  onApply: (newText: string) => void;
  theme: ThemeType;
}

export const AiProofreadModal: React.FC<AiProofreadModalProps> = ({
  isOpen,
  onClose,
  correctedText,
  onApply,
  theme,
}) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-zinc-950/80 backdrop-blur-xs animate-in fade-in duration-200">
      <div
        className={`rounded-2xl max-w-4xl w-full max-h-[85vh] flex flex-col p-6 shadow-2xl border transition-all ${
          theme === "ObsidianDark"
            ? "bg-[#1f1f1f] text-zinc-100 border-purple-500/50"
            : theme === "CyberpunkNeon"
            ? "bg-[#0a0f1e] text-cyan-50 border-cyan-500/60"
            : theme === "FrostedCrystal"
            ? "bg-white text-slate-900 border-blue-300"
            : "bg-zinc-900 text-zinc-100 border-indigo-500/50"
        }`}
      >
        {/* Header */}
        <div className="flex items-center justify-between border-b border-zinc-700/50 pb-3">
          <div className="flex items-center space-x-2.5">
            <div className="w-8 h-8 rounded-xl bg-indigo-950/80 border border-indigo-500/40 flex items-center justify-center text-indigo-400">
              <Sparkles className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-sm font-bold">✨ AI Tahlili &amp; Xatoliklarni Tuzatish Natijasi</h3>
              <p className="text-[11px] text-zinc-400">Grammatika, jadvallar va shrift xatoliklari to'g'rilandi</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-lg text-zinc-400 hover:text-white hover:bg-zinc-800 transition-colors cursor-pointer"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Content Box */}
        <div className="flex-1 my-4 overflow-hidden flex flex-col">
          <div className="text-xs font-semibold text-zinc-400 mb-2 flex items-center gap-1.5">
            <Wand2 className="w-3.5 h-3.5 text-indigo-400" />
            <span>Tuzatilgan Markdown Ko'rinishi:</span>
          </div>
          <div
            className={`flex-1 p-4 rounded-xl border font-mono text-xs overflow-y-auto leading-relaxed whitespace-pre-wrap ${
              theme === "FrostedCrystal"
                ? "bg-slate-50 text-slate-800 border-slate-200"
                : "bg-zinc-950 text-zinc-200 border-zinc-800"
            }`}
          >
            {correctedText}
          </div>
        </div>

        {/* Footer Actions */}
        <div className="flex items-center justify-end space-x-3 pt-3 border-t border-zinc-700/50">
          <button
            onClick={onClose}
            className="px-4 py-2 text-xs font-semibold text-zinc-400 hover:text-white rounded-xl hover:bg-zinc-800 cursor-pointer"
          >
            Bekor Qilish
          </button>
          <button
            onClick={() => {
              onApply(correctedText);
              onClose();
            }}
            className="flex items-center gap-1.5 px-5 py-2 text-xs font-bold bg-indigo-600 hover:bg-indigo-500 text-white rounded-xl shadow-xs transition-all cursor-pointer"
          >
            <Check className="w-4 h-4" />
            <span>✅ Tasdiqlash va Hujjatga Qo'llash</span>
          </button>
        </div>
      </div>
    </div>
  );
};
