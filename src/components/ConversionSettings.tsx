import React, { useState } from "react";
import { Sparkles, Sliders, ChevronDown, ChevronUp, Tag, Info, ShieldCheck, Zap } from "lucide-react";
import { ConversionOptions } from "../types";

interface ConversionSettingsProps {
  options: ConversionOptions;
  onChange: (options: ConversionOptions) => void;
}

export const ConversionSettings: React.FC<ConversionSettingsProps> = ({ options, onChange }) => {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <div className="bg-white border border-zinc-200/80 rounded-xl p-4 shadow-xs">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        {/* Simple features status */}
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2 bg-emerald-50 text-emerald-800 border border-emerald-200 px-3 py-1.5 rounded-lg text-xs font-semibold">
            <Zap className="w-3.5 h-3.5 text-emerald-600" />
            <span>Hujjatlar uchun 0 AI Token (Bepul)</span>
          </div>

          <div className="flex items-center gap-2 bg-indigo-50 text-indigo-800 border border-indigo-200 px-3 py-1.5 rounded-lg text-xs font-semibold">
            <Sparkles className="w-3.5 h-3.5 text-indigo-600" />
            <span>Rasm (OCR) & Audio (Ovozdan matn) Avtomatik</span>
          </div>

          {/* YAML Frontmatter Toggle (Default OFF) */}
          <label className="flex items-center gap-2 cursor-pointer select-none text-xs text-zinc-600 hover:text-zinc-900 ml-1">
            <input
              type="checkbox"
              id="toggle-frontmatter"
              checked={options.includeFrontmatter || false}
              onChange={(e) => onChange({ ...options, includeFrontmatter: e.target.checked })}
              className="w-3.5 h-3.5 text-indigo-600 rounded border-zinc-300 focus:ring-indigo-500"
            />
            <span className="flex items-center gap-1">
              <Tag className="w-3 h-3 text-zinc-400" />
              YAML ma'lumotlar bloki (Frontmatter)
            </span>
          </label>
        </div>

        {/* Expand Advanced Controls */}
        <button
          onClick={() => setIsOpen(!isOpen)}
          id="btn-advanced-options-toggle"
          className="flex items-center gap-1.5 text-xs font-medium text-zinc-600 hover:text-zinc-900 self-start sm:self-auto py-1 px-2 rounded-lg hover:bg-zinc-100 transition-colors"
        >
          <Sliders className="w-3.5 h-3.5 text-zinc-500" />
          <span>Maxsus Sozlamalar</span>
          {isOpen ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
        </button>
      </div>

      {/* Advanced Drawer */}
      {isOpen && (
        <div className="mt-3 pt-3 border-t border-zinc-100 space-y-3 text-xs">
          <div>
            <label className="block font-medium text-zinc-800 mb-1">
              Maxsus Ko'rsatma / Prompt (Ixtiyoriy)
            </label>
            <p className="text-zinc-500 text-[11px] mb-1.5">
              Rasm, audio yoki hujjatdan matn ajratishda sun'iy intellektga maxsus buyruq berish (masalan: "Faqat jadval qismini ajrat" yoki "O'zbekchaga tarjima qil").
            </p>
            <input
              type="text"
              id="input-custom-prompt"
              value={options.customPrompt || ""}
              onChange={(e) => onChange({ ...options, customPrompt: e.target.value })}
              placeholder="Masalan: Jadvallarni chiroyli formatla..."
              className="w-full px-3 py-2 text-xs border border-zinc-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-zinc-50/50"
            />
          </div>

          <div className="flex items-center text-[11px] text-zinc-500 bg-zinc-50 p-2.5 rounded-lg border border-zinc-200/60">
            <Info className="w-4 h-4 text-indigo-500 shrink-0 mr-2" />
            <span>
              Barcha konvertatsiya qilingan matnlar to'g'ridan-to'g'ri .md (Markdown) fayl sifatida saqlanishga tayyor holda shakllanadi.
            </span>
          </div>
        </div>
      )}
    </div>
  );
};
