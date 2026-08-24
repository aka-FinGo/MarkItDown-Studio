import { GoogleGenAI } from "@google/genai";
import * as XLSX from "xlsx";
import JSZip from "jszip";
import { createRequire } from "module";
import mammoth from "mammoth";
import TurndownService from "turndown";

const require = createRequire(import.meta.url);
const pdfParse = require("pdf-parse");

// Gemini SDK
const ai = new GoogleGenAI({
  apiKey: process.env.GEMINI_API_KEY,
  httpOptions: {
    headers: {
      "User-Agent": "aistudio-build",
    },
  },
});

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

export interface ConversionOptions {
  enableAi?: boolean;
  includeFrontmatter?: boolean;
  includeSummary?: boolean;
  tableStyle?: "standard" | "compact" | "html";
  headingStyle?: "atx" | "setext";
  extractImagesAsDescriptions?: boolean;
  customPrompt?: string;
}

export interface ConversionResult {
  id: string;
  filename: string;
  originalFormat: string;
  originalSize: number;
  markdown: string;
  markdownSize: number;
  wordCount: number;
  charCount: number;
  lineCount: number;
  estimatedTokens: number;
  durationMs: number;
  usedAi: boolean;
  tokensConsumed: number;
  engine: "local" | "gemini-ai";
  frontmatter?: Record<string, any>;
  summary?: string;
  previewSnippet?: string;
}

export function estimateTokens(text: string): number {
  return Math.ceil(text.length / 3.8);
}

export function countWords(text: string): number {
  const clean = text.replace(/```[\s\S]*?```/g, "").replace(/[#*_`\[\]()]/g, "");
  const words = clean.trim().match(/\S+/g);
  return words ? words.length : 0;
}

// 1. PDF Local Conversion (0 AI Tokens)
export async function pdfToMarkdown(buffer: Buffer): Promise<string> {
  try {
    const data = await pdfParse(buffer);
    const text = data.text || "";
    
    const lines = text.split(/\r?\n/);
    const cleanedLines: string[] = [];
    let prevEmpty = false;

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i].trim();
      if (!line) {
        if (!prevEmpty) {
          cleanedLines.push("");
          prevEmpty = true;
        }
        continue;
      }
      prevEmpty = false;

      if (line.length < 60 && !/[.,;:!?]$/.test(line) && (line === line.toUpperCase() && /[A-Z]/.test(line))) {
        cleanedLines.push(`\n## ${line}\n`);
      } else {
        cleanedLines.push(line);
      }
    }

    let md = cleanedLines.join("\n").replace(/\n{3,}/g, "\n\n").trim();
    return md;
  } catch (err: any) {
    throw new Error(`PDF o'qishda xatolik: ${err.message}`);
  }
}

// 2. DOCX Local Conversion (0 AI Tokens)
export async function docxToMarkdown(buffer: Buffer): Promise<string> {
  try {
    const result = await mammoth.convertToHtml({ buffer });
    const html = result.value || "";
    let md = turndownService.turndown(html);
    return md.trim();
  } catch (err: any) {
    throw new Error(`Word faylni o'qishda xatolik: ${err.message}`);
  }
}

// 3. PPTX Local Conversion (0 AI Tokens)
export async function pptxToMarkdown(buffer: Buffer): Promise<string> {
  try {
    const zip = await JSZip.loadAsync(buffer);
    let md = `# Taqdimot Slaydlari\n\n`;
    let slideIndex = 1;

    const slideFiles = Object.keys(zip.files)
      .filter((name) => /^ppt\/slides\/slide\d+\.xml$/i.test(name))
      .sort((a, b) => {
        const numA = parseInt(a.match(/\d+/)![0], 10);
        const numB = parseInt(b.match(/\d+/)![0], 10);
        return numA - numB;
      });

    if (slideFiles.length === 0) {
      return `> Taqdimot ichida o'qiladigan slayd matni topilmadi.`;
    }

    for (const slidePath of slideFiles) {
      const xml = await zip.files[slidePath].async("text");
      const textMatches = xml.match(/<a:t[^>]*>(.*?)<\/a:t>/g) || [];
      const slideTexts = textMatches
        .map((m) => m.replace(/<[^>]+>/g, "").trim())
        .filter((t) => t.length > 0);

      if (slideTexts.length > 0) {
        md += `## Slayd ${slideIndex}: ${slideTexts[0]}\n\n`;
        const bulletPoints = slideTexts.slice(1);
        for (const pt of bulletPoints) {
          md += `- ${pt}\n`;
        }
        md += `\n---\n\n`;
      }
      slideIndex++;
    }

    return md.trim();
  } catch (err: any) {
    throw new Error(`PowerPoint faylni o'qishda xatolik: ${err.message}`);
  }
}

