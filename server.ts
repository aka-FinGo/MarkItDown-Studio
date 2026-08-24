import express from "express";
import path from "path";
import multer from "multer";
import { createServer as createViteServer } from "vite";
import {
  convertFile,
  convertUrlToMarkdown,
  convertZipArchive,
  ConversionOptions,
  countWords,
  estimateTokens,
} from "./server/converter.js";

const app = express();
const PORT = 3000;

// Setup multer for in-memory file uploads (up to 50MB)
const upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: 50 * 1024 * 1024 }, // 50MB
});

// Middleware for parsing JSON and URL-encoded bodies (large limits for raw text and base64)
app.use(express.json({ limit: "50mb" }));
app.use(express.urlencoded({ extended: true, limit: "50mb" }));

function formatErrorMessage(error: any): string {
  if (!error) return "Faylni konvertatsiya qilishda xatolik yuz berdi.";
  let msg = typeof error === "string" ? error : error.message || "";
  try {
    const parsed = JSON.parse(msg);
    if (parsed.error?.message) {
      msg = parsed.error.message;
    }
  } catch {}

  if (msg.includes("503") || msg.includes("high demand") || msg.includes("UNAVAILABLE")) {
    return "Sun'iy intellekt modeli ayni paytda yuqori talab tufayli band (503). Tizim avtomatik qayta ulandi, iltimos 2-3 soniyadan so'ng yana bir bor urinib ko'ring.";
  }
  if (msg.includes("429") || msg.includes("RESOURCE_EXHAUSTED")) {
    return "So'rovlar soni me'yoridan oshdi (429). Iltimos, birozdan so'ng qayta urinib ko'ring.";
  }
  return msg || "Faylni o'girishda xatolik yuz berdi.";
}

// --- API ROUTES ---

// Health check
app.get("/api/health", (req, res) => {
  res.json({
    status: "ok",
    service: "MarkItDown Studio",
    version: "1.0.0",
    engine: "Microsoft MarkItDown + Gemini Multimodal AI",
  });
});

// Supported formats info
app.get("/api/supported-formats", (req, res) => {
  res.json({
    categories: [
      {
        name: "Documents",
        formats: ["PDF (.pdf)", "Word (.docx, .doc)", "PowerPoint (.pptx, .ppt)", "Rich Text (.rtf, .odt)"],
        capabilities: ["Text & layout preservation", "Headings & lists", "Table extraction", "OCR for scans", "Math LaTeX formulas"],
      },
      {
        name: "Spreadsheets & Tables",
        formats: ["Excel (.xlsx, .xls)", "CSV (.csv)", "TSV (.tsv)", "ODS (.ods)"],
        capabilities: ["Multi-sheet tabs", "GFM table formatting", "Header detection", "Cell data alignment"],
      },
      {
        name: "Images & Scans (OCR & Multimodal)",
        formats: ["PNG (.png)", "JPEG (.jpg, .jpeg)", "WEBP (.webp)", "SVG (.svg)", "GIF (.gif)"],
        capabilities: ["Optical Character Recognition (OCR)", "Diagram & flowchart descriptions", "Chart to Markdown table data", "Receipt & invoice transcription"],
      },
      {
        name: "Audio & Speech",
        formats: ["MP3 (.mp3)", "WAV (.wav)", "M4A (.m4a)", "OGG (.ogg)", "FLAC (.flac)"],
        capabilities: ["Speech-to-text transcription", "Speaker identification", "Timestamps & structured sections"],
      },
      {
        name: "Code & Structured Data",
        formats: ["JSON (.json)", "XML (.xml)", "HTML (.html)", "YAML (.yaml)", "Code (.py, .ts, .js, .cpp, .java, .sql, .rs, .go)"],
        capabilities: ["Syntax-highlighted code fences", "JSON to GFM table formatting", "HTML boilerplate cleaning"],
      },
      {
        name: "Web & Archives",
        formats: ["Web URLs (Articles, Docs, GitHub)", "ZIP Archives (.zip)"],
        capabilities: ["Web page article extraction", "Batch recursive archive extraction"],
      },
    ],
  });
});

