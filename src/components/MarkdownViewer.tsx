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
  X,
  Wand2,
} from "lucide-react";
import { ConvertedItem, ThemeType } from "../types";

interface MarkdownViewerProps {
  item: ConvertedItem | undefined;
  items: ConvertedItem[];
  activeItemId: string;
  onSelectItem: (id: string) => void;
  onClearHistory: () => void;
  onDeleteItem: (id: string) => void;
  onAiProofread?: () => void;
  theme?: ThemeType;
}

export const MarkdownViewer: React.FC<MarkdownViewerProps> = ({
  item,
  items,
  activeItemId,
  onSelectItem,
  onClearHistory,
  onDeleteItem,
  onAiProofread,
  theme = "MidnightGlass",
}) => {
  const [viewMode, setViewMode] = useState<"split" | "preview" | "editor">("split");
  const [copied, setCopied] = useState(false);

  const activeItem = item || items.find((i) => i.id === activeItemId) || items[0];

  if (!activeItem) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center p-12 text-center rounded-2xl border border-zinc-800 bg-zinc-900/50 min-h-[400px]">
        <div className="w-14 h-14 rounded-2xl bg-zinc-800 flex items-center justify-center text-zinc-500 mb-3">
          <Sparkles className="w-7 h-7" />
        </div>
        <h3 className="text-base font-bold text-white mb-1">Markdown Ko'ruvchi Bo'sh</h3>
        <p className="text-xs text-zinc-400 max-w-sm">
          Chap tomondan fayl yuklang yoki web URL kiriting. Natija bu yerda toza Markdown formatida paydo bo'ladi.
        </p>
      </div>
    );
  }

  const handleCopy = () => {
    navigator.clipboard.writeText(activeItem.markdown);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
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
    for (const it of items) {
      const safeName = it.filename.replace(/\.[^/.]+$/, "");
      zip.file(`${safeName}.md`, it.markdown);
    }
    const content = await zip.generateAsync({ type: "blob" });
    const url = URL.createObjectURL(content);
    const a = document.createElement("a");
    a.href = url;
    a.download = `markdown_hujjatlar_${new Date().toISOString().slice(0, 10)}.zip`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div
      className={`border rounded-2xl shadow-sm overflow-hidden flex flex-col transition-all ${
        theme === "ObsidianDark"
          ? "bg-[#1e1e1e] border-zinc-800 text-zinc-100"
          : theme === "CyberpunkNeon"
          ? "bg-[#0a0f1e] border-cyan-500/40 text-cyan-50"
          : theme === "FrostedCrystal"
          ? "bg-white border-slate-200 text-slate-900"
          : "bg-zinc-900 border-zinc-800 text-zinc-100"
      }`}
    >
      {/* File Navigation Tabs */}
      <div className="bg-zinc-950/60 border-b border-zinc-800/80 px-3 py-2 flex items-center justify-between overflow-x-auto gap-2">
        <div className="flex items-center space-x-1.5 overflow-x-auto py-0.5">
          {items.map((it) => {
            const isActive = it.id === activeItem.id;
            return (
              <div
                key={it.id}
                className={`flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium transition-all ${
                  isActive
                    ? "bg-indigo-600 text-white shadow-xs font-semibold"
                    : "bg-zinc-800/80 text-zinc-400 hover:text-zinc-200 hover:bg-zinc-800"
                }`}
              >
                <button
                  onClick={() => onSelectItem(it.id)}
                  className="truncate max-w-[130px] cursor-pointer text-left"
                >
                  {it.filename}
                </button>
                <span className="text-[9px] px-1 py-0.2 rounded bg-black/30 font-mono opacity-80">
                  {it.originalFormat}
                </span>
                {/* Single item delete button */}
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    onDeleteItem(it.id);
                  }}
                  className="p-0.5 rounded hover:bg-red-500/40 text-zinc-300 hover:text-red-200 transition-colors ml-0.5"
                  title="Ushbu faylni o'chirish"
                >
                  <X className="w-3 h-3" />
                </button>
              </div>
            );
          })}
        </div>

        <div className="flex items-center space-x-2 shrink-0">
          {items.length > 1 && (
            <button
              onClick={handleDownloadAllZip}
              className="flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-indigo-300 bg-indigo-950/80 hover:bg-indigo-900 rounded-lg border border-indigo-500/40 transition-colors cursor-pointer"
            >
              <FileDown className="w-3.5 h-3.5" />
              <span className="hidden sm:inline">ZIP qilib yuklash</span>
            </button>
          )}

          <button
            onClick={onClearHistory}
            className="flex items-center gap-1 text-zinc-400 hover:text-red-400 px-2 py-1 text-xs rounded-lg hover:bg-red-950/40 border border-transparent hover:border-red-500/30 transition-colors cursor-pointer"
            title="Barcha tarixni tozalash"
          >
            <Trash2 className="w-3.5 h-3.5" />
            <span className="hidden sm:inline">Tozalash</span>
          </button>
        </div>
      </div>

      {/* Action Toolbar */}
      <div className="p-3 border-b border-zinc-800/80 flex flex-wrap items-center justify-between gap-3 bg-zinc-900/40">
        {/* Left: View Mode Switches */}
        <div className="flex items-center bg-zinc-800/80 p-0.5 rounded-xl border border-zinc-700/60 text-xs">
          <button
            onClick={() => setViewMode("split")}
            className={`flex items-center gap-1.5 px-3 py-1 rounded-lg font-medium transition-colors cursor-pointer ${
              viewMode === "split" ? "bg-indigo-600 text-white shadow-xs" : "text-zinc-400 hover:text-zinc-200"
            }`}
          >
            <Columns className="w-3.5 h-3.5" />
            <span className="hidden sm:inline">Split</span>
          </button>

          <button
            onClick={() => setViewMode("preview")}
            className={`flex items-center gap-1.5 px-3 py-1 rounded-lg font-medium transition-colors cursor-pointer ${
              viewMode === "preview" ? "bg-indigo-600 text-white shadow-xs" : "text-zinc-400 hover:text-zinc-200"
            }`}
          >
            <Eye className="w-3.5 h-3.5" />
            <span>Preview</span>
          </button>

          <button
            onClick={() => setViewMode("editor")}
            className={`flex items-center gap-1.5 px-3 py-1 rounded-lg font-medium transition-colors cursor-pointer ${
              viewMode === "editor" ? "bg-indigo-600 text-white shadow-xs" : "text-zinc-400 hover:text-zinc-200"
            }`}
          >
            <Edit3 className="w-3.5 h-3.5" />
            <span>Editor</span>
          </button>
        </div>

        {/* Right: Actions */}
        <div className="flex items-center space-x-2">
          {onAiProofread && (
            <button
              onClick={onAiProofread}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-indigo-300 bg-indigo-950/80 hover:bg-indigo-900 rounded-xl border border-indigo-500/50 transition-all cursor-pointer shadow-xs"
              title="Grammatika, jadvallar va shrift xatoliklarini AI orqali tekshirish"
            >
              <Sparkles className="w-3.5 h-3.5 text-indigo-400" />
              <span>AI Bilan Tekshirish</span>
            </button>
          )}

          <button
            onClick={handleCopy}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-zinc-300 bg-zinc-800 hover:bg-zinc-700 rounded-xl transition-colors border border-zinc-700 cursor-pointer"
          >
            {copied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
            <span>{copied ? "Nusxalandi!" : "Nusxa olish"}</span>
          </button>

          <button
            onClick={handleDownloadSingle}
            className="flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-bold text-white bg-indigo-600 hover:bg-indigo-500 rounded-xl shadow-xs transition-colors cursor-pointer"
          >
            <Download className="w-3.5 h-3.5" />
            <span>.md Saqlash</span>
          </button>
        </div>
      </div>

      {/* Info Stats Bar */}
      <div className="px-4 py-1.5 bg-zinc-950/40 border-b border-zinc-800/60 flex flex-wrap items-center justify-between text-[11px] text-zinc-400 gap-2">
        <div className="flex items-center space-x-2">
          <span className="font-mono text-zinc-200 font-semibold">{activeItem.filename}</span>
          <span>•</span>
          <span>{activeItem.wordCount.toLocaleString()} ta so'z</span>
          <span>•</span>
          <span>{activeItem.charCount.toLocaleString()} ta belgi</span>
        </div>
        <div className="flex items-center space-x-2 text-zinc-500">
          <Clock className="w-3 h-3" />
          <span>{activeItem.durationMs} ms</span>
          <span>•</span>
          <span>{activeItem.engine || "Local"}</span>
        </div>
      </div>

      {/* Viewer Content Area */}
      <div className="flex-1 min-h-[450px] max-h-[620px] overflow-hidden flex">
        {/* Editor Pane */}
        {(viewMode === "split" || viewMode === "editor") && (
          <div className={`flex-1 p-4 overflow-y-auto ${viewMode === "split" ? "border-r border-zinc-800" : ""}`}>
            <textarea
              value={activeItem.markdown}
              readOnly
              className="w-full h-full bg-transparent font-mono text-xs text-zinc-200 resize-none focus:outline-none leading-relaxed"
            />
          </div>
        )}

        {/* Rendered Preview Pane */}
        {(viewMode === "split" || viewMode === "preview") && (
          <div className="flex-1 p-5 overflow-y-auto prose prose-invert prose-zinc max-w-none text-xs leading-relaxed">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {activeItem.markdown}
            </ReactMarkdown>
          </div>
        )}
      </div>
    </div>
  );
};
