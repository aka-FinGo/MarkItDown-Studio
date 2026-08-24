import React, { useState, useEffect } from "react";
import { Key, X, Check, ExternalLink, ShieldCheck, Sparkles, Zap, Server } from "lucide-react";

interface ApiKeyModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSaveApiKey: (key: string, provider: string, model: string, baseUrl?: string) => void;
  currentKey: string;
  currentProvider?: string;
  currentModel?: string;
}

const RECOMMENDED_MODELS: Record<string, string[]> = {
  GoogleGemini: [
    "gemini-2.5-flash",
    "gemini-2.5-pro",
    "gemini-3.7-flash",
    "gemini-3-flash",
    "gemini-3-pro",
    "gemini-3-deep-think",
    "gemini-2.5-flash-lite",
    "gemini-2.5-flash-image",
    "gemini-2.0-flash",
    "gemini-1.5-pro",
    "gemini-1.5-flash",
    "gemini-live-2.5-flash-preview-native-audio-09-2025",
  ],
  GroqAI: [
    "llama-3.3-70b-versatile",
    "llama-3.1-8b-instant",
    "deepseek-r1-distill-llama-70b",
    "qwen-qwq-32b",
    "mistral-saba-24b",
    "gemma2-9b-it",
    "whisper-large-v3-turbo",
    "whisper-large-v3",
    "meta-llama/llama-4-maverick-17b-128e-instruct",
  ],
  OpenAI: [
    "gpt-4o",
    "gpt-4o-mini",
    "o3-mini",
    "o1",
    "gpt-4-turbo",
  ],
  AnthropicClaude: [
    "claude-3-7-sonnet-20250219",
    "claude-3-5-sonnet-20241022",
    "claude-3-5-haiku-20241022",
  ],
  DeepSeek: [
    "deepseek-chat",
    "deepseek-reasoner",
  ],
  Ollama: [
    "llama3.2-vision",
    "llava:latest",
    "qwen2.5-vl:latest",
    "mistral:latest",
    "deepseek-r1:latest",
  ],
  Custom: [
    "default-model",
  ],
};