// 4. CSV/TSV to GFM Markdown table (0 AI Tokens)
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
        row.push(current.trim());
        current = "";
      } else {
        current += char;
      }
    }
    row.push(current.trim());
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

// 5. Convert XLSX buffer to Markdown (0 AI Tokens)
export function xlsxToMarkdown(buffer: Buffer): string {
  const workbook = XLSX.read(buffer, { type: "buffer" });
  let fullMd = "";

  for (const sheetName of workbook.SheetNames) {
    const sheet = workbook.Sheets[sheetName];
    const csv = XLSX.utils.sheet_to_csv(sheet);
    if (!csv.trim()) continue;

    if (workbook.SheetNames.length > 1) {
      fullMd += `## Sahifa: ${sheetName}\n\n`;
    }
    fullMd += csvToMarkdown(csv, ",") + "\n\n";
  }

  return fullMd.trim();
}

// 6. Convert JSON to Markdown (0 AI Tokens)
export function jsonToMarkdown(content: string): string {
  try {
    const parsed = JSON.parse(content);
    if (Array.isArray(parsed) && parsed.length > 0 && typeof parsed[0] === "object" && parsed[0] !== null) {
      const keys = Array.from(new Set(parsed.flatMap((item) => Object.keys(item))));
      let md = `| ${keys.join(" | ")} |\n| ${keys.map(() => "---").join(" | ")} |\n`;
      for (const item of parsed) {
        const row = keys.map((k) => {
          const val = item[k];
          if (val === undefined || val === null) return "";
          if (typeof val === "object") return JSON.stringify(val).replace(/\|/g, "\\|");
          return String(val).replace(/\|/g, "\\|");
        });
        md += `| ${row.join(" | ")} |\n`;
      }
      return md;
    }

    return "```json\n" + JSON.stringify(parsed, null, 2) + "\n```";
  } catch {
    return "```json\n" + content + "\n```";
  }
}

// 7. Clean HTML to Markdown using Turndown (0 AI Tokens)
export function htmlToMarkdownSimple(html: string): string {
  try {
    const cleanHtml = html
      .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, "")
      .replace(/<style\b[^<]*(?:(?!<\/style>)<[^<]*)*<\/style>/gi, "");
    return turndownService.turndown(cleanHtml).trim();
  } catch {
    return html
      .replace(/<[^>]+>/g, " ")
      .replace(/\s+/g, " ")
      .trim();
  }
}

// 8. Convert code/plain text to Markdown (0 AI Tokens)
export function codeToMarkdown(code: string, extension: string): string {
  const extMap: Record<string, string> = {
    ts: "typescript",
    tsx: "tsx",
    js: "javascript",
    jsx: "jsx",
    py: "python",
    java: "java",
    cpp: "cpp",
    c: "c",
    cs: "csharp",
    go: "go",
    rs: "rust",
    sql: "sql",
    sh: "bash",
    bash: "bash",
    css: "css",
    scss: "scss",
    html: "html",
    xml: "xml",
    yaml: "yaml",
    yml: "yaml",
    json: "json",
    md: "markdown",
    txt: "text",
    log: "log",
  };
  const lang = extMap[extension.toLowerCase()] || "";
  return "```" + lang + "\n" + code + "\n```";
}

// Resilient Gemini call with fast model fallbacks and retry
const CANDIDATE_MODELS = [
  "gemini-2.5-flash",
  "gemini-3.7-flash",
  "gemini-2.5-flash-lite",
  "gemini-2.5-pro",
];

