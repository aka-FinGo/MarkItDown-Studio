import React, { useState } from "react";
import { X, Terminal, Copy, Check, Code2, Sparkles, Send } from "lucide-react";

interface ApiDocsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const ApiDocsModal: React.FC<ApiDocsModalProps> = ({ isOpen, onClose }) => {
  const [activeTab, setActiveTab] = useState<"curl" | "python" | "node" | "url">("curl");
  const [copied, setCopied] = useState(false);

  if (!isOpen) return null;

  const currentHost = typeof window !== "undefined" ? window.location.origin : "https://your-markitdown-app.run.app";

  const codeSnippets = {
    curl: `# 1. Har qanday faylni (PDF, Word, Excel, Rasm, Audio) Markdown ga o'girish
curl -X POST "${currentHost}/api/convert" \\
  -F "files=@hujjat.pdf"

# 2. Web sahifa yoki maqola havolasini Markdown ga o'girish
curl -X POST "${currentHost}/api/convert-url" \\
  -H "Content-Type: application/json" \\
  -d '{"url": "https://uz.wikipedia.org/wiki/Markdown"}'

# 3. Matn, HTML yoki JSON ni toza Markdown jadvali/matniga o'girish
curl -X POST "${currentHost}/api/convert-text" \\
  -H "Content-Type: application/json" \\
  -d '{"text": "<table><tr><td>Mahsulot</td><td>Narxi</td></tr><tr><td>Pro Plan</td><td>99000</td></tr></table>", "format": "html"}'`,

    python: `import requests

API_URL = "${currentHost}/api/convert"

# PDF, Word, Excel, Rasm (OCR) yoki Audio yuklash
with open("moliyaviy_hisobot.pdf", "rb") as f:
    files = {"files": ("moliyaviy_hisobot.pdf", f, "application/pdf")}
    response = requests.post(API_URL, files=files)
    result = response.json()

markdown_matni = result["results"][0]["markdown"]
print("Olingan Markdown matni:")
print(markdown_matni)

# .md fayl sifatida saqlab olish
with open("hisobot.md", "w", encoding="utf-8") as out:
    out.write(markdown_matni)`,

    node: `import fs from 'fs';
import FormData from 'form-data';
import fetch from 'node-fetch';

async function convertToMarkdown(filePath) {
  const form = new FormData();
  form.append('files', fs.createReadStream(filePath));

  const res = await fetch('${currentHost}/api/convert', {
    method: 'POST',
    body: form
  });

  const data = await res.json();
  const md = data.results[0].markdown;
  
  // .md faylga saqlash
  fs.writeFileSync('natija.md', md, 'utf-8');
  console.log('Markdown fayl muvaffaqiyatli saqlandi!');
  return md;
}

convertToMarkdown('taqdimot.pptx');`,

    url: `// Web sahifani Markdown ga o'tkazish
const response = await fetch('${currentHost}/api/convert-url', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    url: 'https://uz.wikipedia.org/wiki/Markdown'
  })
});

const data = await response.json();
console.log(data.result.markdown);`,
  };

  const handleCopy = () => {
    navigator.clipboard.writeText(codeSnippets[activeTab]);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-zinc-900/60 backdrop-blur-xs">
      <div className="bg-white rounded-2xl border border-zinc-200 shadow-2xl max-w-3xl w-full overflow-hidden flex flex-col max-h-[85vh] animate-in fade-in zoom-in-95 duration-150">
        <div className="px-6 py-4 border-b border-zinc-200 flex items-center justify-between bg-zinc-50">
          <div className="flex items-center space-x-2.5">
            <div className="w-8 h-8 rounded-lg bg-zinc-900 text-white flex items-center justify-center font-mono text-sm">
              &gt;_
            </div>
            <div>
              <h3 className="text-sm font-semibold text-zinc-900">API va Dasturiy Integratsiya</h3>
              <p className="text-xs text-zinc-500">
                cURL, Python va Node.js orqali to'g'ridan-to'g'ri .md fayllarga o'girish
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            id="btn-close-api-docs-modal"
            className="text-zinc-400 hover:text-zinc-700 p-1.5 rounded-lg hover:bg-zinc-200/60 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="p-6 overflow-y-auto space-y-4">
          {/* Navigation tabs */}
          <div className="flex items-center justify-between border-b border-zinc-200 pb-2">
            <div className="flex space-x-1">
              {[
                { id: "curl", label: "cURL (Terminal)" },
                { id: "python", label: "Python" },
                { id: "node", label: "Node.js" },
                { id: "url", label: "Web URL API" },
              ].map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id as any)}
                  className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-colors ${
                    activeTab === tab.id
                      ? "bg-zinc-900 text-white"
                      : "text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100"
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </div>

            <button
              onClick={handleCopy}
              className="flex items-center gap-1 text-xs text-zinc-600 hover:text-zinc-900 px-2.5 py-1 rounded bg-zinc-100 hover:bg-zinc-200 transition-colors"
            >
              {copied ? <Check className="w-3.5 h-3.5 text-emerald-600" /> : <Copy className="w-3.5 h-3.5" />}
              <span>{copied ? "Nusxalandi!" : "Kodni nusxalash"}</span>
            </button>
          </div>

          {/* Code display */}
          <div className="bg-zinc-900 text-zinc-100 p-4 rounded-xl font-mono text-xs overflow-x-auto leading-relaxed border border-zinc-800 shadow-inner">
            <pre>{codeSnippets[activeTab]}</pre>
          </div>
        </div>

        <div className="px-6 py-3 border-t border-zinc-200 bg-zinc-50 flex items-center justify-between text-xs text-zinc-500">
          <span>Har qanday dasturingizga oson ulanadi</span>
          <button
            onClick={onClose}
            className="px-4 py-1.5 bg-zinc-900 text-white rounded-lg hover:bg-zinc-800 text-xs font-medium"
          >
            Yopish
          </button>
        </div>
      </div>
    </div>
  );
};
