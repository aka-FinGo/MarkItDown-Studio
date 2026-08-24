import * as XLSX from "xlsx";
import JSZip from "jszip";
import mammoth from "mammoth";
import TurndownService from "turndown";
import * as pdfjsLib from "pdfjs-dist";
import { ConvertedItem, ConversionOptions } from "../types";

// Configure pdfjs worker for browser compatibility
if (typeof window !== "undefined") {
  pdfjsLib.GlobalWorkerOptions.workerSrc = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${pdfjsLib.version}/pdf.worker.min.mjs`;
}

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
  const clean = text.replace(/```[\s\S]*?```/g, "").replace(/[#*_`\[\]()]/g, "");
  const words = clean.trim().match(/\S+/g);
  return words ? words.length : 0;
}

// 1. PDF Conversion in Browser (0 AI Tokens)
export async function pdfToMarkdown(arrayBuffer: ArrayBuffer): Promise<string> {
  try {
    const loadingTask = pdfjsLib.getDocument({ data: new Uint8Array(arrayBuffer) });
    const pdf = await loadingTask.promise;
    let fullText = "";

    for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {
      const page = await pdf.getPage(pageNum);
      const textContent = await page.getTextContent();
      const pageText = textContent.items
        .map((item: any) => item.str)
        .join(" ");

      if (pdf.numPages > 1) {
        fullText += `\n\n### Sahifa ${pageNum}\n\n` + pageText;
      } else {
        fullText += "\n\n" + pageText;
      }
    }

    return fullText.trim();
  } catch (err: any) {
    console.warn("PDF o'qishda xatolik:", err);
    throw new Error(`PDF faylni o'qib bo'lmadi: ${err.message || err}`);
  }
}

// 2. Word (.docx) Conversion (0 AI Tokens)
export async function docxToMarkdown(arrayBuffer: ArrayBuffer): Promise<string> {
  try {
    const result = await mammoth.convertToHtml({ arrayBuffer });
    const html = result.value || "";
    return turndownService.turndown(html).trim();
  } catch (err: any) {
    throw new Error(`Word faylni o'qishda xatolik: ${err.message || err}`);
  }
}

// 3. PowerPoint (.pptx) Conversion (0 AI Tokens)
export async function pptxToMarkdown(arrayBuffer: ArrayBuffer): Promise<string> {
  try {
    const zip = await JSZip.loadAsync(arrayBuffer);
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
    throw new Error(`PowerPoint faylni o'qishda xatolik: ${err.message || err}`);
  }
}

// 4. CSV / TSV to Markdown Table
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

// 5. Excel (.xlsx / .xls) to Markdown
export function xlsxToMarkdown(arrayBuffer: ArrayBuffer): string {
  const workbook = XLSX.read(arrayBuffer, { type: "array" });
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

// 6. JSON to Markdown Table or formatted JSON
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

// 7. Clean HTML to Markdown
export function htmlToMarkdown(html: string): string {
  try {
    const cleanHtml = html
      .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, "")
      .replace(/<style\b[^<]*(?:(?!<\/style>)<[^<]*)*<\/style>/gi, "");
    return turndownService.turndown(cleanHtml).trim();
  } catch {
    return html.replace(/<[^>]+>/g, " ").replace(/\s+/g, " ").trim();
  }
}

// 8. Code & Text to Markdown
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

