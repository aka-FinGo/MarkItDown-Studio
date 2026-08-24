export interface ConversionOptions {
  enableAi: boolean;
  includeFrontmatter: boolean;
  includeSummary?: boolean;
  tableStyle?: "standard" | "compact" | "html";
  customPrompt?: string;
}

export interface ConvertedItem {
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
  tokensConsumed?: number;
  engine?: "local" | "gemini-ai";
  frontmatter?: Record<string, any>;
  summary?: string;
  previewSnippet?: string;
  sourceUrl?: string;
  status: "idle" | "converting" | "success" | "error";
  errorMessage?: string;
}

export interface FormatCategory {
  name: string;
  formats: string[];
  capabilities: string[];
}
