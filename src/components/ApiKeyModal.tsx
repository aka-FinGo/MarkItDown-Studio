import React, { useState, useEffect } from "react";
import { Key, X, Check, ExternalLink, ShieldCheck, Sparkles, AlertCircle } from "lucide-react";

interface ApiKeyModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSaveApiKey: (key: string) => void;
  currentKey: string;
}

export const ApiKeyModal: React.FC<ApiKeyModalProps> = ({
  isOpen,
  onClose,
  onSaveApiKey,
  currentKey,
}) => {
  const [apiKey, setApiKey] = useState(currentKey);
  const [showSavedToast, setShowSavedToast] = useState(false);

  useEffect(() => {
    setApiKey(currentKey);
  }, [currentKey]);

  if (!isOpen) return null;

  const handleSave = () => {
    onSaveApiKey(apiKey.trim());
    setShowSavedToast(true);
    setTimeout(() => {
      setShowSavedToast(false);
      onClose();
    }, 800);
  };

  const handleClear = () => {
    setApiKey("");
    onSaveApiKey("");
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-zinc-900/60 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-zinc-200 space-y-5">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-zinc-100 pb-3">
          <div className="flex items-center space-x-2.5">
            <div className="w-8 h-8 rounded-xl bg-indigo-50 flex items-center justify-center text-indigo-600">
              <Key className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-zinc-900">Google Gemini API Kaliti</h3>
              <p className="text-[11px] text-zinc-500">Rasm OCR va Audio transkripsiyasi uchun</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-lg text-zinc-400 hover:text-zinc-600 hover:bg-zinc-100 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Free Local Engine Notice */}
        <div className="flex items-start gap-2.5 p-3 rounded-xl bg-emerald-50 border border-emerald-200/80 text-emerald-900 text-xs">
          <ShieldCheck className="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" />
          <div>
            <span className="font-semibold">Lokal fayllar 100% bepul:</span>
            <p className="text-[11px] text-emerald-800 mt-0.5">
              Word (.docx), Excel (.xlsx), PowerPoint (.pptx), PDF, CSV, JSON, Kod va Web sahifalarni o'girish uchun API kalit <strong>kerak emas</strong>. Ular to'g'ridan-to'g'ri brauzerda 0 tokenda o'giriladi.
            </p>
          </div>
        </div>

        {/* API Key Input */}
        <div className="space-y-2">
          <label className="block text-xs font-semibold text-zinc-800">
            Gemini API Kalitingiz:
          </label>
          <div className="relative">
            <input
              type="password"
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              placeholder="AIzaSy..."
              className="w-full px-3 py-2.5 text-xs font-mono border border-zinc-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-zinc-50/50"
            />
          </div>
          <p className="text-[11px] text-zinc-500 flex items-center gap-1.5">
            <Sparkles className="w-3 h-3 text-indigo-500" />
            Kalit faqat sizning brauzeringizning <code>localStorage</code> xotirasida saqlanadi va hech qayerga yuborilmaydi.
          </p>
        </div>

        {/* How to get a free key */}
        <div className="p-3 bg-zinc-50 rounded-xl border border-zinc-200/60 text-xs space-y-1.5">
          <div className="font-semibold text-zinc-800 flex items-center justify-between">
            <span>Bepul API kalit olish:</span>
            <a
              href="https://aistudio.google.com/app/apikey"
              target="_blank"
              rel="noopener noreferrer"
              className="text-indigo-600 hover:text-indigo-700 flex items-center gap-1 text-[11px] font-medium"
            >
              <span>Google AI Studio</span>
              <ExternalLink className="w-3 h-3" />
            </a>
          </div>
          <p className="text-[11px] text-zinc-600">
            1. Google AI Studio saytiga kiring. <br />
            2. "Get API Key" tugmasini bosing va bepul kalit yarating. <br />
            3. Shu yerga nusxalab joylashtiring.
          </p>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center justify-between pt-2 border-t border-zinc-100">
          {apiKey ? (
            <button
              onClick={handleClear}
              className="text-xs text-red-600 hover:text-red-700 font-medium px-2 py-1"
            >
              Kalitni o'chirish
            </button>
          ) : (
            <div />
          )}

          <div className="flex items-center space-x-2">
            <button
              onClick={onClose}
              className="px-4 py-2 text-xs font-semibold text-zinc-600 hover:text-zinc-800 rounded-xl hover:bg-zinc-100"
            >
              Bekor qilish
            </button>
            <button
              onClick={handleSave}
              className="flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl shadow-xs transition-all"
            >
              {showSavedToast ? <Check className="w-3.5 h-3.5" /> : <Key className="w-3.5 h-3.5" />}
              <span>{showSavedToast ? "Saqlandi!" : "Saqlash"}</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
