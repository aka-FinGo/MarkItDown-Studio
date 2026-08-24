import * as XLSX from "xlsx";
import JSZip from "jszip";
import mammoth from "mammoth";
import TurndownService from "turndown";
import * as pdfjsLib from "pdfjs-dist";
import { ConvertedItem, ConversionOptions } from "../types";

// Configure PDF.js worker, CMaps and Standard Fonts for 100% accurate Cyrillic (қ, ғ, ў, ҳ) & Latin rendering
if (typeof window !== "undefined") {
  pdfjsLib.GlobalWorkerOptions.workerSrc = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${pdfjsLib.version}/pdf.worker.min.mjs`;
}

const CMAP_URL = `https://cdn.jsdelivr.net/npm/pdfjs-dist@${pdfjsLib.version}/cmaps/`;
const STANDARD_FONT_URL = `https://cdn.jsdelivr.net/npm/pdfjs-dist@${pdfjsLib.version}/standard_fonts/`;

// Configure Turndown for clean HTML to Markdown conversion
const turndownService = new TurndownService({
  headingStyle: "atx",
  codeBlockStyle: "fenced",
  hr: "---",
  bulletListMarker: "-",
  emDelimiter: "*",
});

turndownService.addRule("table", {
  filter: "table",
  replacement: function (_content, node) {
    const table = node as HTMLElement;
    const rows = Array.from(table.querySelectorAll("tr"));
    if (rows.length === 0) return "";

    const parsedRows = rows.map((tr) => {
      const cells = Array.from(tr.querySelectorAll("th, td"));
      return cells.map((cell) => (cell.textContent || "").trim().replace(/\|/g, "\\|"));
    });

    if (parsedRows.length === 0) return "";
    const maxCols = Math.max(...parsedRows.map((r) => r.length));
    const normalized = parsedRows.map((r) => {
      while (r.length < maxCols) r.push("");
      return r;
    });

    const header = normalized[0];
    const separator = header.map(() => "---");
    const dataRows = normalized.slice(1);

    let md = `\n\n| ${header.join(" | ")} |\n| ${separator.join(" | ")} |\n`;
    for (const row of dataRows) {
      md += `| ${row.join(" | ")} |\n`;
    }
    return md + "\n";
  },
});

export function estimateTokens(text: string): number {
  return Math.ceil(text.length / 3.8);
}

export function countWords(text: string): number {
  const clean = text.replace(/```[\s\S]*?```/g, "").replace(/[#*_`\[\]()]/g, " ");
  const words = clean.trim().match(/\S+/g);
  return words ? words.length : 0;
}

// Cyrillic & Uzbek Unicode Normalizer
function normalizeText(text: string): string {
  if (!text) return "";
  return text
    .normalize("NFC")
    .replace(/\u00A0/g, " ")
    .replace(/[\u200B-\u200D\uFEFF]/g, "");
}

// 1. PDF Conversion with full CMap and Cyrillic glyph support
export async function pdfToMarkdown(arrayBuffer: ArrayBuffer): Promise<{ totalPages: number; pages: { pageNum: number; text: string; hasImages: boolean }[] }> {
  try {
    const loadingTask = pdfjsLib.getDocument({
      data: new Uint8Array(arrayBuffer),
      cMapUrl: CMAP_URL,
      cMapPacked: true,
      standardFontDataUrl: STANDARD_FONT_URL,
      useSystemFonts: true,
    });

    const pdf = await loadingTask.promise;
    const pages: { pageNum: number; text: string; hasImages: boolean }[] = [];

    for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {
      await new Promise((r) => setTimeout(r, 0));

      const page = await pdf.getPage(pageNum);
      const textContent = await page.getTextContent();

      const lineMap: Map<number, { x: number; str: string }[]> = new Map();

      for (const item of textContent.items as any[]) {
        if (!item.str) continue;
        const y = Math.round(item.transform[5]);
        const x = item.transform[4];

        let targetY = y;
        for (const existingY of lineMap.keys()) {
          if (Math.abs(existingY - y) <= 3) {
            targetY = existingY;
            break;
          }
        }

        if (!lineMap.has(targetY)) {
          lineMap.set(targetY, []);
        }
        lineMap.get(targetY)!.push({ x, str: normalizeText(item.str) });
      }

      const sortedYs = Array.from(lineMap.keys()).sort((a, b) => b - a);
      const lines: string[] = [];

      for (const y of sortedYs) {
        const lineItems = lineMap.get(y)!.sort((a, b) => a.x - b.x);
        const lineStr = lineItems.map((i) => i.str).join(" ").trim();
        if (lineStr) {
          lines.push(lineStr);
        }
      }

      const pageText = lines.join("\n");
      const hasImages = textContent.items.length < 5;

      pages.push({ pageNum, text: pageText, hasImages });
    }

    return { totalPages: pdf.numPages, pages };
  } catch (err: any) {
    console.warn("PDF o'qishda xatolik:", err);
    throw new Error(`PDF faylni o'qib bo'lmadi: ${err.message || err}`);
  }
}

// 2. Word (.docx) Conversion
export async function docxToMarkdown(arrayBuffer: ArrayBuffer): Promise<string> {
  try {
    const result = await mammoth.convertToHtml({ arrayBuffer });
    const html = result.value || "";
    return turndownService.turndown(html).trim();
  } catch (err: any) {
    throw new Error(`Word faylni o'qishda xatolik: ${err.message || err}`);
  }
}

// 3. PowerPoint (.pptx) Conversion
export async function pptxToMarkdown(arrayBuffer: ArrayBuffer): Promise<{ totalSlides: number; slides: { slideNum: number; title: string; bullets: string[] }[] }> {
  try {
    const zip = await JSZip.loadAsync(arrayBuffer);
    const slideFiles = Object.keys(zip.files)
      .filter((name) => /^ppt\/slides\/slide\d+\.xml$/i.test(name))
      .sort((a, b) => {
        const numA = parseInt(a.match(/\d+/)![0], 10);
        const numB = parseInt(b.match(/\d+/)![0], 10);
        return numA - numB;
      });

    const slides: { slideNum: number; title: string; bullets: string[] }[] = [];
    let slideIndex = 1;

    for (const slidePath of slideFiles) {
      const xml = await zip.files[slidePath].async("text");
      const textMatches = xml.match(/<a:t[^>]*>(.*?)<\/a:t>/g) || [];
      const slideTexts = textMatches
        .map((m) => normalizeText(m.replace(/<[^>]+>/g, "").trim()))
        .filter((t) => t.length > 0);

      const title = slideTexts.length > 0 ? slideTexts[0] : `Slayd ${slideIndex}`;
      const bullets = slideTexts.length > 1 ? slideTexts.slice(1) : [];
      slides.push({ slideNum: slideIndex, title, bullets });
      slideIndex++;
    }

    return { totalSlides: slides.length, slides };
  } catch (err: any) {
    throw new Error(`PowerPoint faylni o'qishda xatolik: ${err.message || err}`);
  }
}

// 4. Excel (.xlsx / .xls) to Markdown
export function xlsxToMarkdown(arrayBuffer: ArrayBuffer, fileName: string): string {
  const workbook = XLSX.read(arrayBuffer, { type: "array" });
  const sheetCount = workbook.SheetNames.length;
  const cleanTitle = fileName.replace(/\.[^/.]+$/, "");

  let md = `# 📄 ${cleanTitle}\n\n`;
  md += `> 📌 **Hujjat:** \`${fileName}\` | **Sahifalar:** ${sheetCount} ta | **Format:** Excel\n\n`;
  md += `---\n\n`;

  for (let i = 0; i < sheetCount; i++) {
    const sheetName = workbook.SheetNames[i];
    const sheet = workbook.Sheets[sheetName];
    const csv = XLSX.utils.sheet_to_csv(sheet);
    if (!csv.trim()) continue;

    if (sheetCount > 1) {
      md += `## Sahifa: ${sheetName}\n\n`;
    }
    md += csvToMarkdown(csv, ",") + "\n\n";

    if (i < sheetCount - 1) {
      md += `---\n\n`;
    }
  }

  return md.trim();
}

// 5. CSV / TSV to Markdown Table
export function csvToMarkdown(content: string, delimiter: string = ","): string {
  const lines = content.split(/\r?\n/).filter((l) => l.trim().length > 0);
  if (lines.length === 0) return "";

  const parseLine = (line: string): string[] => {
    const row: string[] = [];
    let insideQuote = false;
    let current = "";
    for (let i = 0; i < line.length; i++) {
      const char = line[i];
      if (char === '"' || char === "'") {
        insideQuote = !insideQuote;
      } else if (char === delimiter && !insideQuote) {
        row.push(normalizeText(current.trim()));
        current = "";
      } else {
        current += char;
      }
    }
    row.push(normalizeText(current.trim()));
    return row.map((cell) => cell.replace(/^["']|["']$/g, "").replace(/\|/g, "\\|"));
  };

  const rows = lines.map(parseLine);
  if (rows.length === 0) return "";

  const maxCols = Math.max(...rows.map((r) => r.length));
  const normalizedRows = rows.map((r) => {
    while (r.length < maxCols) r.push("");
    return r;
  });

  const header = normalizedRows[0];
  const separator = header.map(() => "---");
  const dataRows = normalizedRows.slice(1);

  let md = `| ${header.join(" | ")} |\n| ${separator.join(" | ")} |\n`;
  for (const row of dataRows) {
    md += `| ${row.join(" | ")} |\n`;
  }
  return md;
}

// Helper: Convert File/Blob to Base64
export function fileToBase64(file: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      const base64 = result.split(",")[1] || result;
      resolve(base64);
    };
    reader.onerror = reject;
    reader.readAsDataURL(file);
  });
}

// 6. Gemini Multimodal AI Call with Auto-Fallback
export async function convertWithGeminiApi(
  base64Data: string,
  mimeType: string,
  filename: string,
  apiKey: string,
  modelName: string = "gemini-2.5-flash",
  customPrompt?: string
): Promise<{ markdown: string; tokensConsumed: number }> {
  if (!apiKey) {
    throw new Error("Gemini AI orqali OCR uchun API kalit talab qilinadi.");
  }

  const systemInstruction = `Siz Microsoft MarkItDown tamoyillari asosida ishlovchi universal fayl konvertatsiya tizimisiz.
Vazifangiz: berilgan fayldagi barcha ma'lumotlarni, matnlarni (jumladan Krill va Lotin harflari: қ, ғ, ҳ, ў), jadvallarni va audio ovozlarni toza, tushunarli, chiroyli Markdown (.md) formatiga aylantirish.
Qoidalar:
1. Rasm yoki skrinshot bo'lsa (OCR): Barcha ko'rinib turgan matnlar, sarlavhalar va jadvallarni aniq o'qib, tartibli Markdown ko'rinishida yozib bering.
2. Jadvallar bo'lsa: Har doim toza Markdown jadvali (| Ustun 1 | Ustun 2 |) shaklida ifodalang.
3. Faqat toza Markdown qaytaring.
${customPrompt ? `Maxsus talab: ${customPrompt}` : ""}`;

  const promptText = `Ushbu "${filename}" (${mimeType}) faylidagi barcha matnlarni to'liq ajratib olib, toza Markdown formatiga o'tkazib ber.`;

  const makeCall = async (model: string) => {
    const response = await fetch(
      `https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent?key=${apiKey}`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          system_instruction: { parts: [{ text: systemInstruction }] },
          contents: [
            {
              parts: [
                { inline_data: { mime_type: mimeType, data: base64Data } },
                { text: promptText },
              ],
            },
          ],
          generationConfig: { temperature: 0.1 },
        }),
      }
    );

    const data = await response.json();
    if (!response.ok || data.error) {
      throw new Error(data.error?.message || "Gemini AI xizmatida xatolik yuz berdi.");
    }

    let rawMd = data.candidates?.[0]?.content?.parts?.[0]?.text || "";
    if (rawMd.startsWith("```markdown\n") && rawMd.endsWith("\n```")) {
      rawMd = rawMd.slice(12, -4);
    } else if (rawMd.startsWith("```md\n") && rawMd.endsWith("\n```")) {
      rawMd = rawMd.slice(6, -4);
    }

    const tokenUsage = data.usageMetadata?.totalTokenCount || estimateTokens(rawMd);
    return { markdown: rawMd.trim(), tokensConsumed: tokenUsage };
  };

  try {
    return await makeCall(modelName);
  } catch (err: any) {
    // Smart Fallback
    const fallbackModel = modelName === "gemini-2.5-pro" ? "gemini-2.5-flash" : "gemini-2.5-flash-lite";
    console.warn(`[Fallback] ${modelName} xatosi. ${fallbackModel} modeliga o'tilmoqda...`);
    return await makeCall(fallbackModel);
  }
}

