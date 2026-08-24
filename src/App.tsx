/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React, { useState, useEffect } from "react";
import {
  UploadCloud,
  Globe,
  Code,
  FileCheck2,
  AlertCircle,
  Loader2,
} from "lucide-react";
import { Header } from "./components/Header";
import { DropZone } from "./components/DropZone";
import { UrlConverter } from "./components/UrlConverter";
import { TextConverter } from "./components/TextConverter";
import { ConversionSettings } from "./components/ConversionSettings";
import { MarkdownViewer } from "./components/MarkdownViewer";
import { SamplePicker } from "./components/SamplePicker";
import { ApiDocsModal } from "./components/ApiDocsModal";
import { SupportedFormatsModal } from "./components/SupportedFormatsModal";
import { ApiKeyModal } from "./components/ApiKeyModal";
import { ConvertedItem, ConversionOptions } from "./types";
import { SampleFile } from "./data/samples";
import {
  convertFileClient,
  convertUrlClient,
  convertTextClient,
} from "./services/converterService";

export default function App() {
  const [activeTab, setActiveTab] = useState<"upload" | "url" | "text">("upload");
  const [options, setOptions] = useState<ConversionOptions>({
    enableAi: false,
    includeFrontmatter: false, // Default OFF as requested by user
    tableStyle: "standard",
    customPrompt: "",
  });

  const [geminiApiKey, setGeminiApiKey] = useState<string>("");
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [convertedItems, setConvertedItems] = useState<ConvertedItem[]>([]);
  const [activeItemId, setActiveItemId] = useState<string>("");
  const [isConverting, setIsConverting] = useState(false);
  const [conversionStatusMsg, setConversionStatusMsg] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const [isApiDocsOpen, setIsApiDocsOpen] = useState(false);
  const [isFormatsOpen, setIsFormatsOpen] = useState(false);
  const [isApiKeyModalOpen, setIsApiKeyModalOpen] = useState(false);

  // Load from localStorage on mount
  useEffect(() => {
    const savedKey = localStorage.getItem("markitdown_gemini_api_key");
    if (savedKey) {
      setGeminiApiKey(savedKey);
    }

    const saved = localStorage.getItem("markitdown_items");
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        if (Array.isArray(parsed) && parsed.length > 0) {
          setConvertedItems(parsed);
          setActiveItemId(parsed[0].id);
        }
      } catch (e) {
        console.error("Failed to parse saved items:", e);
      }
    }
  }, []);

  // Save items to localStorage
  useEffect(() => {
    if (convertedItems.length > 0) {
      localStorage.setItem("markitdown_items", JSON.stringify(convertedItems.slice(0, 15)));
    } else {
      localStorage.removeItem("markitdown_items");
    }
  }, [convertedItems]);

  const handleSaveApiKey = (key: string) => {
    setGeminiApiKey(key);
    if (key) {
      localStorage.setItem("markitdown_gemini_api_key", key);
    } else {
      localStorage.removeItem("markitdown_gemini_api_key");
    }
  };

  const handleFilesSelected = (newFiles: File[]) => {
    setSelectedFiles((prev) => [...prev, ...newFiles]);
    setErrorMessage(null);
  };

  const handleRemoveFile = (index: number) => {
    setSelectedFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const handleClearAllFiles = () => {
    setSelectedFiles([]);
  };

  // Convert uploaded files (100% in-browser)
  const handleConvertFiles = async () => {
    if (selectedFiles.length === 0) return;
    setIsConverting(true);
    setConversionStatusMsg(`${selectedFiles.length} ta fayl Markdown (.md) formatiga o'tkazilmoqda...`);
    setErrorMessage(null);

    try {
      const allResults: ConvertedItem[] = [];

      for (let i = 0; i < selectedFiles.length; i++) {
        const file = selectedFiles[i];
        setConversionStatusMsg(`Fayl tahlil qilinmoqda (${i + 1}/${selectedFiles.length}): ${file.name}`);
        const results = await convertFileClient(file, options, geminiApiKey);
        allResults.push(...results);
      }

      setConvertedItems((prev) => [...allResults, ...prev]);
      if (allResults.length > 0) {
        setActiveItemId(allResults[0].id);
      }
      setSelectedFiles([]);
    } catch (err: any) {
      console.error("Conversion error:", err);
      setErrorMessage(err.message || "Fayllarni konvertatsiya qilishda xatolik. Qaytadan urinib ko'ring.");
    } finally {
      setIsConverting(false);
      setConversionStatusMsg("");
    }
  };

  // Convert URL (100% in-browser via r.jina.ai)
  const handleConvertUrl = async (url: string) => {
    setIsConverting(true);
    setConversionStatusMsg("Web sahifa yuklanib, toza Markdown matniga o'tkazilmoqda...");
    setErrorMessage(null);

    try {
      const newItem = await convertUrlClient(url, options, geminiApiKey);
      setConvertedItems((prev) => [newItem, ...prev]);
      setActiveItemId(newItem.id);
    } catch (err: any) {
      console.error("URL error:", err);
      setErrorMessage(err.message || "URL manzilni o'girishda xatolik yuz berdi.");
    } finally {
      setIsConverting(false);
      setConversionStatusMsg("");
    }
  };

  // Convert raw text snippet (100% in-browser)
  const handleConvertText = async (text: string, format: string, filename: string) => {
    setIsConverting(true);
    setConversionStatusMsg("Matn tahlil qilinib, Markdown formatiga keltirilmoqda...");
    setErrorMessage(null);

    try {
      const newItem = convertTextClient(text, format, filename, options);
      setConvertedItems((prev) => [newItem, ...prev]);
      setActiveItemId(newItem.id);
    } catch (err: any) {
      console.error("Text conversion error:", err);
      setErrorMessage(err.message || "Matnni konvertatsiya qilishda xatolik yuz berdi.");
    } finally {
      setIsConverting(false);
      setConversionStatusMsg("");
    }
  };

  // Convert Sample
  const handleSelectSample = async (sample: SampleFile) => {
    await handleConvertText(sample.content, sample.format.toLowerCase(), sample.name);
  };

  const handleUpdateMarkdown = (id: string, newMarkdown: string) => {
    setConvertedItems((prev) =>
      prev.map((item) => {
        if (item.id === id) {
          const lines = newMarkdown.split("\n").length;
          const words = newMarkdown.trim().split(/\s+/).filter(Boolean).length;
          return {
            ...item,
            markdown: newMarkdown,
            markdownSize: new Blob([newMarkdown]).size,
            lineCount: lines,
            wordCount: words,
          };
        }
        return item;
      })
    );
  };

  const handleDeleteItem = (id: string) => {
    setConvertedItems((prev) => {
      const updated = prev.filter((item) => item.id !== id);
      if (activeItemId === id && updated.length > 0) {
        setActiveItemId(updated[0].id);
      }
      return updated;
    });
  };

  const handleClearAllItems = () => {
    setConvertedItems([]);
    setActiveItemId("");
  };

  return (
    <div className="min-h-screen bg-zinc-100/70 text-zinc-900 flex flex-col font-sans selection:bg-indigo-600 selection:text-white">
      {/* Header */}
      <Header
        onOpenApiDocs={() => setIsApiDocsOpen(true)}
        onOpenFormats={() => setIsFormatsOpen(true)}
        onOpenApiKey={() => setIsApiKeyModalOpen(true)}
        hasApiKey={Boolean(geminiApiKey)}
      />

      {/* Main Container */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6 sm:py-8 space-y-6">
        {/* Error Alert Banner */}
        {errorMessage && (
          <div className="flex items-center justify-between gap-3 p-4 rounded-xl bg-red-50 border border-red-200 text-red-800 text-xs">
            <div className="flex items-center gap-2">
              <AlertCircle className="w-4 h-4 text-red-600 shrink-0" />
              <span className="font-medium">{errorMessage}</span>
            </div>
            <button
              onClick={() => setErrorMessage(null)}
              className="text-red-500 hover:text-red-700 font-semibold cursor-pointer"
            >
              Yopish
            </button>
          </div>
        )}

        {/* Global Conversion Settings Bar */}
        <ConversionSettings options={options} onChange={setOptions} />

        {/* Conversion Input Section (Tabs) */}
        <div className="space-y-4">
          {/* Tab Navigation */}
          <div className="flex items-center space-x-2 border-b border-zinc-200/80 pb-2">
            <button
              onClick={() => setActiveTab("upload")}
              id="tab-btn-upload"
              className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-semibold transition-all cursor-pointer ${
                activeTab === "upload"
                  ? "bg-zinc-900 text-white shadow-xs"
                  : "text-zinc-600 hover:text-zinc-900 hover:bg-zinc-200/60"
              }`}
            >
              <UploadCloud className="w-3.5 h-3.5" />
              <span>Fayl / Rasm / Audio Yuklash</span>
              {selectedFiles.length > 0 && (
                <span className="ml-1 px-1.5 py-0.2 rounded-full bg-indigo-500 text-white text-[10px]">
                  {selectedFiles.length}
                </span>
              )}
            </button>

            <button
              onClick={() => setActiveTab("url")}
              id="tab-btn-url"
              className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-semibold transition-all cursor-pointer ${
                activeTab === "url"
                  ? "bg-zinc-900 text-white shadow-xs"
                  : "text-zinc-600 hover:text-zinc-900 hover:bg-zinc-200/60"
              }`}
            >
              <Globe className="w-3.5 h-3.5" />
              <span>Web Havola (URL)</span>
            </button>

            <button
              onClick={() => setActiveTab("text")}
              id="tab-btn-text"
              className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-semibold transition-all cursor-pointer ${
                activeTab === "text"
                  ? "bg-zinc-900 text-white shadow-xs"
                  : "text-zinc-600 hover:text-zinc-900 hover:bg-zinc-200/60"
              }`}
            >
              <Code className="w-3.5 h-3.5" />
              <span>Xom Matn / Kod / Jadval</span>
            </button>
          </div>

          {/* Tab Panes */}
          {activeTab === "upload" && (
            <DropZone
              onFilesSelected={handleFilesSelected}
              isConverting={isConverting}
              selectedFiles={selectedFiles}
              onRemoveFile={handleRemoveFile}
              onClearAll={handleClearAllFiles}
              onConvert={handleConvertFiles}
              options={options}
            />
          )}

          {activeTab === "url" && (
            <UrlConverter
              onConvertUrl={handleConvertUrl}
              isConverting={isConverting}
              options={options}
            />
          )}

          {activeTab === "text" && (
            <TextConverter
              onConvertText={handleConvertText}
              isConverting={isConverting}
              options={options}
            />
          )}
        </div>

        {/* Quick Sample Selector */}
        <SamplePicker onSelectSample={handleSelectSample} disabled={isConverting} />

        {/* Loading Indicator */}
        {isConverting && (
          <div className="bg-indigo-50/80 border border-indigo-200 rounded-xl p-4 flex items-center justify-center space-x-3 text-xs font-medium text-indigo-900 shadow-xs">
            <Loader2 className="w-4 h-4 animate-spin text-indigo-600" />
            <span>{conversionStatusMsg || "Fayl Markdown (.md) formatiga aylantirilmoqda..."}</span>
          </div>
        )}

        {/* Converted Markdown Viewer & Workspace */}
        {convertedItems.length > 0 && (
          <div className="space-y-3 pt-2">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <FileCheck2 className="w-4 h-4 text-emerald-600" />
                <h2 className="text-sm font-bold text-zinc-900">
                  Aylantirilgan Markdown Hujjatlar ({convertedItems.length})
                </h2>
              </div>
              <span className="text-[11px] text-zinc-500">.md faylni to'g'ridan-to'g'ri yuklab olishingiz mumkin</span>
            </div>

            <MarkdownViewer
              items={convertedItems}
              activeId={activeItemId}
              onSelectActive={setActiveItemId}
              onUpdateMarkdown={handleUpdateMarkdown}
              onDeleteItem={handleDeleteItem}
              onClearAll={handleClearAllItems}
            />
          </div>
        )}
      </main>

      {/* Footer */}
      <footer className="border-t border-zinc-200 bg-white py-6 mt-12 text-center text-xs text-zinc-500">
        <div className="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-2">
          <p>
            MarkItDown Studio &copy; {new Date().getFullYear()} • Universal Fayl va Matn Konvertatsiya Xizmati.
          </p>
          <div className="flex items-center space-x-4 text-zinc-400">
            <span>100% Toza Markdown (.md)</span>
            <span>•</span>
            <span>Lokal va Brauzerda Ishlaydi</span>
            <span>•</span>
            <span>0 Server Ehtiyoji</span>
          </div>
        </div>
      </footer>

      {/* Modals */}
      <ApiKeyModal
        isOpen={isApiKeyModalOpen}
        onClose={() => setIsApiKeyModalOpen(false)}
        onSaveApiKey={handleSaveApiKey}
        currentKey={geminiApiKey}
      />
      <ApiDocsModal isOpen={isApiDocsOpen} onClose={() => setIsApiDocsOpen(false)} />
      <SupportedFormatsModal isOpen={isFormatsOpen} onClose={() => setIsFormatsOpen(false)} />
    </div>
  );
}
