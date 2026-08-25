import React, { useState, useEffect } from "react";
import { Key, X, Check, ExternalLink, Trash2, Plus, Server } from "lucide-react";

interface ApiKeyModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSaveApiKey: (key: string, provider: string, model: string, baseUrl?: string) => void;
  currentKey: string;
  currentProvider?: string;
  currentModel?: string;
}

const DEFAULT_RECOMMENDED_MODELS: Record<string, string[]> = {
  GoogleGemini: [
    "gemini-2.5-flash",
    "gemini-2.5-pro",
    "gemini-2.5-flash-lite",
    "gemini-2.0-flash",
    "gemini-2.0-pro-exp-02-05",
    "gemini-1.5-pro",
    "gemini-1.5-flash",
  ],
  GroqAI: [
    "llama-3.3-70b-versatile",
    "llama-3.1-8b-instant",
    "deepseek-r1-distill-llama-70b",
    "qwen-qwq-32b",
    "mistral-saba-24b",
    "gemma2-9b-it",
    "whisper-large-v3-turbo",
  ],
  OpenRouter: [
    "google/gemini-2.5-flash",
    "google/gemini-2.5-pro",
    "deepseek/deepseek-r1",
    "deepseek/deepseek-chat",
    "anthropic/claude-3.7-sonnet",
    "openai/gpt-4o",
    "openai/gpt-4o-mini",
    "meta-llama/llama-3.3-70b-instruct",
    "qwen/qwen-2.5-72b-instruct",
    "mistralai/mistral-large-2411",
  ],
  OpenAI: [
    "gpt-4o",
    "gpt-4o-mini",
    "o3-mini",
    "o1",
    "o1-mini",
    "gpt-4-turbo",
  ],
  AnthropicClaude: [
    "claude-3-7-sonnet-20250219",
    "claude-3-5-sonnet-20241022",
    "claude-3-5-haiku-20241022",
    "claude-3-opus-20240229",
  ],
  DeepSeek: [
    "deepseek-chat",
    "deepseek-reasoner",
  ],
  OllamaLocal: [
    "llama3.2-vision",
    "llava:latest",
    "qwen2.5-vl:latest",
    "deepseek-r1:14b",
    "deepseek-r1:8b",
    "llama3.3:70b",
    "mistral:latest",
    "phi4:latest",
  ],
  OllamaCloud: [
    "llama3.2-vision",
    "deepseek-r1:latest",
    "qwen2.5-vl:latest",
    "llama3.3:latest",
    "mistral:latest",
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
  OpenRouter: {
    text: "OpenRouter ga kiring va yagona API kalit orqali 300+ AI modellardan foydalaning.",
    linkText: "OpenRouter Keys",
    url: "https://openrouter.ai/keys",
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
  OllamaLocal: {
    text: "Kompyuteringizda 'ollama run llama3.2-vision' buyrug'ini bering (Lokal, API kalitsiz).",
    linkText: "Ollama Sayti",
    url: "https://ollama.com",
  },
  OllamaCloud: {
    text: "Masofaviy Ollama serveringiz manzilini (Base URL) va model nomini kiriting.",
    linkText: "Ollama Docs",
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
  const [newModelInput, setNewModelInput] = useState("");
  const [customModels, setCustomModels] = useState<Record<string, string[]>>({});
  const [showSavedToast, setShowSavedToast] = useState(false);

  // Load custom models from localStorage
  useEffect(() => {
    try {
      const saved = localStorage.getItem("markitdown_custom_models");
      if (saved) {
        setCustomModels(JSON.parse(saved));
      }
    } catch (e) {
      console.error("Failed to load custom models:", e);
    }
  }, []);

  useEffect(() => {
    setApiKey(currentKey);
    setProvider(currentProvider);
    setModel(currentModel);
  }, [currentKey, currentProvider, currentModel]);

  if (!isOpen) return null;

  const getCombinedModels = (p: string) => {
    const defaults = DEFAULT_RECOMMENDED_MODELS[p] || ["default-model"];
    const customs = customModels[p] || [];
    const combined = [...defaults];
    customs.forEach((m) => {
      if (!combined.includes(m)) combined.push(m);
    });
    return combined;
  };

  const handleProviderChange = (p: string) => {
    setProvider(p);
    const models = getCombinedModels(p);
    setModel(models[0]);
    if (p === "OllamaLocal") {
      setBaseUrl("http://localhost:11434");
    }
  };

  const handleAddCustomModel = () => {
    const trimmed = newModelInput.trim();
    if (!trimmed) return;

    const currentList = customModels[provider] || [];
    if (!currentList.includes(trimmed)) {
      const updated = {
        ...customModels,
        [provider]: [...currentList, trimmed],
      };
      setCustomModels(updated);
      localStorage.setItem("markitdown_custom_models", JSON.stringify(updated));
    }

    setModel(trimmed);
    setNewModelInput("");
  };

  const handleDeleteModel = (modelToDelete: string) => {
    const currentList = customModels[provider] || [];
    const updatedList = currentList.filter((m) => m !== modelToDelete);
    const updated = {
      ...customModels,
      [provider]: updatedList,
    };
    setCustomModels(updated);
    localStorage.setItem("markitdown_custom_models", JSON.stringify(updated));

    const remaining = getCombinedModels(provider).filter((m) => m !== modelToDelete);
    if (remaining.length > 0) {
      setModel(remaining[0]);
    }
  };

  const handleSave = () => {
    onSaveApiKey(apiKey.trim(), provider, model.trim(), baseUrl);
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
  const availableModels = getCombinedModels(provider);
  const isCustomModel = (customModels[provider] || []).includes(model);

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
              <p className="text-[11px] text-zinc-400">Gemini, Groq, OpenRouter, OpenAI, Claude, DeepSeek, Ollama</p>
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
        <div className="space-y-3">
          <div>
            <label className="block text-xs font-semibold text-zinc-300 mb-1">AI Provayder:</label>
            <select
              value={provider}
              onChange={(e) => handleProviderChange(e.target.value)}
              className="w-full px-3 py-2 text-xs border border-zinc-700 rounded-xl bg-zinc-800 text-white focus:outline-none focus:ring-1 focus:ring-indigo-500 font-medium cursor-pointer"
            >
              <option value="GoogleGemini">Google Gemini (100% Bepul Vision OCR)</option>
              <option value="GroqAI">⚡ Groq AI (Ultra-Tez 500+ tok/s)</option>
              <option value="OpenRouter">🌐 OpenRouter (300+ AI Modellari)</option>
              <option value="OpenAI">OpenAI (GPT-4o, o3-mini, o1)</option>
              <option value="AnthropicClaude">Anthropic Claude (Claude 3.7 Sonnet)</option>
              <option value="DeepSeek">DeepSeek (V3, R1 Reasoner)</option>
              <option value="OllamaLocal">Ollama (Lokal / Desktop)</option>
              <option value="OllamaCloud">Ollama (Cloud / Masofaviy Endpoint)</option>
              <option value="Custom">Custom OpenAI-Mos Endpoint</option>
            </select>
          </div>

          {/* Model Selection & Custom Model Management */}
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="text-xs font-semibold text-zinc-300">Modelni Tanlang yoki Tahrirlang:</label>
              {isCustomModel && (
                <button
                  onClick={() => handleDeleteModel(model)}
                  className="text-[11px] text-red-400 hover:text-red-300 flex items-center gap-1 cursor-pointer font-medium"
                  title="Tanlangan modelni o'chirish"
                >
                  <Trash2 className="w-3 h-3" />
                  <span>Modelni o'chirish</span>
                </button>
              )}
            </div>
            <div className="flex gap-2">
              <select
                value={model}
                onChange={(e) => setModel(e.target.value)}
                className="flex-1 px-3 py-2 text-xs border border-zinc-700 rounded-xl bg-zinc-800 text-white focus:outline-none focus:ring-1 focus:ring-indigo-500 font-mono cursor-pointer"
              >
                {availableModels.map((m) => (
                  <option key={m} value={m}>
                    {m}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Add New Custom Model Input */}
          <div className="flex gap-2 items-center">
            <input
              type="text"
              value={newModelInput}
              onChange={(e) => setNewModelInput(e.target.value)}
              placeholder="Yangi model nomini kiriting (masalan: gemini-2.0-flash)..."
              className="flex-1 px-3 py-1.5 text-xs font-mono border border-zinc-700 rounded-xl bg-zinc-800/90 text-white placeholder-zinc-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            />
            <button
              onClick={handleAddCustomModel}
              disabled={!newModelInput.trim()}
              className="px-3 py-1.5 bg-zinc-800 hover:bg-zinc-700 border border-zinc-700 text-indigo-400 hover:text-indigo-300 rounded-xl text-xs font-semibold flex items-center gap-1 cursor-pointer disabled:opacity-50"
            >
              <Plus className="w-3.5 h-3.5" />
              <span>Qo'shish</span>
            </button>
          </div>
        </div>

        {/* Base URL for Ollama / Custom */}
        {(provider === "OllamaLocal" || provider === "OllamaCloud" || provider === "Custom") && (
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
        {provider !== "OllamaLocal" && (
          <div className="space-y-1.5">
            <label className="block text-xs font-semibold text-zinc-300">
              API Kalitingiz ({provider}):
            </label>
            <input
              type="password"
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              placeholder={provider === "GoogleGemini" ? "AIzaSy..." : provider === "GroqAI" ? "gsk_..." : provider === "OpenRouter" ? "sk-or-v1-..." : "sk-..."}
              className="w-full px-3 py-2.5 text-xs font-mono border border-zinc-700 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-500 bg-zinc-800/80 text-white"
            />
          </div>
        )}

        {/* Dynamic Contextual Guide Banner when API Key is empty */}
        {provider !== "OllamaLocal" && !apiKey.trim() && (
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
