import React, { useRef, useState } from "react";
import { UploadCloud, File, X, Loader2, Sparkles, FileSpreadsheet, FileCode2, Music, Image as ImageIcon } from "lucide-react";
import { Language, TRANSLATIONS } from "../locales/i18n";
import { ThemeType } from "../types";

interface DropZoneProps {
  onFilesSelected: (files: File[]) => void;
  isConverting: boolean;
  selectedFiles: File[];
  onRemoveFile: (index: number) => void;
  onClearFiles: () => void;
  onConvert: () => void;
  language?: Language;
  theme?: ThemeType;
}

export const DropZone: React.FC<DropZoneProps> = ({
  onFilesSelected,
  isConverting,
  selectedFiles,
  onRemoveFile,
  onClearFiles,
  onConvert,
  language = "uz",
  theme = "MidnightGlass",
}) => {
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const t = TRANSLATIONS[language];

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      onFilesSelected(Array.from(e.dataTransfer.files));
    }
  };

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      onFilesSelected(Array.from(e.target.files));
    }
  };

  const getFormatIcon = (filename: string) => {
    const ext = filename.split(".").pop()?.toLowerCase();
    if (["xlsx", "xls", "csv", "tsv"].includes(ext || "")) return <FileSpreadsheet className="w-4 h-4 text-emerald-400" />;
    if (["jpg", "jpeg", "png", "webp", "gif", "svg"].includes(ext || "")) return <ImageIcon className="w-4 h-4 text-purple-400" />;
    if (["mp3", "wav", "m4a", "ogg"].includes(ext || "")) return <Music className="w-4 h-4 text-amber-400" />;
    if (["py", "js", "ts", "json", "html", "css", "sql"].includes(ext || "")) return <FileCode2 className="w-4 h-4 text-blue-400" />;
    return <File className="w-4 h-4 text-indigo-400" />;
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  };

  return (
    <div className="space-y-4">
      {/* Drag & Drop Area */}
      <div
        id="dropzone-container"
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        className={`relative border-2 border-dashed rounded-2xl p-6 sm:p-8 text-center transition-all cursor-pointer shadow-sm ${
          isDragOver
            ? "border-indigo-500 bg-indigo-950/40 scale-[0.99]"
            : theme === "FrostedCrystal"
            ? "border-slate-300 hover:border-indigo-500 bg-white hover:bg-slate-50 text-slate-900"
            : "border-zinc-800 hover:border-indigo-500 bg-zinc-900/60 hover:bg-zinc-900 text-zinc-100"
        }`}
      >
        <input
          ref={fileInputRef}
          type="file"
          id="file-input-hidden"
          multiple
          onChange={handleFileInputChange}
          className="hidden"
          accept=".pdf,.docx,.doc,.pptx,.ppt,.xlsx,.xls,.csv,.tsv,.html,.htm,.json,.xml,.yaml,.yml,.txt,.log,.py,.js,.ts,.java,.cpp,.c,.sql,.png,.jpg,.jpeg,.webp,.svg,.mp3,.wav,.m4a,.ogg,.zip"
        />

        <div className="flex flex-col items-center justify-center space-y-3">
          <div className="w-14 h-14 rounded-2xl bg-indigo-950/80 text-indigo-400 flex items-center justify-center shadow-inner border border-indigo-500/40">
            <UploadCloud className="w-7 h-7" />
          </div>

          <div>
            <p className="text-sm font-bold">
              {t.dropTitle} yoki <span className="text-indigo-400 underline">{t.selectFile}</span>
            </p>
            <p className="text-xs text-zinc-400 mt-1 max-w-md mx-auto">
              {t.dropSubtitle}
            </p>
          </div>

          <div className="flex flex-wrap items-center justify-center gap-1.5 pt-1">
            {["PDF", "Word (.docx)", "Excel (.xlsx)", "PowerPoint", "CSV/TSV", "JSON", "Rasmlar (OCR)", "Audio Ovoz", "ZIP"].map((fmt) => (
              <span key={fmt} className="text-[10px] font-medium px-2 py-0.5 rounded bg-zinc-800 text-zinc-300 border border-zinc-700/60">
                {fmt}
              </span>
            ))}
          </div>
        </div>
      </div>

      {/* Selected Files Queue */}
      {selectedFiles.length > 0 && (
        <div
          className={`border rounded-xl p-4 shadow-sm space-y-3 ${
            theme === "FrostedCrystal"
              ? "bg-white border-slate-200 text-slate-900"
              : "bg-zinc-900 border-zinc-800 text-zinc-100"
          }`}
        >
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="text-xs font-semibold">
                Tanlangan ({selectedFiles.length})
              </span>
              <span className="text-xs text-zinc-400">
                Jami: {formatFileSize(selectedFiles.reduce((acc, f) => acc + f.size, 0))}
              </span>
            </div>

            <button
              onClick={(e) => {
                e.stopPropagation();
                onClearFiles();
              }}
              className="text-xs text-zinc-400 hover:text-red-400 transition-colors font-medium cursor-pointer"
            >
              {t.clearAll}
            </button>
          </div>

          <div className="max-h-48 overflow-y-auto divide-y divide-zinc-800/60 border border-zinc-800/80 rounded-lg">
            {selectedFiles.map((file, idx) => (
              <div key={`${file.name}-${idx}`} className="flex items-center justify-between p-2.5 hover:bg-zinc-800/50 text-xs">
                <div className="flex items-center space-x-2.5 min-w-0 pr-3">
                  {getFormatIcon(file.name)}
                  <span className="font-medium truncate max-w-xs">{file.name}</span>
                  <span className="text-[11px] text-zinc-500 shrink-0">({formatFileSize(file.size)})</span>
                </div>

                <button
                  onClick={() => onRemoveFile(idx)}
                  className="text-zinc-400 hover:text-red-400 p-1 rounded hover:bg-red-950/40 transition-colors cursor-pointer"
                  title="Faylni o'chirish"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              </div>
            ))}
          </div>

          <div className="pt-2 flex flex-col sm:flex-row items-center justify-between gap-3">
            <span className="text-xs text-zinc-400">
              {selectedFiles.length} ta fayl .md formatiga o'tkazishga tayyor
            </span>

            <button
              onClick={onConvert}
              disabled={isConverting}
              className="w-full sm:w-auto px-6 py-2.5 bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-bold rounded-xl shadow-md shadow-indigo-600/20 flex items-center justify-center space-x-2 transition-all disabled:opacity-50 cursor-pointer"
            >
              {isConverting ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>{t.convertingProgress}...</span>
                </>
              ) : (
                <>
                  <Sparkles className="w-4 h-4" />
                  <span>Matnni Markdown (.md) ga O'tkazish</span>
                </>
              )}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