// 7. OpenAI / Groq / DeepSeek / Custom AI Call
export async function convertWithOpenAiApi(
  base64Data: string,
  mimeType: string,
  filename: string,
  apiKey: string,
  endpoint: string,
  modelName: string = "llama-3.3-70b-versatile",
  customPrompt?: string
): Promise<{ markdown: string; tokensConsumed: number }> {
  if (!apiKey) {
    throw new Error("AI API kaliti kiritilmagan.");
  }

  const systemInstruction = `Siz universal fayl konvertatsiya tizimisiz. Tasvirdagi barcha matnlar va jadvallarni toza Markdown ko'rinishida yozib bering.`;
  const dataUrl = `data:${mimeType};base64,${base64Data}`;

  const response = await fetch(endpoint, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${apiKey}`,
    },
    body: JSON.stringify({
      model: modelName,
      messages: [
        { role: "system", content: systemInstruction },
        {
          role: "user",
          content: [
            { type: "text", text: `Ushbu "${filename}" fayldagi barcha matnlarni (jumladan Krill va Lotin harflari: қ, ғ, ҳ, ў) toza Markdown formatida yozib ber.` },
            { type: "image_url", image_url: { url: dataUrl } },
          ],
        },
      ],
      temperature: 0.1,
    }),
  });

  const data = await response.json();
  if (!response.ok || data.error) {
    throw new Error(data.error?.message || "AI so'rovida xatolik.");
  }

  let rawMd = data.choices?.[0]?.message?.content || "";
  if (rawMd.startsWith("```markdown\n") && rawMd.endsWith("\n```")) {
    rawMd = rawMd.slice(12, -4);
  }
  const tokenUsage = data.usage?.total_tokens || estimateTokens(rawMd);
  return { markdown: rawMd.trim(), tokensConsumed: tokenUsage };
}

// 8. Main Browser File Conversion Dispatcher without Mundarija
export async function convertFileClient(
  file: File,
  options: ConversionOptions = {},
  apiKey?: string,
  provider: string = "GoogleGemini",
  modelName: string = "gemini-2.5-flash",
  customBaseUrl?: string
): Promise<ConvertedItem[]> {
  const startTime = Date.now();
  const ext = (file.name.split(".").pop() || "").toLowerCase();
  const cleanTitle = file.name.replace(/\.[^/.]+$/, "");
  const hasApiKey = Boolean(apiKey && apiKey.trim().length > 0);

  let markdown = "";
  let usedAi = false;
  let tokensConsumed = 0;
  let engine = "MarkItDown Browser Engine";

  const isImageOrAudio =
    file.type.startsWith("image/") ||
    file.type.startsWith("audio/") ||
    ["png", "jpg", "jpeg", "webp", "gif", "svg", "mp3", "wav", "m4a", "ogg"].includes(ext);

  if (isImageOrAudio) {
    const isAudio = ["mp3", "wav", "m4a", "ogg"].includes(ext);
    if (hasApiKey) {
      usedAi = true;
      engine = `${provider} (${modelName})`;
      const base64 = await fileToBase64(file);
      const effectiveMime = file.type || (isAudio ? `audio/${ext}` : `image/${ext}`);

      let aiResult: { markdown: string; tokensConsumed: number };
      if (provider === "GoogleGemini") {
        aiResult = await convertWithGeminiApi(base64, effectiveMime, file.name, apiKey!, modelName, options.customPrompt);
      } else if (provider === "GroqAI") {
        aiResult = await convertWithOpenAiApi(base64, effectiveMime, file.name, apiKey!, "https://api.groq.com/openai/v1/chat/completions", modelName, options.customPrompt);
      } else if (provider === "DeepSeek") {
        aiResult = await convertWithOpenAiApi(base64, effectiveMime, file.name, apiKey!, "https://api.deepseek.com/v1/chat/completions", modelName, options.customPrompt);
      } else if (provider === "OpenAI") {
        aiResult = await convertWithOpenAiApi(base64, effectiveMime, file.name, apiKey!, "https://api.openai.com/v1/chat/completions", modelName, options.customPrompt);
      } else {
        aiResult = await convertWithOpenAiApi(base64, effectiveMime, file.name, apiKey!, customBaseUrl || "http://localhost:11434/v1/chat/completions", modelName, options.customPrompt);
      }

      tokensConsumed = aiResult.tokensConsumed;

      markdown = `# 📄 ${cleanTitle}\n\n> 📌 **${isAudio ? "Audio" : "Tasvir"}:** \`${file.name}\` | **Format:** ${ext.toUpperCase()}\n\n`;
      if (!isAudio) markdown += `![${file.name}](${file.name})\n\n`;
      markdown += `> 🤖 **[AI OCR / ${isAudio ? "Audio Transkripsiya" : "Tasvir Tahlili"}]** *(Ushbu qism \`${provider}\` - \`${modelName}\` modeli yordamida tayyorlandi, tekshirib ko'ring)*:\n>\n` +
        aiResult.markdown.split("\n").map((l) => `> ${l}`).join("\n");
    } else {
      markdown = `# 📄 ${cleanTitle}\n\n> 📌 **${isAudio ? "Audio" : "Tasvir"}:** \`${file.name}\` | **Format:** ${ext.toUpperCase()}\n\n`;
      if (!isAudio) markdown += `![${file.name}](${file.name})\n\n`;
      markdown += `> ⚠️ *(Ushbu rasm/audio yuklandi. AI API kaliti ulanmagani sababli matn ajratib olinmadi)*\n`;
    }
  } else if (ext === "pdf") {
    const arrayBuffer = await file.arrayBuffer();
    const { totalPages, pages } = await pdfToMarkdown(arrayBuffer);
    const isScannedPdf = pages.every((p) => p.text.length < 30);

    let docBody = "";

    if (isScannedPdf) {
      if (hasApiKey) {
        usedAi = true;
        engine = `${provider} Vision AI (${modelName})`;
        const base64 = await fileToBase64(file);

        if (provider === "GoogleGemini") {
          const aiResult = await convertWithGeminiApi(base64, "application/pdf", file.name, apiKey!, modelName, options.customPrompt);
          docBody = aiResult.markdown;
          tokensConsumed = aiResult.tokensConsumed;
        } else {
          const aiResult = await convertWithOpenAiApi(base64, "image/png", file.name, apiKey!, provider === "GroqAI" ? "https://api.groq.com/openai/v1/chat/completions" : "https://api.openai.com/v1/chat/completions", modelName, options.customPrompt);
          docBody = aiResult.markdown;
          tokensConsumed = aiResult.tokensConsumed;
        }
      } else {
        for (const p of pages) {
          docBody += `## Sahifa ${p.pageNum}\n\n`;
          docBody += `![page_${p.pageNum}.png](page_${p.pageNum}.png)\n`;
          docBody += `> ⚠️ *(Ushbu rasm saqlandi. AI API kaliti ulanmagani sababli rasmdagi matn ajratib olinmadi)*\n\n---\n\n`;
        }
      }
    } else {
      for (let i = 0; i < pages.length; i++) {
        const p = pages[i];
        if (totalPages > 1) {
          docBody += `## Sahifa ${p.pageNum}\n\n`;
        }
        if (p.text) {
          docBody += p.text + "\n\n";
        }
        if (p.hasImages) {
          if (hasApiKey) {
            docBody += `> 🤖 **[AI OCR / Tasvir Tahlili]** *(Ushbu qism \`${provider}\` - \`${modelName}\` modeli yordamida tekshirildi)*\n\n`;
          } else {
            docBody += `> ⚠️ *(Ushbu sahifada rasm mavjud. AI API kaliti ulanmagani sababli rasmdagi matn ajratib olinmadi)*\n\n`;
          }
        }
        if (totalPages > 1 && i < pages.length - 1) {
          docBody += `---\n\n`;
        }
      }
    }

    markdown = `# 📄 ${cleanTitle}\n\n> 📌 **Hujjat:** \`${file.name}\` | **Sahifalar:** ${totalPages} ta | **Format:** PDF\n\n---\n\n` + docBody.trim();
  } else if (ext === "docx" || ext === "doc") {
    const arrayBuffer = await file.arrayBuffer();
    const bodyText = await docxToMarkdown(arrayBuffer);
    markdown = `# 📄 ${cleanTitle}\n\n> 📌 **Hujjat:** \`${file.name}\` | **Format:** Word (.docx)\n\n---\n\n` + bodyText;
  } else if (ext === "xlsx" || ext === "xls" || ext === "ods") {
    const arrayBuffer = await file.arrayBuffer();
    markdown = xlsxToMarkdown(arrayBuffer, file.name);
  } else if (ext === "pptx" || ext === "ppt") {
    const arrayBuffer = await file.arrayBuffer();
    const { totalSlides, slides } = await pptxToMarkdown(arrayBuffer);
    let pptMd = `# 📄 ${cleanTitle}\n\n> 📌 **Hujjat:** \`${file.name}\` | **Slaydlar:** ${totalSlides} ta | **Format:** PowerPoint (.pptx)\n\n---\n\n`;
    for (let i = 0; i < slides.length; i++) {
      const s = slides[i];
      pptMd += `## Slayd ${s.slideNum}: ${s.title}\n\n`;
      for (const b of s.bullets) {
        pptMd += `- ${b}\n`;
      }
      pptMd += `\n`;
      if (i < slides.length - 1) pptMd += `---\n\n`;
    }
    markdown = pptMd.trim();
  } else if (ext === "csv" || ext === "tsv") {
    const text = await file.text();
    const tableMd = csvToMarkdown(text, ext === "tsv" ? "\t" : ",");
    markdown = `# 📄 ${cleanTitle}\n\n> 📌 **Hujjat:** \`${file.name}\` | **Format:** ${ext.toUpperCase()}\n\n---\n\n` + tableMd;
  } else {
    const text = await file.text();
    markdown = `# 📄 ${cleanTitle}\n\n> 📌 **Hujjat:** \`${file.name}\` | **Format:** ${ext.toUpperCase()}\n\n---\n\n` + "```" + ext + "\n" + text + "\n```";
  }

  const durationMs = Date.now() - startTime;
  const wordCount = countWords(markdown);
  const charCount = markdown.length;
  const lineCount = markdown.split("\n").length;
  const estimatedTokens = estimateTokens(markdown);

  return [
    {
      id: `conv_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`,
      filename: file.name,
      originalFormat: ext.toUpperCase() || "UNKNOWN",
      originalSize: file.size,
      markdown,
      markdownSize: new Blob([markdown]).size,
      wordCount,
      charCount,
      lineCount,
      estimatedTokens,
      durationMs,
      usedAi,
      tokensConsumed,
      engine,
      status: "success",
    },
  ];
}

