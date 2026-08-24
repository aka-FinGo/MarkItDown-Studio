/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React, { useState, useEffect } from "react";
import {
  UploadCloud,
  Globe,
  AlertCircle,
  Loader2,
} from "lucide-react";
import { Header } from "./components/Header";
import { DropZone } from "./components/DropZone";
import { UrlConverter } from "./components/UrlConverter";
import { MarkdownViewer } from "./components/MarkdownViewer";
import { ApiDocsModal } from "./components/ApiDocsModal";
import { SupportedFormatsModal } from "./components/SupportedFormatsModal";
import { ApiKeyModal } from "./components/ApiKeyModal";
import { AiProofreadModal } from "./components/AiProofreadModal";
import { ConvertedItem, ConversionOptions, ThemeType } from "./types";
import { Language, TRANSLATIONS } from "./locales/i18n";
import {
  convertFileClient,
  convertUrlClient,
  convertWithGeminiApi,
  convertWithOpenAiApi,
} from "./services/converterService";

export default function App() {
  const [activeTab, setActiveTab] = useState<"upload" | "url">("upload");
  const [options, setOptions] = useState<ConversionOptions>({
    enableAi: true,
    includeFrontmatter: false,
    tableStyle: "standard",
    customPrompt: "",
  });

  const [language, setLanguage] = useState<Language>("uz");
  const [theme, setTheme] = useState<ThemeType>("MidnightGlass");
  const [aiProvider, setAiProvider] = useState<string>("GoogleGemini");
  const [aiModel, setAiModel] = useState<string>("gemini-2.5-flash");
  const [apiKey, setApiKey] = useState<string>("");
  const [customBaseUrl, setCustomBaseUrl] = useState<string>("http://localhost:11434");

  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [convertedItems, setConvertedItems] = useState<ConvertedItem[]>([]);
  const [activeItemId, setActiveItemId] = useState<string>("");
  const [isConverting, setIsConverting] = useState(false);
  const [conversionProgress, setConversionProgress] = useState<{ current: number; total: number; filename: string } | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Modals state
  const [isApiDocsOpen, setIsApiDocsOpen] = useState(false);
  const [isFormatsOpen, setIsFormatsOpen] = useState(false);
  const [isApiKeyModalOpen, setIsApiKeyModalOpen] = useState(false);
  const [isAiProofreadOpen, setIsAiProofreadOpen] = useState(false);
  const [aiProofreadResult, setAiProofreadResult] = useState<string>("");
  const [isProofreading, setIsProofreading] = useState(false);

  const t = TRANSLATIONS[language];

  // Load saved state from localStorage
  useEffect(() => {
    const savedLang = localStorage.getItem("markitdown_lang") as Language;
    if (savedLang) setLanguage(savedLang);

    const savedTheme = localStorage.getItem("markitdown_theme") as ThemeType;
    if (savedTheme) setTheme(savedTheme);

    const savedProvider = localStorage.getItem("markitdown_ai_provider");
    if (savedProvider) setAiProvider(savedProvider);

    const savedModel = localStorage.getItem("markitdown_ai_model");
    if (savedModel) setAiModel(savedModel);

    const savedKey = localStorage.getItem(`markitdown_api_key_${savedProvider || "GoogleGemini"}`);
    if (savedKey) setApiKey(savedKey);

    const savedItems = localStorage.getItem("markitdown_items");
    if (savedItems) {
      try {
        const parsed = JSON.parse(savedItems);
        if (Array.isArray(parsed) && parsed.length > 0) {
          setConvertedItems(parsed);
          setActiveItemId(parsed[0].id);
        }
      } catch (e) {
        console.error("Failed to parse saved items:", e);
      }
    }
  }, []);

  // Save converted items to localStorage
  useEffect(() => {
    if (convertedItems.length > 0) {
      localStorage.setItem("markitdown_items", JSON.stringify(convertedItems.slice(0, 15)));
    } else {
      localStorage.removeItem("markitdown_items");
    }
  }, [convertedItems]);

  const handleLanguageChange = (newLang: Language) => {
    setLanguage(newLang);
    localStorage.setItem("markitdown_lang", newLang);
  };

  const handleThemeChange = (newTheme: ThemeType) => {
    setTheme(newTheme);
    localStorage.setItem("markitdown_theme", newTheme);
  };

  const handleSaveApiKey = (key: string, provider: string, model: string, baseUrl?: string) => {
    setApiKey(key);
    setAiProvider(provider);
    setAiModel(model);
    if (baseUrl) setCustomBaseUrl(baseUrl);

    localStorage.setItem("markitdown_ai_provider", provider);
    localStorage.setItem("markitdown_ai_model", model);
    if (key) {
      localStorage.setItem(`markitdown_api_key_${provider}`, key);
    } else {
      localStorage.removeItem(`markitdown_api_key_${provider}`);
    }
  };

  const handleFilesSelected = (newFiles: File[]) => {
    setSelectedFiles((prev) => [...prev, ...newFiles]);
    setErrorMessage(null);
  };

  const handleRemoveFile = (index: number) => {
    setSelectedFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const handleClearFiles = () => {
    setSelectedFiles([]);
  };

  // Multi-File Async Batch Converter
  const handleConvertFiles = async () => {
    if (selectedFiles.length === 0) return;
    setIsConverting(true);
    setErrorMessage(null);

    const total = selectedFiles.length;
    const newResults: ConvertedItem[] = [];

    try {
      for (let i = 0; i < total; i++) {
        const file = selectedFiles[i];
        setConversionProgress({ current: i + 1, total, filename: file.name });

        await new Promise((resolve) => setTimeout(resolve, 20));

        try {
          const results = await convertFileClient(
            file,
            options,
            apiKey,
            aiProvider,
            aiModel,
            customBaseUrl
          );
          newResults.push(...results);
        } catch (fileErr: any) {
          console.error(`Error converting ${file.name}:`, fileErr);
          newResults.push({
            id: `err_${Date.now()}_${i}`,
            filename: file.name,
            originalFormat: file.name.split(".").pop()?.toUpperCase() || "FILE",
            originalSize: file.size,
            markdown: `# ⚠️ Xatolik\n\nFaylni o'girishda xatolik yuz berdi: ${fileErr.message || fileErr}`,
            markdownSize: 0,
            wordCount: 0,
            charCount: 0,
            lineCount: 0,
            estimatedTokens: 0,
            durationMs: 0,
            usedAi: false,
            status: "error",
            errorMessage: fileErr.message || "Xatolik",
          });
        }
      }

      setConvertedItems((prev) => [...newResults, ...prev]);
      if (newResults.length > 0) {
        setActiveItemId(newResults[0].id);
      }
      setSelectedFiles([]);
    } catch (err: any) {
      setErrorMessage(err.message || "Fayllarni o'girishda xatolik yuz berdi");
    } finally {
      setIsConverting(false);
      setConversionProgress(null);
    }
  };

  const handleConvertUrl = async (url: string) => {
    setIsConverting(true);
    setErrorMessage(null);
    try {
      const result = await convertUrlClient(url, options, apiKey);
      setConvertedItems((prev) => [result, ...prev]);
      setActiveItemId(result.id);
    } catch (err: any) {
      setErrorMessage(err.message || "URL ni o'girishda xatolik yuz berdi");
    } finally {
      setIsConverting(false);
    }
  };

  const activeItem = convertedItems.find((item) => item.id === activeItemId) || convertedItems[0];

  // AI Proofreading flow
  const handleTriggerAiProofread = async () => {
    if (!activeItem || !activeItem.markdown) return;
    if (aiProvider !== "Ollama" && (!apiKey || apiKey.trim().length === 0)) {
      setIsApiKeyModalOpen(true);
      return;
    }

    setIsProofreading(true);
    setErrorMessage(null);

    try {
      const prompt = `Ushbu Markdown hujjatidagi barcha orfografik, grammatik xatoliklarni, buzilgan jadvallarni va Lotin/Krill chalkashliklarini (o'/ў, g'/ғ, sh/ш, ch/ч) to'liq to'g'rilab, toza va chiroyli Markdown qilib qaytar. Ortiqcha izohlarsiz, faqat to'g'rilangan yakuniy Markdownni ber.`;

      let resultText = "";
      const base64Text = btoa(unescape(encodeURIComponent(activeItem.markdown)));

      if (aiProvider === "GoogleGemini") {
        const res = await convertWithGeminiApi(base64Text, "text/plain", "document_review.md", apiKey, aiModel, prompt);
        resultText = res.markdown;
      } else if (aiProvider === "GroqAI") {
        const res = await convertWithOpenAiApi(base64Text, "text/plain", "document_review.md", apiKey, "https://api.groq.com/openai/v1/chat/completions", aiModel, prompt);
        resultText = res.markdown;
      } else {
        const res = await convertWithOpenAiApi(base64Text, "text/plain", "document_review.md", apiKey, customBaseUrl || "https://api.openai.com/v1/chat/completions", aiModel, prompt);
        resultText = res.markdown;
      }

      setAiProofreadResult(resultText);
      setIsAiProofreadOpen(true);
    } catch (err: any) {
      setErrorMessage(`AI tahlilida xatolik: ${err.message || err}`);
    } finally {
      setIsProofreading(false);
    }
  };

  const handleApplyAiProofread = (correctedText: string) => {
    if (!activeItem) return;
    const updated = {
      ...activeItem,
      markdown: correctedText,
      markdownSize: new Blob([correctedText]).size,
      wordCount: correctedText.replace(/```[\s\S]*?```/g, "").replace(/[#*_`\[\]()]/g, " ").trim().match(/\S+/g)?.length || 0,
      charCount: correctedText.length,
      lineCount: correctedText.split("\n").length,
    };

    setConvertedItems((prev) => prev.map((item) => (item.id === activeItem.id ? updated : item)));
  };

  return (
    <div
      className={`min-h-screen flex flex-col font-sans transition-colors duration-200 ${
        theme === "ObsidianDark"
          ? "bg-[#181818] text-zinc-100"
          : theme === "CyberpunkNeon"
          ? "bg-[#050811] text-cyan-50"
          : theme === "FrostedCrystal"
          ? "bg-slate-100 text-slate-900"
          : "bg-slate-950 text-slate-100"
      }`}
    >
      <Header
        onOpenApiDocs={() => setIsApiDocsOpen(true)}
        onOpenFormats={() => setIsFormatsOpen(true)}
        onOpenApiKey={() => setIsApiKeyModalOpen(true)}
        hasApiKey={Boolean(apiKey && apiKey.trim().length > 0)}
        theme={theme}
        onThemeChange={handleThemeChange}
        language={language}
        onLanguageChange={handleLanguageChange}
      />

      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6 flex flex-col">
        {/* Error Alert */}
        {errorMessage && (
          <div className="mb-6 p-4 bg-red-900/30 border border-red-500/50 rounded-xl flex items-start space-x-3 text-red-200 animate-in fade-in">
            <AlertCircle className="w-5 h-5 text-red-400 shrink-0 mt-0.5" />
            <div className="flex-1">
              <h4 className="text-sm font-semibold">Xatolik</h4>
              <p className="text-xs text-red-300 mt-0.5">{errorMessage}</p>
            </div>
            <button
              onClick={() => setErrorMessage(null)}
              className="text-xs font-semibold text-red-400 hover:text-red-300"
            >
              Yopish
            </button>
          </div>
        )}

        {/* Proofreading Loader Banner */}
        {isProofreading && (
          <div className="mb-6 p-3 bg-indigo-950/70 border border-indigo-500/50 rounded-xl flex items-center space-x-3 text-indigo-200 animate-in fade-in">
            <Loader2 className="w-4 h-4 animate-spin text-indigo-400 shrink-0" />
            <span className="text-xs font-semibold">{t.proofreadingProgress}</span>
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 flex-1 items-start">
          {/* LEFT COLUMN: Input / Controls */}
          <div className="lg:col-span-5 space-y-4 flex flex-col">
            {/* Tabs */}
            <div className="flex rounded-xl bg-zinc-900/80 p-1 border border-zinc-800 shadow-xs">
              <button
                onClick={() => setActiveTab("upload")}
                className={`flex-1 flex items-center justify-center gap-2 py-2 rounded-lg text-xs font-semibold transition-all cursor-pointer ${
                  activeTab === "upload"
                    ? "bg-indigo-600 text-white shadow-sm"
                    : "text-zinc-400 hover:text-zinc-200"
                }`}
              >
                <UploadCloud className="w-3.5 h-3.5" />
                <span>{t.dropTitle}</span>
              </button>
              <button
                onClick={() => setActiveTab("url")}
                className={`flex-1 flex items-center justify-center gap-2 py-2 rounded-lg text-xs font-semibold transition-all cursor-pointer ${
                  activeTab === "url"
                    ? "bg-indigo-600 text-white shadow-sm"
                    : "text-zinc-400 hover:text-zinc-200"
                }`}
              >
                <Globe className="w-3.5 h-3.5" />
                <span>Web URL</span>
              </button>
            </div>

            {/* Batch Progress Bar */}
            {conversionProgress && (
              <div className="p-3 bg-indigo-950/60 border border-indigo-500/40 rounded-xl space-y-2 animate-in fade-in">
                <div className="flex items-center justify-between text-xs text-indigo-200 font-semibold">
                  <span className="flex items-center gap-2">
                    <Loader2 className="w-3.5 h-3.5 animate-spin text-indigo-400" />
                    <span>{t.convertingProgress} ({conversionProgress.current}/{conversionProgress.total}):</span>
                  </span>
                  <span className="truncate max-w-[160px]">{conversionProgress.filename}</span>
                </div>
                <div className="w-full bg-indigo-900/50 rounded-full h-1.5 overflow-hidden">
                  <div
                    className="bg-indigo-500 h-1.5 transition-all duration-300"
                    style={{
                      width: `${(conversionProgress.current / conversionProgress.total) * 100}%`,
                    }}
                  />
                </div>
              </div>
            )}

            {/* Tab Content */}
            <div className="flex-1">
              {activeTab === "upload" && (
                <div className="space-y-4">
                  <DropZone
                    selectedFiles={selectedFiles}
                    onFilesSelected={handleFilesSelected}
                    onRemoveFile={handleRemoveFile}
                    onClearFiles={handleClearFiles}
                    onConvert={handleConvertFiles}
                    isConverting={isConverting}
                    language={language}
                    theme={theme}
                  />
                </div>
              )}

              {activeTab === "url" && (
                <UrlConverter
                  onConvert={handleConvertUrl}
                  isConverting={isConverting}
                />
              )}
            </div>
          </div>

          {/* RIGHT COLUMN: Output Viewer */}
          <div className="lg:col-span-7 flex flex-col">
            <MarkdownViewer
              item={activeItem}
              items={convertedItems}
              activeItemId={activeItemId}
              onSelectItem={(id) => setActiveItemId(id)}
              onClearHistory={() => {
                setConvertedItems([]);
                localStorage.removeItem("markitdown_items");
              }}
              onDeleteItem={(id) => {
                setConvertedItems((prev) => prev.filter((i) => i.id !== id));
              }}
              onAiProofread={handleTriggerAiProofread}
              theme={theme}
            />
          </div>
        </div>
      </main>

      {/* Modals */}
      <ApiDocsModal
        isOpen={isApiDocsOpen}
        onClose={() => setIsApiDocsOpen(false)}
      />
      <SupportedFormatsModal
        isOpen={isFormatsOpen}
        onClose={() => setIsFormatsOpen(false)}
      />
      <ApiKeyModal
        isOpen={isApiKeyModalOpen}
        onClose={() => setIsApiKeyModalOpen(false)}
        onSaveApiKey={handleSaveApiKey}
        currentKey={apiKey}
        currentProvider={aiProvider}
        currentModel={aiModel}
      />
      <AiProofreadModal
        isOpen={isAiProofreadOpen}
        onClose={() => setIsAiProofreadOpen(false)}
        originalText={activeItem?.markdown || ""}
        correctedText={aiProofreadResult}
        onApply={handleApplyAiProofread}
        theme={theme}
      />
    </div>
  );
}