// Convert uploaded file(s)
app.post("/api/convert", upload.array("files", 10), async (req, res) => {
  try {
    const files = req.files as Express.Multer.File[];
    const options: ConversionOptions = {
      enableAi: req.body.enableAi === "true" || req.body.enableAi === true,
      includeFrontmatter: req.body.includeFrontmatter === "true" || req.body.includeFrontmatter === true,
      includeSummary: req.body.includeSummary === "true" || req.body.includeSummary === true,
      tableStyle: req.body.tableStyle || "standard",
      customPrompt: req.body.customPrompt || "",
    };

    if (!files || files.length === 0) {
      // Check if base64 file passed in JSON
      if (req.body.fileData && req.body.fileName) {
        const buffer = Buffer.from(req.body.fileData, "base64");
        const result = await convertFile(buffer, req.body.fileName, req.body.mimeType || "", options);
        return res.json({ success: true, results: [result] });
      }
      return res.status(400).json({ error: "No files provided for conversion." });
    }

    const results = [];
    for (const file of files) {
      const ext = (file.originalname.split(".").pop() || "").toLowerCase();
      if (ext === "zip") {
        const zipResults = await convertZipArchive(file.buffer, options);
        results.push(...zipResults);
      } else {
        const result = await convertFile(file.buffer, file.originalname, file.mimetype, options);
        results.push(result);
      }
    }

    res.json({
      success: true,
      count: results.length,
      results,
    });
  } catch (error: any) {
    console.error("Conversion error:", error);
    res.status(500).json({ error: formatErrorMessage(error) });
  }
});

// Convert Web URL to Markdown
app.post("/api/convert-url", async (req, res) => {
  try {
    const { url, enableAi, includeFrontmatter, customPrompt } = req.body;
    if (!url || typeof url !== "string" || !url.startsWith("http")) {
      return res.status(400).json({ error: "A valid HTTP/HTTPS URL is required." });
    }

    const startTime = Date.now();
    const result = await convertUrlToMarkdown(url, {
      enableAi: enableAi ?? true,
      includeFrontmatter,
      customPrompt,
    });

    let markdown = result.markdown;
    if (includeFrontmatter) {
      const frontmatter = {
        title: result.title || "Web Document",
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
    const estimatedTokens = estimateTokens(markdown);

    res.json({
      success: true,
      result: {
        id: `url_${Date.now()}`,
        filename: result.title || url,
        originalFormat: "URL",
        originalSize: Buffer.byteLength(markdown, "utf-8"),
        markdown,
        markdownSize: Buffer.byteLength(markdown, "utf-8"),
        wordCount,
        charCount: markdown.length,
        lineCount: markdown.split("\n").length,
        estimatedTokens,
        durationMs,
        usedAi: enableAi ?? true,
        sourceUrl: url,
      },
    });
  } catch (error: any) {
    console.error("URL conversion error:", error);
    res.status(500).json({ error: formatErrorMessage(error) });
  }
});

// Convert raw text / snippet
app.post("/api/convert-text", async (req, res) => {
  try {
    const { text, format, filename, enableAi, includeFrontmatter, customPrompt } = req.body;
    if (!text || typeof text !== "string") {
      return res.status(400).json({ error: "Text content is required." });
    }

    const name = filename || `snippet.${format || "txt"}`;
    const buffer = Buffer.from(text, "utf-8");
    const result = await convertFile(buffer, name, "text/plain", {
      enableAi,
      includeFrontmatter,
      customPrompt,
    });

    res.json({
      success: true,
      result,
    });
  } catch (error: any) {
    console.error("Text conversion error:", error);
    res.status(500).json({ error: formatErrorMessage(error) });
  }
});

// Vite Middleware & Static handling
async function startServer() {
  if (process.env.NODE_ENV !== "production") {
    const vite = await createViteServer({
      server: { middlewareMode: true },
      appType: "spa",
    });
    app.use(vite.middlewares);
  } else {
    const distPath = path.join(process.cwd(), "dist");
    app.use(express.static(distPath));
    app.get("*", (req, res) => {
      res.sendFile(path.join(distPath, "index.html"));
    });
  }

  app.listen(PORT, "0.0.0.0", () => {
    console.log(`MarkItDown Studio server running on http://0.0.0.0:${PORT}`);
  });
}

startServer();
