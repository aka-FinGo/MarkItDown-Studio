import React, { useEffect, useState } from "react";
import { X, Layers, CheckCircle2, FileText, FileSpreadsheet, Image as ImageIcon, Music, Code, Globe } from "lucide-react";
import { FormatCategory } from "../types";

interface SupportedFormatsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const SupportedFormatsModal: React.FC<SupportedFormatsModalProps> = ({ isOpen, onClose }) => {
  const [categories, setCategories] = useState<FormatCategory[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (isOpen) {
      fetch("/api/supported-formats")
        .then((res) => res.json())
        .then((data) => {
          setCategories(data.categories || []);
          setLoading(false);
        })
        .catch(() => {
          setLoading(false);
        });
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const getCategoryIcon = (name: string) => {
    if (name.includes("Hujjat") || name.includes("Document")) return <FileText className="w-4 h-4 text-indigo-600" />;
    if (name.includes("Jadval") || name.includes("Spreadsheet")) return <FileSpreadsheet className="w-4 h-4 text-emerald-600" />;
    if (name.includes("Rasm") || name.includes("Image")) return <ImageIcon className="w-4 h-4 text-purple-600" />;
    if (name.includes("Audio") || name.includes("Ovoz")) return <Music className="w-4 h-4 text-amber-600" />;
    if (name.includes("Kod") || name.includes("Code")) return <Code className="w-4 h-4 text-blue-600" />;
    return <Globe className="w-4 h-4 text-cyan-600" />;
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-zinc-900/60 backdrop-blur-xs">
      <div className="bg-white rounded-2xl border border-zinc-200 shadow-2xl max-w-3xl w-full overflow-hidden flex flex-col max-h-[85vh] animate-in fade-in zoom-in-95 duration-150">
        <div className="px-6 py-4 border-b border-zinc-200 flex items-center justify-between bg-zinc-50">
          <div className="flex items-center space-x-2.5">
            <div className="w-8 h-8 rounded-lg bg-indigo-600 text-white flex items-center justify-center font-bold">
              <Layers className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-sm font-semibold text-zinc-900">Qo'llab-quvvatlanuvchi Formatlar & Imkoniyatlar</h3>
              <p className="text-xs text-zinc-500">
                Microsoft MarkItDown va Gemini Multimodal AI texnologiyalari asosida
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            id="btn-close-formats-modal"
            className="text-zinc-400 hover:text-zinc-700 p-1.5 rounded-lg hover:bg-zinc-200/60 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="p-6 overflow-y-auto space-y-4">
          {loading ? (
            <div className="py-12 text-center text-xs text-zinc-500">Formatlar ro'yxati yuklanmoqda...</div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {categories.map((cat, idx) => (
                <div key={idx} className="bg-zinc-50 border border-zinc-200/80 rounded-xl p-4 space-y-2.5">
                  <div className="flex items-center space-x-2">
                    {getCategoryIcon(cat.name)}
                    <h4 className="text-xs font-semibold text-zinc-900">{cat.name}</h4>
                  </div>

                  <div className="flex flex-wrap gap-1">
                    {cat.formats.map((fmt, fIdx) => (
                      <span
                        key={fIdx}
                        className="text-[10px] font-mono bg-white px-2 py-0.5 rounded border border-zinc-200 text-zinc-700"
                      >
                        {fmt}
                      </span>
                    ))}
                  </div>

                  <div className="pt-2 border-t border-zinc-200/60 space-y-1">
                    {cat.capabilities.map((cap, cIdx) => (
                      <div key={cIdx} className="flex items-center text-[11px] text-zinc-600">
                        <CheckCircle2 className="w-3 h-3 text-emerald-500 mr-1.5 shrink-0" />
                        <span>{cap}</span>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="px-6 py-3 border-t border-zinc-200 bg-zinc-50 flex items-center justify-between text-xs text-zinc-500">
          <span>Har qanday format to'g'ridan-to'g'ri .md faylga o'giriladi</span>
          <button
            onClick={onClose}
            className="px-4 py-1.5 bg-zinc-900 text-white rounded-lg hover:bg-zinc-800 text-xs font-medium"
          >
            Tushundim
          </button>
        </div>
      </div>
    </div>
  );
};