async function generateGeminiWithResilience(params: {
  contents: any;
  config?: any;
}): Promise<{ response: any; usedModel: string }> {
  const wait = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));
  let lastError: any = null;

  for (const model of CANDIDATE_MODELS) {
    try {
      const response = await ai.models.generateContent({
        model,
        contents: params.contents,
        config: params.config,
      });

      if (response && response.text) {
        return { response, usedModel: model };
      }
    } catch (err: any) {
      lastError = err;
      const errMsg = err?.message || JSON.stringify(err);
      console.warn(`[Gemini] Model ${model} failed (${errMsg.slice(0, 120)}), switching to next model...`);
      // Brief pause before trying fallback model
      await wait(300);
    }
  }

  throw new Error(
    lastError?.message || `Sun'iy intellekt xizmati ayni paytda band. Iltimos, bir necha soniyadan so'ng qayta urinib ko'ring.`
  );
}

// 9. Multimodal AI conversion with Gemini (OCR for Images, Speech-to-Text for Audio, Advanced Parsing)
export async function convertWithGemini(
  buffer: Buffer,
  mimeType: string,
  filename: string,
  options: ConversionOptions = {}
): Promise<{ markdown: string; tokensConsumed: number }> {
  const base64Data = buffer.toString("base64");

  const systemInstruction = `Siz Microsoft MarkItDown tamoyillari asosida ishlovchi universal fayl konvertatsiya tizimisiz.
Vazifangiz: berilgan fayldagi barcha ma'lumotlarni, matnlarni, jadvallarni va audio ovozlarni toza, tushunarli, chiroyli Markdown (.md) formatiga aylantirish.

Qoidalar:
1. Rasm yoki skrinshot bo'lsa (OCR): Rasmdagi barcha ko'rinib turgan matnlar, yozuvlar, menyular, tugmalar, maydonlar (inputlar), havolalar va jadvallarni aniq o'qib, sarlavhalar va ro'yxatlar bilan toza Markdown ko'rinishida yozib bering.
2. Audio fayl bo'lsa (Ovozli xabar, diktafon, musiqa, suhbat): Nutqni to'liq eshitib, matnga aylantiring (Transkripsiya). Agar suhbat bo'lsa, so'zlovchilarni va asosiy fikrlarni Markdown ro'yxatlari yoki sarlavhalari bilan chiroyli taqdim eting.
3. Jadvallar bo'lsa: Har doim toza Markdown jadvali (| Ustun 1 | Ustun 2 |) shaklida ifodalang.
4. Hech qanday boshqa kirish yoki yakuniy tushuntirish so'zlari yozmang. Faqat toza Markdown matnini qaytaring.
5. Chiqishni \`\`\`markdown kabi bloklarga o'rab yubormang, to'g'ridan-to'g'ri Markdown matni bo'lsin.
${options.customPrompt ? `Foydalanuvchining maxsus talabi: ${options.customPrompt}` : ""}`;

  const promptText = `Ushbu "${filename}" (${mimeType}) faylidagi barcha matnlarni / ovozni to'liq ajratib olib, toza Markdown (.md) formatiga o'tkazib ber.`;

  const { response } = await generateGeminiWithResilience({
    contents: {
      parts: [
        {
          inlineData: {
            mimeType,
            data: base64Data,
          },
        },
        {
          text: promptText,
        },
      ],
    },
    config: {
      systemInstruction,
      temperature: 0.1,
    },
  });

  let rawMd = response.text || "";

  if (rawMd.startsWith("```markdown\n") && rawMd.endsWith("\n```")) {
    rawMd = rawMd.slice(12, -4);
  } else if (rawMd.startsWith("```md\n") && rawMd.endsWith("\n```")) {
    rawMd = rawMd.slice(6, -4);
  }

  const tokenUsage = (response as any).usageMetadata?.totalTokenCount || estimateTokens(rawMd);

  return { markdown: rawMd.trim(), tokensConsumed: tokenUsage };
}

