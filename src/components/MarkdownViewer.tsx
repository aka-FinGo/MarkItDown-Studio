import React, { useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import JSZip from "jszip";
import {
  Copy,
  Check,
  Download,
  FileDown,
  Columns,
  Eye,
  Edit3,
  Sparkles,
  Clock,
  Trash2,
} from "lucide-react";
import { ConvertedItem } from "../types";

interface MarkdownViewerProps {
  items: ConvertedItem[];
  activeId: string;
  onSelectActive: (id: string) => void;
  onUpdateMarkdown: (id: string, newMarkdown: string) => void;
  onDeleteItem: (id: string) => void;
  onClearAll: () => void;
}

export const MarkdownViewer: React.FC<MarkdownViewerProps> = ({
  items,
  activeId,
  onSelectActive,
  onUpdateMarkdown,
  onDeleteItem,
  onClearAll,
}) => {
  const [viewMode, setViewMode] = useState<"split" | "preview" | "editor" | "stats">("split");
  const [copiedType, setCopiedType] = useState<string | null>(null);

  const activeItem = items.find((item) => item.id === activeId) || items[0];

  if (!activeItem) return null;

  const handleCopy = (type: "raw" | "llm") => {
    let contentToCopy = activeItem.markdown;
    if (type === "llm") {
      contentToCopy = `<document filename="${activeItem.filename}" format="${activeItem.originalFormat}">\n${activeItem.markdown}\n</document>`;
    }

    navigator.clipboard.writeText(contentToCopy);
    setCopiedType(type);
    setTimeout(() => setCopiedType(null), 2000);
  };

  const handleDownloadSingle = () => {
    const blob = new Blob([activeItem.markdown], { type: "text/markdown;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    const safeName = activeItem.filename.replace(/\.[^/.]+$/, "");
    a.href = url;
    a.download = `${safeName}.md`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleDownloadAllZip = async () => {
    const zip = new JSZip();
    for (const item of items) {
      const safeName = item.filename.replace(/\.[^/.]+$/, "");
      zip.file(`${safeName}.md`, item.markdown);
    }
    const content = await zip.generateAsync({ type: "blob" });
    const url = URL.createObjectURL(content);
    const a = document.createElement("a");
    a.href = url;
    a.download = `markdown_hujjatlar_${new Date().toISOString().slice(0, 10)}.zip`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  };

  return (
    <div className="bg-white border border-zinc-200 rounded-2xl shadow-sm overflow-hidden flex flex-col">
      {/* File Navigation Tabs */}
      <div className="bg-zinc-50 border-b border-zinc-200 px-4 py-2.5 flex items-center justify-between overflow-x-auto gap-2">
        <div className="flex items-center space-x-1 overflow-x-auto py-1">
          {items.map((item) => {
            const isActive = item.id === activeItem.id;
            return (
              <button
                key={item.id}
                onClick={() => onSelectActive(item.id)}
                id={`tab-file-${item.id}`}
                className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-medium transition-all whitespace-nowrap ${
                  isActive
                    ? "bg-white text-zinc-900 shadow-sm border border-zinc-200 font-semibold"
                    : "text-zinc-500 hover:text-zinc-800 hover:bg-zinc-100"
                }`}
              >
                <span className="truncate max-w-[150px]">{item.filename}</span>
                <span className="text-[10px] px-1.5 py-0.2 rounded bg-zinc-100 text-zinc-600 font-mono">
                  {item.originalFormat}
                </span>
                {item.usedAi && <Sparkles className="w-3 h-3 text-indigo-500" />}
              </button>
            );
          })}
        </div>

        <div className="flex items-center space-x-2 shrink-0">
          {items.length > 1 && (
            <button
              onClick={handleDownloadAllZip}
              id="btn-download-all-zip"
              className="flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-indigo-700 bg-indigo-50 hover:bg-indigo-100 rounded-lg border border-indigo-200/60 transition-colors"
            >
              <FileDown className="w-3.5 h-3.5" />
              <span>Barchasini ZIP qilib yuklash</span>
            </button>
          )}

          <button
            onClick={onClearAll}
            id="btn-clear-viewer-items"
            className="text-zinc-400 hover:text-red-600 p-1.5 rounded-lg hover:bg-red-50 transition-colors"
            title="Barchasini o'chirish"
          >
            <Trash2 className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      {/* Action Toolbar */}
      <div className="p-3 sm:px-4 border-b border-zinc-200 bg-white flex flex-wrap items-center justify-between gap-3">
        {/* Left: View Mode Switches */}
        <div className="flex items-center bg-zinc-100 p-1 rounded-xl border border-zinc-200/80 text-xs">
          <button
            onClick={() => setViewMode("split")}
            id="btn-view-split"
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg font-medium transition-colors ${
              viewMode === "split" ? "bg-white text-zinc-900 shadow-xs font-semibold" : "text-zinc-600 hover:text-zinc-900"
            }`}
          >
            <Columns className="w-3.5 h-3.5" />
            <span className="hidden sm:inline">Yonma-yon (Split)</span>
          </button>

          <button
            onClick={() => setViewMode("preview")}
            id="btn-view-preview"
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg font-medium transition-colors ${
              viewMode === "preview" ? "bg-white text-zinc-900 shadow-xs font-semibold" : "text-zinc-600 hover:text-zinc-900"
            }`}
          >
            <Eye className="w-3.5 h-3.5" />
            <span>Ko'rinish (Preview)</span>
          </button>

          <button
            onClick={() => setViewMode("editor")}
            id="btn-view-editor"
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg font-medium transition-colors ${
              viewMode === "editor" ? "bg-white text-zinc-900 shadow-xs font-semibold" : "text-zinc-600 hover:text-zinc-900"
            }`}
          >
            <Edit3 className="w-3.5 h-3.5" />
            <span>Matn Tahrirlagich</span>
          </button>
        </div>

        {/* Right: Actions (Download .md, Copy) */}
        <div className="flex items-center space-x-2">
          <button
            onClick={() => handleCopy("raw")}
            id="btn-copy-raw-markdown"
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-zinc-700 bg-zinc-100 hover:bg-zinc-200/80 rounded-lg transition-colors border border-zinc-200/80"
          >
            {copiedType === "raw" ? <Check className="w-3.5 h-3.5 text-emerald-600" /> : <Copy className="w-3.5 h-3.5" />}
            <span>{copiedType === "raw" ? "Nusxalandi!" : "Nusxa olish"}</span>
          </button>

          <button
            onClick={handleDownloadSingle}
            id="btn-download-md-file"
            className="flex items-center gap-1.5 px-4 py-1.5 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm shadow-indigo-600/20 transition-colors"
          >
            <Download className="w-3.5 h-3.5" />
            <span>.md Faylni Yuklab Olish</span>
          </button>
        </div>
      </div>

      {/* Info Stats Bar */}
      <div className="px-4 py-2 bg-zinc-50/50 border-b border-zinc-100 flex flex-wrap items-center justify-between text-xs text-zinc-500 gap-2">
        <div className="flex items-center space-x-3">
          <span className="font-mono text-zinc-700 font-semibold">{activeItem.filename}</span>
          <span>•</span>
          <span>{activeItem.wordCount.toLocaleString()} ta so'z</span>
          <span>•</span>
          <span>{activeItem.lineCount} qator</span>
          <span>•</span>
          <span>{formatFileSize(activeItem.markdownSize)}</span>
        </div>

        <div className="flex items-center space-x-2">
          {activeItem.usedAi ? (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-indigo-50 text-indigo-700 font-medium text-[11px] border border-indigo-200">
              <Sparkles className="w-3 h-3 text-indigo-500" />
              AI Multimodal (OCR / Audio)
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 font-medium text-[11px] border border-emerald-200">
              <Check className="w-3 h-3 text-emerald-600" />
              0 AI Token (Lokal)
            </span>
          )}

          <span className="inline-flex items-center gap-1 text-[11px] text-zinc-400 font-mono">
            <Clock className="w-3 h-3" />
            {activeItem.durationMs}ms
          </span>
        </div>
      </div>

      {/* Main Workspace Body */}
      <div className="flex-1 min-h-[480px]">
        {/* Split View */}
        {viewMode === "split" && (
          <div className="grid grid-cols-1 md:grid-cols-2 divide-y md:divide-y-0 md:divide-x divide-zinc-200 min-h-[480px]">
            {/* Raw Editor */}
            <div className="flex flex-col bg-zinc-900 text-zinc-100">
              <div className="px-4 py-2 border-b border-zinc-800 flex items-center justify-between bg-zinc-950/40 text-[11px] text-zinc-400">
                <span className="font-mono">Xom Markdown Tahrirlagich (.md)</span>
                <span>O'zgartirishlar avtomatik saqlanadi</span>
              </div>
              <textarea
                id="textarea-active-markdown-split"
                value={activeItem.markdown}
                onChange={(e) => onUpdateMarkdown(activeItem.id, e.target.value)}
                className="flex-1 p-4 font-mono text-xs bg-transparent text-zinc-100 focus:outline-none resize-none leading-relaxed selection:bg-indigo-500 selection:text-white"
                rows={22}
                placeholder="Markdown matni shu yerda bo'ladi..."
              />
            </div>

            {/* Rendered Preview */}
            <div className="flex flex-col bg-white overflow-y-auto max-h-[600px]">
              <div className="px-4 py-2 border-b border-zinc-100 flex items-center justify-between bg-zinc-50/60 text-[11px] text-zinc-500">
                <span>Jonli Ko'rinish (Formatted Preview)</span>
                <span className="text-zinc-400 font-mono">GitHub Flavored Markdown</span>
              </div>
              <div className="p-6 overflow-y-auto">
                <div className="markdown-body">
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>
                    {activeItem.markdown}
                  </ReactMarkdown>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Preview Only */}
        {viewMode === "preview" && (
          <div className="p-6 sm:p-8 bg-white min-h-[480px] max-h-[700px] overflow-y-auto">
            <div className="max-w-4xl mx-auto markdown-body">
              <ReactMarkdown remarkPlugins={[remarkGfm]}>
                {activeItem.markdown}
              </ReactMarkdown>
            </div>
          </div>
        )}

        {/* Editor Only */}
        {viewMode === "editor" && (
          <div className="flex flex-col bg-zinc-900 text-zinc-100 min-h-[480px]">
            <div className="px-4 py-2 border-b border-zinc-800 flex items-center justify-between bg-zinc-950/40 text-xs text-zinc-400">
              <span className="font-mono">{activeItem.filename} — Markdown Matni</span>
              <button
                onClick={handleDownloadSingle}
                className="text-indigo-400 hover:text-indigo-300 font-medium"
              >
                .md Faylni Yuklab Olish
              </button>
            </div>
            <textarea
              id="textarea-active-markdown-full"
              value={activeItem.markdown}
              onChange={(e) => onUpdateMarkdown(activeItem.id, e.target.value)}
              className="flex-1 p-6 font-mono text-xs bg-transparent text-zinc-100 focus:outline-none resize-none leading-relaxed selection:bg-indigo-500 selection:text-white min-h-[500px]"
            />
          </div>
        )}
      </div>
    </div>
  );
};
