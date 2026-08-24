import React from "react";
import { Sparkles, FileSpreadsheet, FileCode, Globe, FileText, ArrowRight } from "lucide-react";
import { SAMPLE_FILES, SampleFile } from "../data/samples";

interface SamplePickerProps {
  onSelectSample: (sample: SampleFile) => void;
  disabled: boolean;
}

export const SamplePicker: React.FC<SamplePickerProps> = ({ onSelectSample, disabled }) => {
  const getIcon = (format: string) => {
    if (format === "CSV") return <FileSpreadsheet className="w-3.5 h-3.5 text-emerald-600" />;
    if (format === "JSON") return <FileCode className="w-3.5 h-3.5 text-amber-600" />;
    if (format === "HTML") return <Globe className="w-3.5 h-3.5 text-blue-600" />;
    return <FileText className="w-3.5 h-3.5 text-indigo-600" />;
  };

  return (
    <div className="bg-zinc-50/80 border border-zinc-200/80 rounded-xl p-4">
      <div className="flex items-center justify-between mb-2.5">
        <div className="flex items-center space-x-1.5 text-xs font-semibold text-zinc-800">
          <Sparkles className="w-3.5 h-3.5 text-indigo-600" />
          <span>Namuna Fayllar (1 marta bosib sinab ko'rish)</span>
        </div>
        <span className="text-[11px] text-zinc-500">Tayyor namuna orqali darhol sinang</span>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2.5">
        {SAMPLE_FILES.map((sample) => (
          <button
            key={sample.name}
            onClick={() => onSelectSample(sample)}
            disabled={disabled}
            id={`btn-sample-${sample.format.toLowerCase()}`}
            className="flex flex-col text-left p-2.5 rounded-lg bg-white border border-zinc-200 hover:border-indigo-400 hover:shadow-xs transition-all text-xs group disabled:opacity-50 disabled:pointer-events-none"
          >
            <div className="flex items-center justify-between w-full mb-1">
              <div className="flex items-center space-x-1.5">
                {getIcon(sample.format)}
                <span className="font-semibold text-zinc-800 group-hover:text-indigo-600 transition-colors">
                  {sample.format}
                </span>
              </div>
              <span className="text-[10px] font-mono text-zinc-400">{sample.type}</span>
            </div>
            <p className="text-[11px] text-zinc-500 line-clamp-2 leading-relaxed">
              {sample.description}
            </p>
            <div className="mt-2 flex items-center text-[10px] font-medium text-indigo-600 group-hover:translate-x-0.5 transition-transform">
              <span>Namunani o'girish</span>
              <ArrowRight className="w-3 h-3 ml-0.5" />
            </div>
          </button>
        ))}
      </div>
    </div>
  );
};
