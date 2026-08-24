import React, { useRef, useState } from "react";
import { UploadCloud, File, X, Loader2, Sparkles, FileSpreadsheet, FileCode2, Music, Image as ImageIcon } from "lucide-react";
import { ConversionOptions } from "../types";

interface DropZoneProps {
  onFilesSelected: (files: File[]) => void;
  isConverting: boolean;
  selectedFiles: File[];
  onRemoveFile: (index: number) => void;
  onClearAll: () => void;
  onConvert: () => void;
  options: ConversionOptions;
}

export const DropZone: React.FC<DropZoneProps> = ({
  onFilesSelected,
  isConverting,
  selectedFiles,
  onRemoveFile,
  onClearAll,
  onConvert,
  options,
}) => {
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

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
    if (["xlsx", "xls", "csv", "tsv"].includes(ext || "")) return <FileSpreadsheet className="w-4 h-4 text-emerald-600" />;
    if (["jpg", "jpeg", "png", "webp", "gif", "svg"].includes(ext || "")) return <ImageIcon className="w-4 h-4 text-purple-600" />;
    if (["mp3", "wav", "m4a", "ogg"].includes(ext || "")) return <Music className="w-4 h-4 text-amber-600" />;
    if (["py", "js", "ts", "json", "html", "css", "sql"].includes(ext || "")) return <FileCode2 className="w-4 h-4 text-blue-600" />;
    return <File className="w-4 h-4 text-indigo-600" />;
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
        className={`relative border-2 border-dashed rounded-2xl p-6 sm:p-8 text-center transition-all cursor-pointer ${
          isDragOver
            ? "border-indigo-500 bg-indigo-50/50 scale-[0.99]"
            : "border-zinc-300 hover:border-indigo-400 bg-white hover:bg-zinc-50/40"
        } shadow-sm`}
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
          <div className="w-14 h-14 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center shadow-inner border border-indigo-100">
            <UploadCloud className="w-7 h-7" />
          </div>

          <div>
            <p className="text-sm font-semibold text-zinc-800">
              Fayllarni shu yerga tashlang yoki <span className="text-indigo-600 underline">qurilmadan tanlang</span>
            </p>
            <p className="text-xs text-zinc-500 mt-1 max-w-md mx-auto">
              PDF, Word, Excel, PowerPoint, CSV, JSON, HTML, Rasmlar (OCR matn ajratish), Ovozli audio fayllar (nutqdan matn) va Kod fayllarini qo'llab-quvvatlaydi
            </p>
          </div>

          <div className="flex flex-wrap items-center justify-center gap-1.5 pt-1">
            {["PDF", "Word (.docx)", "Excel (.xlsx)", "PowerPoint", "CSV/TSV", "JSON", "Rasmlar (OCR)", "Audio Ovoz (Nutq)", "ZIP"].map((fmt) => (
              <span key={fmt} className="text-[10px] font-medium px-2 py-0.5 rounded bg-zinc-100 text-zinc-600 border border-zinc-200/60">
                {fmt}
              </span>
            ))}
          </div>
        </div>
      </div>

      {/* Selected Files Queue */}
      {selectedFiles.length > 0 && (
        <div className="bg-white border border-zinc-200 rounded-xl p-4 shadow-sm space-y-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="text-xs font-semibold text-zinc-900">
                Tanlangan fayllar ({selectedFiles.length})
              </span>
              <span className="text-xs text-zinc-500">
                Jami: {formatFileSize(selectedFiles.reduce((acc, f) => acc + f.size, 0))}
              </span>
            </div>

            <button
              onClick={(e) => {
                e.stopPropagation();
                onClearAll();
              }}
              id="btn-clear-all-files"
              className="text-xs text-zinc-500 hover:text-red-600 transition-colors font-medium"
            >
              Barchasini tozalash
            </button>
          </div>

          <div className="max-h-48 overflow-y-auto divide-y divide-zinc-100 border border-zinc-100 rounded-lg">
            {selectedFiles.map((file, idx) => (
              <div key={`${file.name}-${idx}`} className="flex items-center justify-between p-2.5 hover:bg-zinc-50/80 text-xs">
                <div className="flex items-center space-x-2.5 min-w-0 pr-3">
                  {getFormatIcon(file.name)}
                  <span className="font-medium text-zinc-800 truncate max-w-xs">{file.name}</span>
                  <span className="text-[11px] text-zinc-400 shrink-0">({formatFileSize(file.size)})</span>
                </div>

                <button
                  onClick={() => onRemoveFile(idx)}
                  id={`btn-remove-file-${idx}`}
                  className="text-zinc-400 hover:text-red-500 p-1 rounded hover:bg-red-50 transition-colors"
                  title="Faylni o'chirish"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              </div>
            ))}
          </div>

          <div className="pt-2 flex flex-col sm:flex-row items-center justify-between gap-3">
            <span className="text-xs text-zinc-500">
              {selectedFiles.length} ta fayl Markdown (.md) formatiga o'tkazishga tayyor
            </span>

            <button
              onClick={onConvert}
              disabled={isConverting}
              id="btn-start-convert-files"
              className="w-full sm:w-auto px-6 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold rounded-xl shadow-md shadow-indigo-600/20 flex items-center justify-center space-x-2 transition-all disabled:opacity-50"
            >
              {isConverting ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>Markdown ga aylantirilmoqda...</span>
                </>
              ) : (
                <>
                  <Sparkles className="w-4 h-4" />
                  <span>Matnni Markdown (.md) ga o'tkazish</span>
                </>
              )}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
