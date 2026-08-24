import React from "react";
import {
  FileText,
  Key,
  BookOpen,
  Sparkles,
  Layers,
  Palette,
  Globe,
} from "lucide-react";
import { ThemeType } from "../types";
import { Language, TRANSLATIONS } from "../locales/i18n";

interface HeaderProps {
  onOpenApiDocs: () => void;
  onOpenFormats: () => void;
  onOpenApiKey: () => void;
  hasApiKey: boolean;
  theme: ThemeType;
  onThemeChange: (theme: ThemeType) => void;
  language: Language;
  onLanguageChange: (lang: Language) => void;
}

export const Header: React.FC<HeaderProps> = ({
  onOpenApiDocs,
  onOpenFormats,
  onOpenApiKey,
  hasApiKey,
  theme,
  onThemeChange,
  language,
  onLanguageChange,
}) => {
  const t = TRANSLATIONS[language];

  return (
    <header className="sticky top-0 z-40 w-full border-b border-zinc-800/80 bg-zinc-950/80 backdrop-blur-md transition-colors">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between gap-2">
        {/* Left: Logo & Branding */}
        <div className="flex items-center space-x-3">
          <div className="w-10 h-10 rounded-xl bg-indigo-600 flex items-center justify-center text-white shadow-lg shadow-indigo-500/30">
            <FileText className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h1 className="text-base font-extrabold text-white tracking-tight">
                {t.appTitle}
              </h1>
              <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                {t.badge}
              </span>
            </div>
            <p className="text-xs text-zinc-400">Microsoft MarkItDown Web &amp; Desktop</p>
          </div>
        </div>

        {/* Right: Actions, Language & Theme Switchers */}
        <div className="flex items-center space-x-2 sm:space-x-3">
          {/* Language Switcher */}
          <div className="flex items-center space-x-1 bg-zinc-900 border border-zinc-800 rounded-xl px-2 py-1">
            <Globe className="w-3.5 h-3.5 text-zinc-400" />
            <select
              value={language}
              onChange={(e) => onLanguageChange(e.target.value as Language)}
              className="bg-transparent text-xs font-semibold text-zinc-200 focus:outline-none cursor-pointer"
            >
              <option value="uz" className="bg-zinc-900 text-white">🇺🇿 O'zbekcha</option>
              <option value="en" className="bg-zinc-900 text-white">🇬🇧 English</option>
              <option value="ru" className="bg-zinc-900 text-white">🇷🇺 Русский</option>
            </select>
          </div>

          {/* Theme Selector */}
          <div className="flex items-center space-x-1 bg-zinc-900 border border-zinc-800 rounded-xl px-2 py-1">
            <Palette className="w-3.5 h-3.5 text-zinc-400" />
            <select
              value={theme}
              onChange={(e) => onThemeChange(e.target.value as ThemeType)}
              className="bg-transparent text-xs font-semibold text-zinc-200 focus:outline-none cursor-pointer"
            >
              <option value="MidnightGlass" className="bg-zinc-900 text-white">🌌 Midnight Glass</option>
              <option value="ObsidianDark" className="bg-zinc-900 text-white">🔮 Obsidian Dark</option>
              <option value="CyberpunkNeon" className="bg-zinc-900 text-white">⚡ Cyberpunk</option>
              <option value="FrostedCrystal" className="bg-zinc-900 text-white">❄️ Frosted</option>
            </select>
          </div>

          {/* Format Guide */}
          <button
            onClick={onOpenFormats}
            className="hidden md:flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-zinc-300 hover:text-white bg-zinc-900 hover:bg-zinc-800 rounded-xl border border-zinc-800 transition-colors cursor-pointer"
          >
            <Layers className="w-3.5 h-3.5 text-zinc-400" />
            <span>{t.supportedFormats}</span>
          </button>

          {/* API Modal Button */}
          <button
            onClick={onOpenApiKey}
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-xl border transition-all cursor-pointer shadow-xs ${
              hasApiKey
                ? "bg-indigo-600/20 text-indigo-300 border-indigo-500/40 hover:bg-indigo-600/30"
                : "bg-amber-500/10 text-amber-300 border-amber-500/30 hover:bg-amber-500/20"
            }`}
          >
            <Key className="w-3.5 h-3.5" />
            <span>{hasApiKey ? t.keySaved : t.apiKey}</span>
          </button>
        </div>
      </div>
    </header>
  );
};
