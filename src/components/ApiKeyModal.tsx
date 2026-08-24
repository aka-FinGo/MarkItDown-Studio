import React, { useState, useEffect } from "react";
import { Key, X, Check, ExternalLink, ShieldCheck, Sparkles, Server } from "lucide-react";

interface ApiKeyModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSaveApiKey: (key: string, provider: string, model: string, baseUrl?: string) => void;
  currentKey: string;
  currentProvider?: string;
  currentModel?: string;
}

export const ApiKeyModal: React.FC<ApiKeyModalProps> = ({
  isOpen,
  onClose,
  onSaveApiKey,
  currentKey,
  currentProvider = "GoogleGemini",
  currentModel = "gemini-2.5-flash",
}) => {
  const [provider, setProvider] = useState(currentProvider);
  const [model, setModel] = useState(currentModel);
  const [apiKey, setApiKey] = useState(currentKey);
  const [baseUrl, setBaseUrl] = useState("http://localhost:11434");
  const [showSavedToast, setShowSavedToast] = useState(false);

  useEffect(() => {
    setApiKey(currentKey);
    setProvider(currentProvider);
    setModel(currentModel);
  }, [currentKey, currentProvider, currentModel]);

  if (!isOpen) return null;

  const handleProviderChange = (p: string) => {
    setProvider(p);
    if (p === "GoogleGemini") setModel("gemini-2.5-flash");
    else if (p === "OpenAI") setModel("gpt-4o-mini");
    else if (p === "AnthropicClaude") setModel("claude-3-5-haiku-20241022");
    else if (p === "DeepSeek") setModel("deepseek-chat");
    else if (p === "Ollama") setModel("llama3.2-vision");
  };

  const handleSave = () => {
    onSaveApiKey(apiKey.trim(), provider, model, baseUrl);
    setShowSavedToast(true);
    setTimeout(() => {
      setShowSavedToast(false);
      onClose();
    }, 600);
  };

  const handleClear = () => {
    setApiKey("");
    onSaveApiKey("", provider, model, baseUrl);
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
              <h3 className="text-sm font-bold text-zinc-900">Multi-Provider AI Sozlamalari</h3>
              <p className="text-[11px] text-zinc-500">Rasm OCR, Tasvir tahlili va Audio transkripsiyasi</p>
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
            <span className="font-semibold">Lokal matnlar 100% bepul:</span>
            <p className="text-[11px] text-emerald-800 mt-0.5">
              Word (.docx), Excel (.xlsx), PowerPoint (.pptx), PDF, CSV, JSON va Kod fayllar uchun API kalit shart emas.
            </p>
          </div>
        </div>

        {/* Provider Selector */}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-semibold text-zinc-800 mb-1">AI Provayder:</label>
            <select
              value={provider}
              onChange={(e) => handleProviderChange(e.target.value)}
              className="w-full px-3 py-2 text-xs border border-zinc-200 rounded-xl bg-zinc-50 focus:outline-none focus:ring-1 focus:ring-indigo-500 font-medium"
            >
              <option value="GoogleGemini">Google Gemini (Tavsiya)</option>
              <option value="OpenAI">OpenAI</option>
              <option value="AnthropicClaude">Anthropic Claude</option>
              <option value="DeepSeek">DeepSeek</option>
              <option value="Ollama">Ollama (Lokal AI)</option>
              <option value="Custom">Custom Endpoint</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-zinc-800 mb-1">Model Nomi:</label>
            <input
              type="text"
              value={model}
              onChange={(e) => setModel(e.target.value)}
              className="w-full px-3 py-2 text-xs border border-zinc-200 rounded-xl bg-zinc-50 focus:outline-none focus:ring-1 focus:ring-indigo-500 font-mono"
            />
          </div>
        </div>

        {/* API Key Input */}
        <div className="space-y-1.5">
          <label className="block text-xs font-semibold text-zinc-800">
            API Kalit ({provider}):
          </label>
          <input
            type="password"
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            placeholder={provider === "GoogleGemini" ? "AIzaSy..." : "sk-..."}
            className="w-full px-3 py-2.5 text-xs font-mono border border-zinc-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-zinc-50/50"
          />
          <p className="text-[11px] text-zinc-500 flex items-center gap-1.5">
            <Sparkles className="w-3 h-3 text-indigo-500" />
            Kalit faqat sizning brauzeringizning <code>localStorage</code> xotirasida xavfsiz saqlanadi.
          </p>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center justify-between pt-2 border-t border-zinc-100">
          {apiKey ? (
            <button onClick={handleClear} className="text-xs text-red-600 hover:text-red-700 font-medium px-2 py-1">
              Kalitni o'chirish
            </button>
          ) : (
            <div />
          )}

          <div className="flex items-center space-x-2">
            <button onClick={onClose} className="px-4 py-2 text-xs font-semibold text-zinc-600 hover:text-zinc-800 rounded-xl hover:bg-zinc-100">
              Bekor qilish
            </button>
            <button
              onClick={handleSave}
              className="flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl shadow-xs transition-all cursor-pointer"
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
