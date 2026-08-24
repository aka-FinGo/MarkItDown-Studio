import React from "react";
import { FileText, Sparkles, Layers, Key, Palette } from "lucide-react";
import { ThemeType } from "../types";

interface HeaderProps {
  onOpenApiDocs: () => void;
  onOpenFormats: () => void;
  onOpenApiKey: () => void;
  hasApiKey: boolean;
  theme: ThemeType;
  onThemeChange: (theme: ThemeType) => void;
}

export const Header: React.FC<HeaderProps> = ({
  onOpenFormats,
  onOpenApiKey,
  hasApiKey,
  theme,
  onThemeChange,
}) => {
  return (
    <header className="border-b border-zinc-700/40 bg-zinc-900/90 backdrop-blur-md sticky top-0 z-30 shadow-xs">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        {/* Brand */}
        <div className="flex items-center space-x-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-zinc-900 via-indigo-950 to-indigo-600 flex items-center justify-center shadow-md text-white font-bold">
            <FileText className="w-5 h-5 text-indigo-400" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <span className="font-bold text-white text-lg tracking-tight">MarkItDown Studio</span>
              <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-emerald-950/80 text-emerald-300 border border-emerald-500/50 flex items-center gap-1">
                <Sparkles className="w-3 h-3 text-emerald-400" /> Multi-AI &amp; Obsidian
              </span>
            </div>
            <p className="text-xs text-zinc-400 hidden sm:block">
              PDF (Krill/Lotin), Word, Excel, PPTX, Rasm (OCR) toza Markdown (.md)
            </p>
          </div>
        </div>

        {/* Right Actions */}
        <div className="flex items-center space-x-2.5 sm:space-x-3">
          {/* Theme Selector */}
          <div className="flex items-center gap-1.5 bg-zinc-800/80 border border-zinc-700/60 rounded-xl px-2.5 py-1">
            <Palette className="w-3.5 h-3.5 text-indigo-400 shrink-0" />
            <select
              value={theme}
              onChange={(e) => onThemeChange(e.target.value as ThemeType)}
              className="text-xs font-semibold bg-transparent text-zinc-200 focus:outline-none cursor-pointer pr-1"
            >
              <option value="MidnightGlass" className="bg-zinc-900 text-white">🌌 Midnight Glass</option>
              <option value="ObsidianDark" className="bg-zinc-900 text-white">🔮 Obsidian Dark</option>
              <option value="CyberpunkNeon" className="bg-zinc-900 text-white">⚡ Cyberpunk Neon</option>
              <option value="FrostedCrystal" className="bg-zinc-900 text-white">❄️ Frosted Crystal</option>
            </select>
          </div>

          {/* AI Key Button */}
          <button
            onClick={onOpenApiKey}
            id="btn-api-key"
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-xl transition-all border cursor-pointer ${
              hasApiKey
                ? "bg-indigo-950/80 text-indigo-300 border-indigo-500/60 hover:bg-indigo-900"
                : "bg-zinc-800 text-zinc-300 border-zinc-700 hover:bg-zinc-700"
            }`}
          >
            <Key className={`w-3.5 h-3.5 ${hasApiKey ? "text-indigo-400" : "text-zinc-400"}`} />
            <span>{hasApiKey ? "AI Kalit Ulangan" : "AI Kalit"}</span>
          </button>

          {/* Formats */}
          <button
            onClick={onOpenFormats}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-zinc-300 bg-zinc-800 hover:bg-zinc-700 rounded-xl transition-colors border border-zinc-700/60 cursor-pointer hidden md:flex"
          >
            <Layers className="w-3.5 h-3.5 text-zinc-400" />
            <span>Formatlar</span>
          </button>
        </div>
      </div>
    </header>
  );
};
