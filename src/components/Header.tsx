import React from "react";
import { FileText, Sparkles, Terminal, Layers, BookOpen, ExternalLink, Key, Zap } from "lucide-react";

interface HeaderProps {
  onOpenApiDocs: () => void;
  onOpenFormats: () => void;
  onOpenApiKey: () => void;
  hasApiKey: boolean;
}

export const Header: React.FC<HeaderProps> = ({
  onOpenApiDocs,
  onOpenFormats,
  onOpenApiKey,
  hasApiKey,
}) => {
  return (
    <header className="border-b border-zinc-200 bg-white/80 backdrop-blur-md sticky top-0 z-30">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        {/* Brand */}
        <div className="flex items-center space-x-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-zinc-900 via-zinc-800 to-zinc-700 flex items-center justify-center shadow-md shadow-zinc-900/10 text-white font-bold">
            <FileText className="w-5 h-5 text-indigo-400" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <span className="font-bold text-zinc-900 text-lg tracking-tight">MarkItDown Studio</span>
              <span className="text-[11px] font-medium px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200/60 flex items-center gap-1">
                <Zap className="w-3 h-3 text-emerald-500" /> 100% Statik & Brauzerda
              </span>
            </div>
            <p className="text-xs text-zinc-500 hidden sm:block">
              Fayl, Rasm, Audio va Hujjatlarni toza Markdown (.md) formatiga o'tkazish xizmati
            </p>
          </div>
        </div>

        {/* Right Actions */}
        <div className="flex items-center space-x-2 sm:space-x-3">
          {/* API Key Modal Trigger */}
          <button
            onClick={onOpenApiKey}
            id="btn-api-key"
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg transition-colors border ${
              hasApiKey
                ? "bg-indigo-50 text-indigo-700 border-indigo-200 hover:bg-indigo-100"
                : "bg-zinc-100 text-zinc-700 border-zinc-200/60 hover:bg-zinc-200/80"
            }`}
          >
            <Key className={`w-3.5 h-3.5 ${hasApiKey ? "text-indigo-600" : "text-zinc-600"}`} />
            <span className="hidden sm:inline">
              {hasApiKey ? "Gemini AI Ulangan" : "AI Kalit"}
            </span>
            <span className="sm:hidden">AI</span>
          </button>

          <button
            onClick={onOpenFormats}
            id="btn-supported-formats"
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-zinc-700 bg-zinc-100 hover:bg-zinc-200/80 rounded-lg transition-colors border border-zinc-200/60"
          >
            <Layers className="w-3.5 h-3.5 text-zinc-600" />
            <span className="hidden sm:inline">Formatlar</span>
            <span className="sm:hidden">Formatlar</span>
          </button>

          <button
            onClick={onOpenApiDocs}
            id="btn-api-docs"
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-zinc-700 bg-zinc-100 hover:bg-zinc-200/80 rounded-lg transition-colors border border-zinc-200/60"
          >
            <Terminal className="w-3.5 h-3.5 text-indigo-600" />
            <span className="hidden sm:inline">API & Kod</span>
            <span className="sm:hidden">API</span>
          </button>

          <a
            href="https://github.com/aka-FinGo/MarkItDown-Studio"
            target="_blank"
            rel="noreferrer noopener"
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-white bg-zinc-900 hover:bg-zinc-800 rounded-lg transition-colors shadow-sm"
          >
            <BookOpen className="w-3.5 h-3.5" />
            <span className="hidden md:inline">GitHub Repo</span>
            <ExternalLink className="w-3 h-3 text-zinc-400" />
          </a>
        </div>
      </div>
    </header>
  );
};