const PROVIDER_GUIDES: Record<string, { text: string; linkText: string; url: string }> = {
  GoogleGemini: {
    text: "Google AI Studio ga kiring va 1 daqiqada 100% bepul API kalit oling.",
    linkText: "Google AI Studio",
    url: "https://aistudio.google.com/app/apikey",
  },
  GroqAI: {
    text: "Groq Console ga kiring va chaqmoqdek tezkor (500+ tok/s) bepul API kalit oling.",
    linkText: "Groq Console",
    url: "https://console.groq.com/keys",
  },
  OpenAI: {
    text: "OpenAI Platform ga kiring va yangi Secret API Key yarating.",
    linkText: "OpenAI Platform",
    url: "https://platform.openai.com/api-keys",
  },
  AnthropicClaude: {
    text: "Anthropic Console orqali Claude API kalit oling.",
    linkText: "Anthropic Console",
    url: "https://console.anthropic.com/settings/keys",
  },
  DeepSeek: {
    text: "DeepSeek Platform dan arzon va yuqori intellektli API kalit oling.",
    linkText: "DeepSeek Platform",
    url: "https://platform.deepseek.com/api_keys",
  },
  Ollama: {
    text: "Kompyuteringizda 'ollama run llama3.2-vision' buyrug'ini bering (API kalit shart emas, 100% lokal).",
    linkText: "Ollama Sayti",
    url: "https://ollama.com",
  },
  Custom: {
    text: "O'zingizning OpenAI-mos keluvchi serveringiz manzilini va kalitini kiriting.",
    linkText: "Sozlash",
    url: "#",
  },
};

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
    const models = RECOMMENDED_MODELS[p] || ["default-model"];
    setModel(models[0]);
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

  const currentGuide = PROVIDER_GUIDES[provider] || PROVIDER_GUIDES.GoogleGemini;
  const availableModels = RECOMMENDED_MODELS[provider] || [model];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-zinc-950/70 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-zinc-900 text-zinc-100 rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-zinc-700/60 space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-zinc-800 pb-3">
          <div className="flex items-center space-x-2.5">
            <div className="w-8 h-8 rounded-xl bg-indigo-950/80 border border-indigo-500/40 flex items-center justify-center text-indigo-400">
              <Key className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white">Multi-Provider AI &amp; Modellar</h3>
              <p className="text-[11px] text-zinc-400">Gemini, Groq, OpenAI, Claude, DeepSeek, Ollama</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-lg text-zinc-400 hover:text-white hover:bg-zinc-800 transition-colors cursor-pointer"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Provider & Model Selectors */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-semibold text-zinc-300 mb-1">AI Provayder:</label>
            <select
              value={provider}
              onChange={(e) => handleProviderChange(e.target.value)}
              className="w-full px-3 py-2 text-xs border border-zinc-700 rounded-xl bg-zinc-800 text-white focus:outline-none focus:ring-1 focus:ring-indigo-500 font-medium cursor-pointer"
            >
              <option value="GoogleGemini">Google Gemini (Barcha Modellar)</option>
              <option value="GroqAI">⚡ Groq AI (Ultra-Tez 500+ tok/s)</option>
              <option value="OpenAI">OpenAI (GPT-4o, o1, o3-mini)</option>
              <option value="AnthropicClaude">Anthropic Claude 3.7 / 3.5</option>
              <option value="DeepSeek">DeepSeek (V3, R1 Reasoner)</option>
              <option value="Ollama">Ollama (Lokal / Ofline)</option>
              <option value="Custom">Custom Endpoint</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-zinc-300 mb-1">Modelni Tanlang:</label>
            <select
              value={model}
              onChange={(e) => setModel(e.target.value)}
              className="w-full px-3 py-2 text-xs border border-zinc-700 rounded-xl bg-zinc-800 text-white focus:outline-none focus:ring-1 focus:ring-indigo-500 font-mono cursor-pointer"
            >
              {availableModels.map((m) => (
                <option key={m} value={m}>
                  {m}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Base URL for Ollama / Custom */}
        {(provider === "Ollama" || provider === "Custom") && (
          <div>
            <label className="block text-xs font-semibold text-zinc-300 mb-1">Base URL:</label>
            <input
              type="text"
              value={baseUrl}
              onChange={(e) => setBaseUrl(e.target.value)}
              placeholder="http://localhost:11434"
              className="w-full px-3 py-2 text-xs font-mono border border-zinc-700 rounded-xl bg-zinc-800 text-white focus:outline-none focus:ring-1 focus:ring-indigo-500"
            />
          </div>
        )}

        {/* API Key Input */}
        {provider !== "Ollama" && (
          <div className="space-y-1.5">
            <label className="block text-xs font-semibold text-zinc-300">
              API Kalitingiz ({provider}):
            </label>
            <input
              type="password"
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              placeholder={provider === "GoogleGemini" ? "AIzaSy..." : provider === "GroqAI" ? "gsk_..." : "sk-..."}
              className="w-full px-3 py-2.5 text-xs font-mono border border-zinc-700 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-500 bg-zinc-800/80 text-white"
            />
          </div>
        )}

        {/* Dynamic Contextual Guide Banner when API Key is empty */}
        {provider !== "Ollama" && !apiKey.trim() && (
          <div className="p-3.5 bg-indigo-950/50 border border-indigo-500/40 rounded-xl text-xs space-y-1 animate-in fade-in">
            <div className="flex items-center justify-between text-indigo-300 font-bold">
              <span>{provider} API kalitini olish:</span>
              <a
                href={currentGuide.url}
                target="_blank"
                rel="noopener noreferrer"
                className="text-indigo-400 hover:text-indigo-300 flex items-center gap-1 text-[11px] underline font-medium"
              >
                <span>{currentGuide.linkText}</span>
                <ExternalLink className="w-3 h-3" />
              </a>
            </div>
            <p className="text-[11px] text-zinc-300 leading-relaxed">
              {currentGuide.text}
            </p>
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex items-center justify-between pt-2 border-t border-zinc-800">
          {apiKey ? (
            <button onClick={handleClear} className="text-xs text-red-400 hover:text-red-300 font-medium px-2 py-1 cursor-pointer">
              Kalitni o'chirish
            </button>
          ) : (
            <div />
          )}

          <div className="flex items-center space-x-2">
            <button onClick={onClose} className="px-4 py-2 text-xs font-semibold text-zinc-400 hover:text-white rounded-xl hover:bg-zinc-800 cursor-pointer">
              Bekor qilish
            </button>
            <button
              onClick={handleSave}
              className="flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-indigo-600 hover:bg-indigo-500 text-white rounded-xl shadow-xs transition-all cursor-pointer"
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
