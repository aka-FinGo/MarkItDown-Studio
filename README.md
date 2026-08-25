# 📄 MarkItDown Studio (Web & Desktop .NET)

<p align="center">
  <img src="https://img.shields.io/badge/Version-v0.0.3-6366F1?style=for-the-badge&logo=github" alt="Version 0.0.3" />
  <img src="https://img.shields.io/badge/.NET-10.0_WPF-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/React-19_Vite-61DAFB?style=for-the-badge&logo=react" alt="React 19" />
  <img src="https://img.shields.io/badge/Snipping_OCR-Win+Shift+S_Integrated-10B981?style=for-the-badge&logo=windows" alt="Win+Shift+S OCR" />
  <img src="https://img.shields.io/badge/AI-Gemini_%7C_Groq_%7C_OpenAI_%7C_Claude_%7C_DeepSeek_%7C_Ollama-EC4899?style=for-the-badge&logo=google" alt="Multi-AI" />
  <img src="https://img.shields.io/badge/Languages-UZ_%7C_EN_%7C_RU-F59E0B?style=for-the-badge" alt="Trilingual" />
</p>

---

## 🌐 Live Web App & Desktop Download
- 🚀 **Web App (GitHub Pages):** [https://aka-FinGo.github.io/MarkItDown-Studio/](https://aka-FinGo.github.io/MarkItDown-Studio/)
- 💻 **Standalone Desktop .exe (Windows 10/11 x64):** [Download MarkItDownStudio.exe v0.0.3](https://github.com/aka-FinGo/MarkItDown-Studio/releases/tag/v0.0.3) *(100% Self-contained, no .NET install required!)*

---

## 🇺🇿 O'zbekcha Tavsif

**MarkItDown Studio (v0.0.3)** — Microsoft MarkItDown tamoyillariga asoslangan, har qanday hujjat, skrinshot (`Win + Shift + S`), rasm, audio va xom matnlarni 100% toza, chiroyli va tartibli Markdown (`.md`) formatiga aylantirib beruvchi universal konvertatsiya studiyasi.

### 🌟 Yangi Imkoniyatlar (v0.0.3):
1. **📸 `Win + Shift + S` Skrinshot OCR Integratsiyasi:**
   * Ekrandan `Win + Shift + S` orqali xohlagan sohani rasmga oling va MarkItDown Studio dasturida `Ctrl + V` bosing — skrinshotdagi barcha matnlar darhol OCR qilinib, toza Markdown bo'lib chiqadi!
2. **📝 "Matnni Markdown (.md) ga O'tkazish" Tugmasi va Rejimi:**
   * Xohlagan xom matn yoki konspektingizni qo'ying va bitta tugma bilan mantiqiy sarlavhalar (`#`, `##`), ro'yxatlar (`-`), ajratilgan jadvallar (`|...|`) bilan toza Markdown qiling.
3. **100% Oflayn va Bepul OCR (Internet va API kalitsiz):**
   * **Desktop:** Windows 10/11 **Windows Native OCR** dvigateli (0 MB, tezkor, Krill va Lotin harflarini aniq taniydi).
   * **Veb (GitHub Pages):** Brauzer ichida **WebAssembly (Tesseract.js)** oflayn OCR.
4. **Ko'p Provayderli Sun'iy Intellekt (Multi-AI) & Smart Fallback:**
   * **Google Gemini, Groq AI (500+ tok/s), OpenAI, Claude 3.7, DeepSeek R1/V3, Ollama**.
5. **AI Proofreader (Xatoliklarni Tekshirish & Review):**
   * Grammatika, buzilgan jadvallar va shrift xatolarini AI orqali tekshirish va tasdiqlash.
6. **3 Ta Tilda Qo'llab-quvvatlash:** O'zbekcha 🇺🇿, English 🇬🇧, Русский 🇷🇺.

---

## 🇬🇧 English Description (v0.0.3)

**MarkItDown Studio** is a universal document, screenshot (`Win + Shift + S`), text, and audio-to-Markdown conversion suite built with modern web technologies and a high-performance .NET 10 WPF desktop engine.

### 🌟 Key Features:
- **📸 `Win + Shift + S` Screenshot OCR Integration:** Capture any area of your screen and press `Ctrl + V` inside the app to immediately extract text with OCR into clean Markdown!
- **📝 Convert Raw Text to Markdown:** Paste any unformatted text and click **✨ Convert Text to Markdown (.md)** to automatically structure it with headings, lists, tables, and Obsidian formatting.
- **100% Offline Free OCR:** Powered by high-speed native **Windows.Media.Ocr** engine on desktop and **WebAssembly OCR** in browser.
- **Universal Multi-AI Engine:** Google Gemini, Groq AI (500+ tok/s), OpenAI GPT-4o, Anthropic Claude 3.7, DeepSeek, and Ollama.
- **Trilingual Interface:** English, Uzbek, and Russian.

---

## 🇷🇺 Описание на русском (v0.0.3)

**MarkItDown Studio** — универсальный инструмент для преобразования любых документов, скриншотов (`Win + Shift + S`), сырого текста, таблиц и аудиофайлов в чистый Markdown (`.md`).

### 🌟 Основные возможности:
- **📸 Интеграция со скриншотами `Win + Shift + S`:** Сделайте скриншот экрана и нажмите `Ctrl + V` в программе, чтобы мгновенно извлечь текст через OCR в Markdown!
- **📝 Преобразование сырого текста в Markdown:** Вставьте любой текст и нажмите **✨ Преобразовать текст в Markdown (.md)** для автоматического структурирования!
- **100% Офлайн OCR (Без ключа API):** Встроенный движок **Windows Native OCR** на десктопе и **WebAssembly OCR** в браузере.
- **Мульти-ИИ интеграция:** Google Gemini, Groq AI (500+ токенов/сек), OpenAI, Claude 3.7, DeepSeek, Ollama.
- **Поддержка 3 языков:** Русский 🇷🇺, Узбекский 🇺🇿, Английский 🇬🇧.

---

## 💻 Desktop Application Setup (.NET 10)

```bash
# Clone the repository
git clone https://github.com/aka-FinGo/MarkItDown-Studio.git
cd MarkItDown-Studio/dotnet

# Build and run standalone WPF application
dotnet run --project src/MarkItDown.App/MarkItDown.App.csproj
```

Or download the ready-to-run **[MarkItDownStudio.exe v0.0.3](https://github.com/aka-FinGo/MarkItDown-Studio/releases/tag/v0.0.3)** from Releases.

---

## 📜 License
Apache-2.0 License. Created by aka-FinGo.
