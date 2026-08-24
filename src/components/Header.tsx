import React from "react";
import { FileText, Sparkles, Terminal, Layers, BookOpen, Key, User, LogIn, Check } from "lucide-react";
import { UserProfile, ThemeType } from "../types";

interface HeaderProps {
  onOpenApiDocs: () => void;
  onOpenFormats: () => void;
  onOpenApiKey: () => void;
  onOpenAuth: () => void;
  hasApiKey: boolean;
  user: UserProfile | null;
  theme: ThemeType;
  onThemeChange: (theme: ThemeType) => void;
}

export const Header: React.FC<HeaderProps> = ({
  onOpenApiDocs,
  onOpenFormats,
  onOpenApiKey,
  onOpenAuth,
  hasApiKey,
  user,
  theme,
  onThemeChange,
}) => {
  return (
    <header className="border-b border-zinc-200/80 bg-white/90 backdrop-blur-md sticky top-0 z-30 shadow-xs">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        {/* Brand */}
        <div className="flex items-center space-x-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-zinc-900 via-indigo-950 to-indigo-600 flex items-center justify-center shadow-md text-white font-bold">
            <FileText className="w-5 h-5 text-indigo-400" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <span className="font-bold text-zinc-900 text-lg tracking-tight">MarkItDown Studio</span>
              <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200/70 flex items-center gap-1">
                <Sparkles className="w-3 h-3 text-emerald-500" /> Obsidian &amp; Multi-AI
              </span>
            </div>
            <p className="text-xs text-zinc-500 hidden sm:block">
              PDF (Krill/Lotin), Word, Excel, PPTX, Rasm (OCR) va Audio toza Markdown (.md)
            </p>
          </div>
        </div>

        {/* Right Actions */}
        <div className="flex items-center space-x-2 sm:space-x-3">
          {/* Theme Selector */}
          <select
            value={theme}
            onChange={(e) => onThemeChange(e.target.value as ThemeType)}
            className="text-xs font-semibold px-2.5 py-1.5 rounded-lg border border-zinc-200 bg-zinc-50 text-zinc-700 focus:outline-none focus:ring-1 focus:ring-indigo-500 cursor-pointer hidden md:block"
          >
            <option value="MidnightGlass">🌌 Midnight Glass</option>
            <option value="ObsidianDark">🔮 Obsidian Dark</option>
            <option value="CyberpunkNeon">⚡ Cyberpunk</option>
            <option value="FrostedCrystal">❄️ Frosted Crystal</option>
          </select>

          {/* API Key Modal Trigger */}
          <button
            onClick={onOpenApiKey}
            id="btn-api-key"
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg transition-all border cursor-pointer ${
              hasApiKey
                ? "bg-indigo-50 text-indigo-700 border-indigo-200 hover:bg-indigo-100"
                : "bg-zinc-100 text-zinc-700 border-zinc-200 hover:bg-zinc-200/80"
            }`}
          >
            <Key className={`w-3.5 h-3.5 ${hasApiKey ? "text-indigo-600" : "text-zinc-600"}`} />
            <span>{hasApiKey ? "AI Kalit Ulangan" : "AI Kalit"}</span>
          </button>

          {/* Google OAuth Login Button / Avatar */}
          <button
            onClick={onOpenAuth}
            id="btn-google-auth"
            className="flex items-center gap-2 px-3 py-1.5 text-xs font-semibold rounded-lg border border-zinc-200 bg-zinc-50 hover:bg-zinc-100 text-zinc-800 transition-all cursor-pointer"
          >
            {user ? (
              <>
                <img src={user.picture} alt={user.name} className="w-4 h-4 rounded-full border border-emerald-500" />
                <span className="max-w-[80px] sm:max-w-[120px] truncate text-[11px]">{user.name}</span>
              </>
            ) : (
              <>
                <svg className="w-3.5 h-3.5" viewBox="0 0 24 24">
                  <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                  <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                  <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" />
                  <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" />
                </svg>
                <span className="hidden sm:inline">Google Kirish</span>
              </>
            )}
          </button>

          {/* Formats & Docs */}
          <button
            onClick={onOpenFormats}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-zinc-700 bg-zinc-100 hover:bg-zinc-200/80 rounded-lg transition-colors border border-zinc-200/60 cursor-pointer hidden lg:flex"
          >
            <Layers className="w-3.5 h-3.5 text-zinc-600" />
            <span>Formatlar</span>
          </button>
        </div>
      </div>
    </header>
  );
};