// 9. Gemini Multimodal AI (OCR for Images, Speech-to-Text for Audio)
export async function convertWithGeminiApi(
  base64Data: string,
  mimeType: string,
  filename: string,
  apiKey: string,
  options: ConversionOptions = {}
): Promise<{ markdown: string; tokensConsumed: number }> {
  if (!apiKey) {
    throw new Error("Gemini AI orqali rasm/audio o'girish uchun API kalit talab qilinadi. Yuqoridagi 'API Kalit' tugmasi orqali kiriting.");
  }

  const systemInstruction = `Siz Microsoft MarkItDown tamoyillari asosida ishlovchi universal fayl konvertatsiya tizimisiz.
Vazifangiz: berilgan fayldagi barcha ma'lumotlarni, matnlarni, jadvallarni va audio ovozlarni toza, tushunarli, chiroyli Markdown (.md) formatiga aylantirish.

Qoidalar:
1. Rasm yoki skrinshot bo'lsa (OCR): Rasmdagi barcha ko'rinib turgan matnlar, yozuvlar, menyular, jadvallarni aniq o'qib, sarlavhalar va ro'yxatlar bilan toza Markdown ko'rinishida yozib bering.
2. Audio fayl bo'lsa (Ovozli xabar, suhbat): Nutqni to'liq eshitib, matnga aylantiring (Transkripsiya).
3. Jadvallar bo'lsa: Har doim toza Markdown jadvali (| Ustun 1 | Ustun 2 |) shaklida ifodalang.
4. Hech qanday boshqa kirish yoki yakuniy tushuntirish so'zlari yozmang. Faqat toza Markdown matnini qaytaring.
${options.customPrompt ? `Foydalanuvchining maxsus talabi: ${options.customPrompt}` : ""}`;

  const promptText = `Ushbu "${filename}" (${mimeType}) faylidagi barcha matnlarni / ovozni to'liq ajratib olib, toza Markdown (.md) formatiga o'tkazib ber.`;

  const response = await fetch(
    `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${apiKey}`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        system_instruction: {
          parts: [{ text: systemInstruction }],
        },
        contents: [
          {
            parts: [
              {
                inline_data: {
                  mime_type: mimeType,
                  data: base64Data,
                },
              },
              {
                text: promptText,
              },
            ],
          },
        ],
        generationConfig: {
          temperature: 0.1,
        },
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
}

// 10. Web URL Conversion in Browser (via r.jina.ai)
export async function convertUrlClient(
  url: string,
  options: ConversionOptions = {},
  apiKey?: string
): Promise<ConvertedItem> {
  const startTime = Date.now();
  let markdown = "";
  let title = url;
  let usedAi = false;
  let tokensConsumed = 0;

  try {
    // Jina Reader is a free, high-speed CORS-friendly Web-to-Markdown proxy
    const jinaUrl = `https://r.jina.ai/${url}`;
    const res = await fetch(jinaUrl, {
      headers: {
        Accept: "text/markdown",
      },
    });

    if (res.ok) {
      markdown = await res.text();
      // Extract title from markdown if available
      const titleMatch = markdown.match(/^Title:\s*(.*)$/m) || markdown.match(/^#\s+(.*)$/m);
      if (titleMatch) {
        title = titleMatch[1].trim();
      }
    } else {
      throw new Error(`Jina Reader error: ${res.statusText}`);
    }
  } catch (err: any) {
    console.warn("Jina reader failed, trying standard fallback:", err);
    // Fallback: try direct fetch or corsproxy
    try {
      const proxyRes = await fetch(`https://api.allorigins.win/raw?url=${encodeURIComponent(url)}`);
      const html = await proxyRes.text();
      const titleMatch = html.match(/<title[^>]*>(.*?)<\/title>/i);
      title = titleMatch ? titleMatch[1].trim() : url;
      markdown = htmlToMarkdown(html);
    } catch {
      throw new Error(`Web havolani o'qib bo'lmadi: ${err.message}`);
    }
  }

  if (options.includeFrontmatter) {
    const frontmatter = {
      title,
      source_url: url,
      converted_at: new Date().toISOString(),
      converter: "MarkItDown Studio Web Engine",
      word_count: countWords(markdown),
      estimated_tokens: estimateTokens(markdown),
    };
    const yaml =
      `---\n` +
      Object.entries(frontmatter)
        .map(([k, v]) => `${k}: ${typeof v === "string" ? `"${v}"` : v}`)
        .join("\n") +
      `\n---\n\n`;
    markdown = yaml + markdown;
  }

  const durationMs = Date.now() - startTime;
  const wordCount = countWords(markdown);
  const charCount = markdown.length;
  const lineCount = markdown.split("\n").length;
  const estimatedTokens = estimateTokens(markdown);

  return {
    id: `url_${Date.now()}`,
    filename: title,
    originalFormat: "URL",
    originalSize: new Blob([markdown]).size,
    markdown,
    markdownSize: new Blob([markdown]).size,
    wordCount,
    charCount,
    lineCount,
    estimatedTokens,
    durationMs,
    usedAi,
    tokensConsumed,
    engine: "local",
    sourceUrl: url,
    status: "success",
  };
}

// 11. Main Browser File Conversion Dispatcher
export async function convertFileClient(
  file: File,
  options: ConversionOptions = {},
  apiKey?: string
): Promise<ConvertedItem[]> {
  const startTime = Date.now();
  const ext = (file.name.split(".").pop() || "").toLowerCase();
  const filename = file.name;
  let markdown = "";
  let usedAi = false;
  let tokensConsumed = 0;
  let engine: "local" | "gemini-ai" = "local";

  const isImageOrAudio =
    file.type.startsWith("image/") ||
    file.type.startsWith("audio/") ||
    ["png", "jpg", "jpeg", "webp", "gif", "svg", "mp3", "wav", "m4a", "ogg"].includes(ext);

  // If ZIP archive -> process recursively
  if (ext === "zip") {
    const arrayBuffer = await file.arrayBuffer();
    const zip = await JSZip.loadAsync(arrayBuffer);
    const results: ConvertedItem[] = [];

    for (const [relativePath, zipEntry] of Object.entries(zip.files)) {
      if (zipEntry.dir) continue;
      if (relativePath.startsWith("__MACOSX/") || relativePath.startsWith(".")) continue;

      const innerBuffer = await zipEntry.async("arraybuffer");
      const innerFilename = relativePath.split("/").pop() || relativePath;
      const innerFile = new File([innerBuffer], innerFilename);
      const innerResults = await convertFileClient(innerFile, options, apiKey);
      results.push(...innerResults);
    }
    return results;
  }

  // If Image or Audio -> Gemini Multimodal AI
  if (isImageOrAudio) {
    usedAi = true;
    engine = "gemini-ai";
    const base64 = await fileToBase64(file);
    const effectiveMime = file.type || `image/${ext}`;
    const aiResult = await convertWithGeminiApi(base64, effectiveMime, filename, apiKey || "", options);
    markdown = aiResult.markdown;
    tokensConsumed = aiResult.tokensConsumed;
  } else {
    // Documents and text files (0 AI tokens, 100% local in browser)
    const arrayBuffer = await file.arrayBuffer();

    if (ext === "pdf") {
      try {
        markdown = await pdfToMarkdown(arrayBuffer);
        // If scanned image PDF with no text and user has Gemini key
        if ((!markdown || markdown.length < 20) && apiKey) {
          usedAi = true;
          engine = "gemini-ai";
          const base64 = await fileToBase64(file);
          const aiResult = await convertWithGeminiApi(base64, "application/pdf", filename, apiKey, options);
          markdown = aiResult.markdown;
          tokensConsumed = aiResult.tokensConsumed;
        }
      } catch (err: any) {
        if (apiKey) {
          usedAi = true;
          engine = "gemini-ai";
          const base64 = await fileToBase64(file);
          const aiResult = await convertWithGeminiApi(base64, "application/pdf", filename, apiKey, options);
          markdown = aiResult.markdown;
          tokensConsumed = aiResult.tokensConsumed;
        } else {
          throw err;
        }
      }
    } else if (ext === "docx" || ext === "doc") {
      markdown = await docxToMarkdown(arrayBuffer);
    } else if (ext === "pptx" || ext === "ppt") {
      markdown = await pptxToMarkdown(arrayBuffer);
    } else if (ext === "xlsx" || ext === "xls" || ext === "ods") {
      markdown = xlsxToMarkdown(arrayBuffer);
    } else if (ext === "csv") {
      const text = await file.text();
      markdown = csvToMarkdown(text, ",");
    } else if (ext === "tsv") {
      const text = await file.text();
      markdown = csvToMarkdown(text, "\t");
    } else if (ext === "json") {
      const text = await file.text();
      markdown = jsonToMarkdown(text);
    } else if (ext === "html" || ext === "htm") {
      const text = await file.text();
      markdown = htmlToMarkdown(text);
    } else if (
      ["txt", "log", "py", "js", "ts", "tsx", "jsx", "java", "c", "cpp", "h", "cs", "go", "rs", "sql", "sh", "css", "yaml", "yml", "xml", "svg"].includes(ext)
    ) {
      const text = await file.text();
      markdown = codeToMarkdown(text, ext);
    } else {
      markdown = await file.text();
    }
  }

  // Add frontmatter if requested
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

  return [
    {
      id: `conv_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`,
      filename,
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
      frontmatter,
      previewSnippet: markdown.slice(0, 300),
      status: "success",
    },
  ];
}

// 12. Convert Raw Text
export function convertTextClient(
  text: string,
  format: string,
  filename: string,
  options: ConversionOptions = {}
): ConvertedItem {
  const startTime = Date.now();
  let markdown = "";

  if (format === "json") {
    markdown = jsonToMarkdown(text);
  } else if (format === "csv") {
    markdown = csvToMarkdown(text, ",");
  } else if (format === "tsv") {
    markdown = csvToMarkdown(text, "\t");
  } else if (format === "html") {
    markdown = htmlToMarkdown(text);
  } else if (["py", "js", "ts", "tsx", "sql", "css", "yaml", "xml", "sh"].includes(format)) {
    markdown = codeToMarkdown(text, format);
  } else {
    markdown = text;
  }

  if (options.includeFrontmatter) {
    const wordCount = countWords(markdown);
    const estTokens = estimateTokens(markdown);
    const frontmatter = {
      sarlavha: filename.replace(/\.[^/.]+$/, ""),
      format: format.toUpperCase(),
      vaqt: new Date().toISOString(),
      sozlar_soni: wordCount,
      taxminiy_tokenlar: estTokens,
    };
    const yaml =
      `---\n` +
      Object.entries(frontmatter)
        .map(([k, v]) => `${k}: ${typeof v === "string" ? `"${v}"` : v}`)
        .join("\n") +
      `\n---\n\n`;
    markdown = yaml + markdown;
  }

  const durationMs = Date.now() - startTime;
  const wordCount = countWords(markdown);
  const charCount = markdown.length;
  const lineCount = markdown.split("\n").length;
  const estimatedTokens = estimateTokens(markdown);

  return {
    id: `txt_${Date.now()}`,
    filename: filename || `snippet.${format}`,
    originalFormat: format.toUpperCase(),
    originalSize: new Blob([text]).size,
    markdown,
    markdownSize: new Blob([markdown]).size,
    wordCount,
    charCount,
    lineCount,
    estimatedTokens,
    durationMs,
    usedAi: false,
    tokensConsumed: 0,
    engine: "local",
    status: "success",
  };
}