// Convert URL to Markdown
export async function convertUrlToMarkdown(
  url: string,
  options: ConversionOptions = {}
): Promise<{ markdown: string; title: string; usedAi: boolean; tokensConsumed: number }> {
  try {
    const res = await fetch(url, {
      headers: {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
      },
    });

    if (!res.ok) {
      throw new Error(`Web sahifani yuklab bo'lmadi (HTTP ${res.status}): ${res.statusText}`);
    }

    const html = await res.text();
    const titleMatch = html.match(/<title[^>]*>(.*?)<\/title>/i);
    const title = titleMatch ? titleMatch[1].trim() : url;

    if (options.enableAi) {
      const prompt = `Ushbu web sahifadagi maqola va asosiy mazmunni toza Markdown formatiga o'tkazib ber. Reklama, menyu va boshqa ortiqcha narsalarni olib tashla.\n\nURL: ${url}\n\nHTML:\n${html.slice(0, 30000)}`;

      try {
        const { response } = await generateGeminiWithResilience({
          contents: prompt,
          config: {
            systemInstruction: "Siz web maqolalarni toza Markdown formatiga o'tkazuvchi tizimsiz. Faqat toza Markdown qaytaring.",
            temperature: 0.1,
          },
        });

        let md = response.text || htmlToMarkdownSimple(html);
        if (md.startsWith("```markdown\n") && md.endsWith("\n```")) {
          md = md.slice(12, -4);
        }
        const tokens = (response as any).usageMetadata?.totalTokenCount || estimateTokens(md);
        return { markdown: md.trim(), title, usedAi: true, tokensConsumed: tokens };
      } catch (aiErr: any) {
        console.warn("AI URL conversion failed, falling back to local:", aiErr.message);
        const localMd = htmlToMarkdownSimple(html);
        return { markdown: localMd, title, usedAi: false, tokensConsumed: 0 };
      }
    } else {
      const localMd = htmlToMarkdownSimple(html);
      return { markdown: localMd, title, usedAi: false, tokensConsumed: 0 };
    }
  } catch (error: any) {
    throw new Error(`URL ni o'girishda xatolik: ${error.message}`);
  }
}

