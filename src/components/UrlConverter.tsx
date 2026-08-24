import React, { useState } from "react";
import { Globe, ArrowRight, Loader2, Sparkles, AlertCircle } from "lucide-react";
import { ConversionOptions } from "../types";

interface UrlConverterProps {
  onConvertUrl: (url: string) => Promise<void>;
  isConverting: boolean;
  options: ConversionOptions;
}

export const UrlConverter: React.FC<UrlConverterProps> = ({ onConvertUrl, isConverting }) => {
  const [url, setUrl] = useState("");
  const [error, setError] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!url.trim()) {
      setError("Iltimos, to'g'ri URL manzil kiriting");
      return;
    }
    if (!url.startsWith("http://") && !url.startsWith("https://")) {
      setError("Havola http:// yoki https:// bilan boshlanishi kerak");
      return;
    }
    setError("");
    await onConvertUrl(url.trim());
  };

  const handleQuickUrl = (quickUrl: string) => {
    setUrl(quickUrl);
  };

  return (
    <div className="bg-white border border-zinc-200 rounded-2xl p-6 shadow-sm space-y-4">
      <div>
        <h3 className="text-sm font-semibold text-zinc-900 flex items-center gap-2">
          <Globe className="w-4 h-4 text-indigo-600" />
          Web Sahifa yoki Maqolani Markdown ga o'tkazish
        </h3>
        <p className="text-xs text-zinc-500 mt-1">
          Onlayn hujjatlar, maqolalar, yangiliklar yoki Wikipedia sahifalaridan toza, reklamasiz Markdown matnini ajratib oling.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="flex flex-col sm:flex-row gap-2">
          <div className="relative flex-1">
            <Globe className="w-4 h-4 text-zinc-400 absolute left-3.5 top-3" />
            <input
              type="url"
              id="input-web-url"
              value={url}
              onChange={(e) => {
                setUrl(e.target.value);
                if (error) setError("");
              }}
              placeholder="https://uz.wikipedia.org/wiki/Markdown yoki har qanday web havola..."
              className="w-full pl-9 pr-3 py-2.5 text-xs border border-zinc-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-zinc-50/50"
            />
          </div>

          <button
            type="submit"
            disabled={isConverting || !url.trim()}
            id="btn-submit-url"
            className={`flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl font-medium text-xs text-white transition-all ${
              isConverting || !url.trim()
                ? "bg-indigo-300 cursor-not-allowed"
                : "bg-indigo-600 hover:bg-indigo-700 active:scale-[0.98] shadow-sm"
            }`}
          >
            {isConverting ? (
              <>
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
                <span>O'girilmoqda...</span>
              </>
            ) : (
              <>
                <span>URL ni o'girish</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </>
            )}
          </button>
        </div>

        {error && (
          <div className="flex items-center gap-1.5 text-xs text-red-600 bg-red-50 p-2 rounded-lg border border-red-200/60">
            <AlertCircle className="w-3.5 h-3.5 shrink-0" />
            <span>{error}</span>
          </div>
        )}
      </form>

      {/* Suggested Quick Links */}
      <div className="pt-2 border-t border-zinc-100 flex flex-wrap items-center gap-2 text-xs">
        <span className="text-[11px] text-zinc-400 font-medium">Sinab ko'rish:</span>
        <button
          type="button"
          onClick={() => handleQuickUrl("https://en.wikipedia.org/wiki/Markdown")}
          className="text-[11px] text-indigo-600 hover:text-indigo-800 bg-indigo-50/70 hover:bg-indigo-100/70 px-2 py-0.5 rounded border border-indigo-100 transition-colors"
        >
          Wikipedia: Markdown
        </button>
        <button
          type="button"
          onClick={() => handleQuickUrl("https://raw.githubusercontent.com/microsoft/markitdown/main/README.md")}
          className="text-[11px] text-indigo-600 hover:text-indigo-800 bg-indigo-50/70 hover:bg-indigo-100/70 px-2 py-0.5 rounded border border-indigo-100 transition-colors"
        >
          Microsoft MarkItDown README
        </button>
      </div>
    </div>
  );
};
