import React, { useState } from "react";
import { Code, ArrowRight, Loader2, Sparkles, AlertCircle, FileText } from "lucide-react";
import { ConversionOptions } from "../types";

interface TextConverterProps {
  onConvertText: (text: string, format: string, filename: string) => Promise<void>;
  isConverting: boolean;
  options: ConversionOptions;
}

export const TextConverter: React.FC<TextConverterProps> = ({ onConvertText, isConverting }) => {
  const [text, setText] = useState("");
  const [format, setFormat] = useState("html");
  const [filename, setFilename] = useState("matn_namunasi");
  const [error, setError] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!text.trim()) {
      setError("Iltimos, o'girish uchun matn yoki kod kiriting");
      return;
    }
    setError("");
    await onConvertText(text, format, `${filename}.${format}`);
  };

  const handlePasteSample = (type: string) => {
    if (type === "html") {
      setFormat("html");
      setFilename("jadval_namunasi");
      setText(
        `<table>\n  <tr><th>Chorak</th><th>Tushum</th><th>Foyda foizi</th></tr>\n  <tr><td>1-chorak 2026</td><td>$14.2M</td><td>72.4%</td></tr>\n  <tr><td>2-chorak 2026</td><td>$18.5M</td><td>75.1%</td></tr>\n</table>\n<p><strong>Izoh:</strong> Barcha hisob-kitoblar tasdiqlangan.</p>`
      );
    } else if (type === "json") {
      setFormat("json");
      setFilename("mahsulotlar");
      setText(
        JSON.stringify(
          [
            { id: "PROD-1", nomi: "MarkItDown Engine", kategoriya: "Dasturiy vosita", narx: 299, holati: "Faol" },
            { id: "PROD-2", nomi: "Gemini Pro Agent", kategoriya: "Sun'iy intellekt", narx: 499, holati: "Faol" },
            { id: "PROD-3", nomi: "OCR Matn O'quvchi", kategoriya: "Vizual vosita", narx: 149, holati: "Sinovda" },
          ],
          null,
          2
        )
      );
    } else if (type === "csv") {
      setFormat("csv");
      setFilename("savdo_hisoboti");
      setText("Sana,Tranzaksiya_ID,Summa,Holat\n2026-08-20,TX_1001,450000,Bajarildi\n2026-08-21,TX_1002,1200000,Bajarildi\n2026-08-22,TX_1003,89000,Qaytarildi");
    }
  };

  return (
    <div className="bg-white border border-zinc-200 rounded-2xl p-6 shadow-sm space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <div>
          <h3 className="text-sm font-semibold text-zinc-900 flex items-center gap-2">
            <Code className="w-4 h-4 text-indigo-600" />
            Matn, Kod, HTML, JSON yoki CSV nusxasini kiritish
          </h3>
          <p className="text-xs text-zinc-500 mt-1">
            Har qanday jadval, HTML kodi yoki murakkab tuzilmalarni darhol toza Markdown matniga o'giring.
          </p>
        </div>

        <div className="flex items-center gap-1.5 self-start sm:self-auto">
          <span className="text-[11px] text-zinc-400 font-medium mr-1">Namuna matnlar:</span>
          <button
            type="button"
            onClick={() => handlePasteSample("html")}
            className="text-[11px] text-zinc-700 hover:text-indigo-600 bg-zinc-100 px-2 py-0.5 rounded border border-zinc-200/60"
          >
            HTML Jadval
          </button>
          <button
            type="button"
            onClick={() => handlePasteSample("json")}
            className="text-[11px] text-zinc-700 hover:text-indigo-600 bg-zinc-100 px-2 py-0.5 rounded border border-zinc-200/60"
          >
            JSON
          </button>
          <button
            type="button"
            onClick={() => handlePasteSample("csv")}
            className="text-[11px] text-zinc-700 hover:text-indigo-600 bg-zinc-100 px-2 py-0.5 rounded border border-zinc-200/60"
          >
            CSV
          </button>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="flex flex-col sm:flex-row gap-3">
          <div className="w-full sm:w-1/3">
            <label className="block text-xs font-medium text-zinc-700 mb-1">Format turi</label>
            <select
              id="select-text-format"
              value={format}
              onChange={(e) => setFormat(e.target.value)}
              className="w-full px-3 py-2 text-xs border border-zinc-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-white"
            >
              <option value="html">HTML</option>
              <option value="json">JSON</option>
              <option value="csv">CSV</option>
              <option value="tsv">TSV</option>
              <option value="sql">SQL</option>
              <option value="py">Python</option>
              <option value="ts">TypeScript / JavaScript</option>
              <option value="xml">XML / SVG</option>
              <option value="txt">Oddiy matn (Plain text)</option>
            </select>
          </div>

          <div className="w-full sm:w-2/3">
            <label className="block text-xs font-medium text-zinc-700 mb-1">Hujjat nomi (Fayl nomi)</label>
            <input
              type="text"
              id="input-text-filename"
              value={filename}
              onChange={(e) => setFilename(e.target.value)}
              placeholder="fayl_nomi"
              className="w-full px-3 py-2 text-xs border border-zinc-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-zinc-50/50"
            />
          </div>
        </div>

        <div>
          <label className="block text-xs font-medium text-zinc-700 mb-1">Matn yoki Kodni joylashtiring</label>
          <textarea
            id="textarea-raw-content"
            rows={7}
            value={text}
            onChange={(e) => {
              setText(e.target.value);
              if (error) setError("");
            }}
            placeholder="Matn, HTML kodi, JSON yoki jadvallarni shu yerga qo'ying..."
            className="w-full p-3 font-mono text-xs border border-zinc-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-zinc-50/30 leading-relaxed"
          />
        </div>

        {error && (
          <div className="flex items-center gap-1.5 text-xs text-red-600 bg-red-50 p-2 rounded-lg border border-red-200/60">
            <AlertCircle className="w-3.5 h-3.5 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <div className="flex justify-end">
          <button
            type="submit"
            disabled={isConverting || !text.trim()}
            id="btn-convert-text"
            className={`flex items-center gap-2 px-6 py-2.5 rounded-xl font-medium text-xs text-white transition-all ${
              isConverting || !text.trim()
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
                <span>Markdown (.md) ga o'tkazish</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  );
};