// Main conversion dispatcher
export async function convertFile(
  buffer: Buffer,
  filename: string,
  mimeType: string,
  options: ConversionOptions = {}
): Promise<ConversionResult> {
  const startTime = Date.now();
  const ext = (filename.split(".").pop() || "").toLowerCase();
  let markdown = "";
  let usedAi = false;
  let tokensConsumed = 0;
  let engine: "local" | "gemini-ai" = "local";

  let effectiveMime = mimeType;
  if (!effectiveMime || effectiveMime === "application/octet-stream") {
    const extMimeMap: Record<string, string> = {
      pdf: "application/pdf",
      docx: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
      doc: "application/msword",
      pptx: "application/vnd.openxmlformats-officedocument.presentationml.presentation",
      ppt: "application/vnd.ms-powerpoint",
      xlsx: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      xls: "application/vnd.ms-excel",
      csv: "text/csv",
      tsv: "text/tab-separated-values",
      json: "application/json",
      html: "text/html",
      htm: "text/html",
      xml: "application/xml",
      png: "image/png",
      jpg: "image/jpeg",
      jpeg: "image/jpeg",
      webp: "image/webp",
      gif: "image/gif",
      svg: "image/svg+xml",
      mp3: "audio/mp3",
      wav: "audio/wav",
      m4a: "audio/m4a",
      ogg: "audio/ogg",
      txt: "text/plain",
      py: "text/x-python",
      js: "application/javascript",
      ts: "application/typescript",
    };
    effectiveMime = extMimeMap[ext] || "text/plain";
  }

  const isImageOrAudio =
    effectiveMime.startsWith("image/") ||
    effectiveMime.startsWith("audio/") ||
    ["png", "jpg", "jpeg", "webp", "gif", "mp3", "wav", "m4a", "ogg"].includes(ext);

  // Agar rasm yoki audio bo'lsa -> Har doim Gemini AI bilan OCR / Audio transkripsiya qilinadi!
  if (isImageOrAudio) {
    usedAi = true;
    engine = "gemini-ai";
    const aiResult = await convertWithGemini(buffer, effectiveMime, filename, options);
    markdown = aiResult.markdown;
    tokensConsumed = aiResult.tokensConsumed;
  } else {
    // Matnli / hujjat fayllari (PDF, Word, Excel, CSV, JSON, Kod) -> Lokal tezkor 0-token parser
    try {
      if (ext === "pdf") {
        const parsedPdf = await pdfToMarkdown(buffer);
        // Agar PDF skanerlangan rasm bo'lsa va matn chiqmasa -> Gemini OCR ga yo'naltirish
        if (!parsedPdf || parsedPdf.length < 20) {
          usedAi = true;
          engine = "gemini-ai";
          const aiResult = await convertWithGemini(buffer, "application/pdf", filename, options);
          markdown = aiResult.markdown;
          tokensConsumed = aiResult.tokensConsumed;
        } else {
          markdown = parsedPdf;
          engine = "local";
          usedAi = false;
          tokensConsumed = 0;
        }
      } else if (ext === "docx" || ext === "doc") {
        markdown = await docxToMarkdown(buffer);
      } else if (ext === "pptx" || ext === "ppt") {
        markdown = await pptxToMarkdown(buffer);
      } else if (ext === "csv") {
        markdown = csvToMarkdown(buffer.toString("utf-8"), ",");
      } else if (ext === "tsv") {
        markdown = csvToMarkdown(buffer.toString("utf-8"), "\t");
      } else if (ext === "xlsx" || ext === "xls") {
        markdown = xlsxToMarkdown(buffer);
      } else if (ext === "json") {
        markdown = jsonToMarkdown(buffer.toString("utf-8"));
      } else if (ext === "html" || ext === "htm") {
        markdown = htmlToMarkdownSimple(buffer.toString("utf-8"));
      } else if (
        ["txt", "log", "py", "js", "ts", "tsx", "jsx", "java", "c", "cpp", "h", "cs", "go", "rs", "sql", "sh", "css", "yaml", "yml", "xml", "svg"].includes(ext)
      ) {
        markdown = codeToMarkdown(buffer.toString("utf-8"), ext);
      } else {
        markdown = buffer.toString("utf-8");
      }
    } catch (localErr: any) {
      console.warn(`Lokal o'qishda xatolik ${filename}:`, localErr.message);
      // Fallback to Gemini
      try {
        const aiResult = await convertWithGemini(buffer, effectiveMime, filename, options);
        markdown = aiResult.markdown;
        tokensConsumed = aiResult.tokensConsumed;
        usedAi = true;
        engine = "gemini-ai";
      } catch {
        markdown = buffer.length < 50000 ? buffer.toString("utf-8") : `Faylni o'qishda xatolik yuz berdi: ${localErr.message}`;
      }
    }
  }

  // Frontmatter faqat foydalanuvchi includeFrontmatter: true deb belgilagan bo'lsa qo'shiladi
  let frontmatter: Record<string, any> | undefined;
  if (options.includeFrontmatter) {
    const wordCount = countWords(markdown);
    const estTokens = estimateTokens(markdown);
    frontmatter = {
      sarlavha: filename.replace(/\.[^/.]+$/, ""),
      fayl_nomi: filename,
      format: ext.toUpperCase(),
      vaqt: new Date().toISOString(),
      dvigatel: engine === "local" ? "Lokal Dvigatel (0 Token)" : "Gemini Multimodal AI",
      ai_token_sarfi: tokensConsumed,
      sozlar_soni: wordCount,
      taxminiy_tokenlar: estTokens,
    };

    const yamlBlock =
      `---\n` +
      Object.entries(frontmatter)
        .map(([k, v]) => `${k}: ${typeof v === "string" ? `"${v}"` : v}`)
        .join("\n") +
      `\n---\n\n`;

    markdown = yamlBlock + markdown;
  }

  const durationMs = Date.now() - startTime;
  const wordCount = countWords(markdown);
  const charCount = markdown.length;
  const lineCount = markdown.split("\n").length;
  const estimatedTokens = estimateTokens(markdown);

  return {
    id: `conv_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`,
    filename,
    originalFormat: ext.toUpperCase() || "UNKNOWN",
    originalSize: buffer.length,
    markdown,
    markdownSize: Buffer.byteLength(markdown, "utf-8"),
    wordCount,
    charCount,
    lineCount,
    estimatedTokens,
    durationMs,
    usedAi,
    tokensConsumed,
    engine,
    frontmatter,
    previewSnippet: markdown.slice(0, 300),
  };
}

// Convert ZIP Archive of multiple files
export async function convertZipArchive(
  zipBuffer: Buffer,
  options: ConversionOptions = {}
): Promise<ConversionResult[]> {
  const zip = await JSZip.loadAsync(zipBuffer);
  const results: ConversionResult[] = [];

  for (const [relativePath, file] of Object.entries(zip.files)) {
    if (file.dir) continue;
    if (relativePath.startsWith("__MACOSX/") || relativePath.startsWith(".")) continue;

    const fileBuffer = await file.async("nodebuffer");
    const filename = relativePath.split("/").pop() || relativePath;
    const res = await convertFile(fileBuffer, filename, "", options);
    results.push(res);
  }

  return results;
}
