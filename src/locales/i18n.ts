export type Language = "uz" | "en" | "ru";

export interface Translations {
  appTitle: string;
  badge: string;
  theme: string;
  language: string;
  aiProvider: string;
  model: string;
  apiKey: string;
  keySaved: string;
  keyMissing: string;
  enableAiVision: string;
  tabFiles: string;
  tabUrl: string;
  tabText: string;
  dropTitle: string;
  dropSubtitle: string;
  selectFile: string;
  urlPlaceholder: string;
  convertUrl: string;
  convertRawText: string;
  pasteClipboard: string;
  rawTextPlaceholder: string;
  convertedDocs: string;
  clearAll: string;
  aiProofread: string;
  copy: string;
  copied: string;
  saveMd: string;
  downloadZip: string;
  words: string;
  chars: string;
  engine: string;
  emptyTitle: string;
  emptySubtitle: string;
  convertingProgress: string;
  proofreadingProgress: string;
  proofreadModalTitle: string;
  proofreadModalSubtitle: string;
  proofreadPreviewLabel: string;
  cancel: string;
  applyProofread: string;
  apiDocs: string;
  supportedFormats: string;
  viewSplit: string;
  viewPreview: string;
  viewEditor: string;
  readyStatus: string;
  footerTagline: string;
}