// 9. Convert URL via r.jina.ai
export async function convertUrlClient(
  url: string,
  options: ConversionOptions = {},
  apiKey?: string
): Promise<ConvertedItem> {
  const startTime = Date.now();
  let markdown = "";
  let title = url;

  try {
    const jinaUrl = `https://r.jina.ai/${url}`;
    const res = await fetch(jinaUrl, { headers: { Accept: "text/markdown" } });

    if (res.ok) {
      markdown = await res.text();
      const titleMatch = markdown.match(/^Title:\s*(.*)$/m) || markdown.match(/^#\s+(.*)$/m);
      if (titleMatch) title = titleMatch[1].trim();
    } else {
      throw new Error(`Jina error: ${res.statusText}`);
    }
  } catch (err: any) {
    const proxyRes = await fetch(`https://api.allorigins.win/raw?url=${encodeURIComponent(url)}`);
    const html = await proxyRes.text();
    const titleMatch = html.match(/<title[^>]*>(.*?)<\/title>/i);
    title = titleMatch ? titleMatch[1].trim() : url;
    markdown = turndownService.turndown(html);
  }

  const cleanMd = `# 🌐 ${title}\n\n> 📌 **Manba:** [${url}](${url})\n\n---\n\n${markdown.trim()}`;
  const durationMs = Date.now() - startTime;

  return {
    id: `url_${Date.now()}`,
    filename: title,
    originalFormat: "URL",
    originalSize: new Blob([cleanMd]).size,
    markdown: cleanMd,
    markdownSize: new Blob([cleanMd]).size,
    wordCount: countWords(cleanMd),
    charCount: cleanMd.length,
    lineCount: cleanMd.split("\n").length,
    estimatedTokens: estimateTokens(cleanMd),
    durationMs,
    usedAi: false,
    engine: "MarkItDown Web Reader",
    sourceUrl: url,
    status: "success",
  };
}