export const TRANSLATIONS: Record<Language, Translations> = {
  uz: {
    appTitle: "MarkItDown Studio",
    badge: "v0.0.3 • Multi-AI & Win+Shift+S",
    theme: "Mavzu:",
    language: "Til:",
    aiProvider: "AI Provayder:",
    model: "Model:",
    apiKey: "API Kalit:",
    keySaved: "🔒 Kalit saqlandi",
    keyMissing: "⚠️ Kalit kiritilmagan",
    enableAiVision: "AI Vision OCR",
    tabFiles: "📁 Fayllar",
    tabUrl: "🌐 Web URL",
    tabText: "📝 Matn / Bufer",
    dropTitle: "Faylni bu yerga tashlang",
    dropSubtitle: "PDF, Word, Excel, PPTX, Rasm (OCR), Audio, ZIP, Kod",
    selectFile: "Fayl Tanlash...",
    urlPlaceholder: "https://uz.wikipedia.org/wiki/Markdown",
    convertUrl: "URL O'girish",
    convertRawText: "✨ Matnni Markdown (.md) ga O'tkazish",
    pasteClipboard: "📋 Buferdan Qo'yish (Ctrl+V / Win+Shift+S)",
    rawTextPlaceholder: "Bu yerga xohlagan xom matnni yozing yoki Win+Shift+S orqali rasm oling...",
    convertedDocs: "O'girilgan Hujjatlar",
    clearAll: "Barchasini Tozalash",
    aiProofread: "✨ AI Bilan Tekshirish",
    copy: "Nusxa olish",
    copied: "Nusxalandi!",
    saveMd: ".md Saqlash",
    downloadZip: "ZIP qilib yuklash",
    words: "so'z",
    chars: "belgi",
    engine: "Dvigatel",
    emptyTitle: "Markdown Ko'ruvchi Bo'sh",
    emptySubtitle: "Chap tomondan fayl yuklang yoki web URL kiriting. Natija toza Markdown formatida paydo bo'ladi.",
    convertingProgress: "O'girilmoqda",
    proofreadingProgress: "AI Markdown matnidagi xatoliklar va jadvallarni tekshirmoqda...",
    proofreadModalTitle: "✨ AI Tahlili & Xatoliklarni Tuzatish Natijasi",
    proofreadModalSubtitle: "Grammatika, jadvallar va shrift xatoliklari to'g'rilandi",
    proofreadPreviewLabel: "Tuzatilgan Markdown Ko'rinishi:",
    cancel: "Bekor Qilish",
    applyProofread: "✅ Tasdiqlash va Hujjatga Qo'llash",
    apiDocs: "API Hujjatlar",
    supportedFormats: "Formatlar",
    viewSplit: "Split",
    viewPreview: "Preview",
    viewEditor: "Editor",
    readyStatus: "Tayyor. PDF, Word, Excel, PPTX, Rasm yoki Win+Shift+S skrinshotini Ctrl+V bilan qo'ying.",
    footerTagline: "Multi-AI Auto-Fallback Arxitekturasi • 100% C# .NET 10 & Web",
  },
  en: {
    appTitle: "MarkItDown Studio",
    badge: "v0.0.3 • Multi-AI & Win+Shift+S",
    theme: "Theme:",
    language: "Lang:",
    aiProvider: "AI Provider:",
    model: "Model:",
    apiKey: "API Key:",
    keySaved: "🔒 Key saved",
    keyMissing: "⚠️ Key missing",
    enableAiVision: "AI Vision OCR",
    tabFiles: "📁 Files",
    tabUrl: "🌐 Web URL",
    tabText: "📝 Text / Clipboard",
    dropTitle: "Drop your files here",
    dropSubtitle: "PDF, Word, Excel, PPTX, Images (OCR), Audio, ZIP, Code",
    selectFile: "Select Files...",
    urlPlaceholder: "https://en.wikipedia.org/wiki/Markdown",
    convertUrl: "Convert URL",
    convertRawText: "✨ Convert Text to Markdown (.md)",
    pasteClipboard: "📋 Paste from Clipboard (Ctrl+V / Win+Shift+S)",
    rawTextPlaceholder: "Paste any raw text here or take a screenshot with Win+Shift+S...",
    convertedDocs: "Converted Documents",
    clearAll: "Clear All",
    aiProofread: "✨ AI Proofread",
    copy: "Copy",
    copied: "Copied!",
    saveMd: "Save .md",
    downloadZip: "Download All ZIP",
    words: "words",
    chars: "chars",
    engine: "Engine",
    emptyTitle: "Markdown Viewer is Empty",
    emptySubtitle: "Upload a file on the left or enter a web URL. The clean Markdown output will appear here.",
    convertingProgress: "Converting",
    proofreadingProgress: "AI is checking Markdown grammar, tables and formatting...",
    proofreadModalTitle: "✨ AI Analysis & Error Correction Result",
    proofreadModalSubtitle: "Grammar, broken tables, and glyph errors fixed",
    proofreadPreviewLabel: "Corrected Markdown Preview:",
    cancel: "Cancel",
    applyProofread: "✅ Apply to Document",
    apiDocs: "API Docs",
    supportedFormats: "Formats",
    viewSplit: "Split",
    viewPreview: "Preview",
    viewEditor: "Editor",
    readyStatus: "Ready. Upload PDF, Word, Excel, PPTX, Image or press Ctrl+V for Screenshot OCR.",
    footerTagline: "Multi-AI Auto-Fallback Architecture • 100% C# .NET 10 & Web",
  },
  ru: {
    appTitle: "MarkItDown Studio",
    badge: "v0.0.3 • Мульти-ИИ и Win+Shift+S",
    theme: "Тема:",
    language: "Язык:",
    aiProvider: "ИИ Провайдер:",
    model: "Модель:",
    apiKey: "API Ключ:",
    keySaved: "🔒 Ключ сохранен",
    keyMissing: "⚠️ Ключ не введен",
    enableAiVision: "AI Vision OCR",
    tabFiles: "📁 Файлы",
    tabUrl: "🌐 Web URL",
    tabText: "📝 Текст / Буфер",
    dropTitle: "Перетащите файлы сюда",
    dropSubtitle: "PDF, Word, Excel, PPTX, Изображения (OCR), Аудио, ZIP, Код",
    selectFile: "Выбрать файлы...",
    urlPlaceholder: "https://ru.wikipedia.org/wiki/Markdown",
    convertUrl: "Конвертировать URL",
    convertRawText: "✨ Преобразовать текст в Markdown (.md)",
    pasteClipboard: "📋 Вставить из буфера (Ctrl+V / Win+Shift+S)",
    rawTextPlaceholder: "Вставьте сюда любой текст или сделайте скриншот через Win+Shift+S...",
    convertedDocs: "Конвертированные файлы",
    clearAll: "Очистить всё",
    aiProofread: "✨ Проверить с ИИ",
    copy: "Копировать",
    copied: "Скопировано!",
    saveMd: "Сохранить .md",
    downloadZip: "Скачать всё в ZIP",
    words: "слов",
    chars: "симв.",
    engine: "Движок",
    emptyTitle: "Просмотрщик Markdown пуст",
    emptySubtitle: "Загрузите файл слева или введите web URL. Результат в формате Markdown появится здесь.",
    convertingProgress: "Конвертация",
    proofreadingProgress: "ИИ проверяет орфографию, таблицы и форматирование...",
    proofreadModalTitle: "✨ Результат анализа и исправления ИИ",
    proofreadModalSubtitle: "Орфография, таблицы и кодировка исправлены",
    proofreadPreviewLabel: "Исправленный текст Markdown:",
    cancel: "Отмена",
    applyProofread: "✅ Применить к документу",
    apiDocs: "API Документация",
    supportedFormats: "Форматы",
    viewSplit: "Раздельно",
    viewPreview: "Просмотр",
    viewEditor: "Редактор",
    readyStatus: "Готово. Загрузите файл или нажмите Ctrl+V для OCR скриншота.",
    footerTagline: "Мульти-ИИ архитектура с авто-переключением • 100% C# .NET 10 и Web",
  },
};
