# 📄 MarkItDown Studio (Web & Desktop .NET)

<p align="center">
  <img src="https://img.shields.io/badge/Version-v0.0.2-6366F1?style=for-the-badge&logo=github" alt="Version 0.0.2" />
  <img src="https://img.shields.io/badge/.NET-10.0_WPF-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/React-19_Vite-61DAFB?style=for-the-badge&logo=react" alt="React 19" />
  <img src="https://img.shields.io/badge/OCR-Windows_Native_%7C_WebAssembly_Tesseract-10B981?style=for-the-badge&logo=windows" alt="Offline OCR" />
  <img src="https://img.shields.io/badge/AI-Gemini_%7C_Groq_%7C_OpenAI_%7C_Claude_%7C_DeepSeek_%7C_Ollama-EC4899?style=for-the-badge&logo=google" alt="Multi-AI" />
  <img src="https://img.shields.io/badge/Languages-UZ_%7C_EN_%7C_RU-F59E0B?style=for-the-badge" alt="Trilingual" />
</p>

---

## 🌐 Live Web App & Desktop Download
- 🚀 **Web App (GitHub Pages):** [https://aka-FinGo.github.io/MarkItDown-Studio/](https://aka-FinGo.github.io/MarkItDown-Studio/)
- 💻 **Standalone Desktop .exe (Windows 10/11 x64):** [Download MarkItDownStudio.exe v0.0.2](https://github.com/aka-FinGo/MarkItDown-Studio/releases/tag/v0.0.2) *(100% Self-contained, no .NET install required!)*

---

## 🇺🇿 O'zbekcha Tavsif

**MarkItDown Studio (v0.0.2)** — Microsoft MarkItDown tamoyillariga asoslangan, har qanday hujjat, rasm va audio fayllarni 100% toza, chiroyli va tartibli Markdown (`.md`) formatiga aylantirib beruvchi universal konvertatsiya studiyasi.

### 🌟 Yangi Imkoniyatlar (v0.0.2):
1. **100% Oflayn va Bepul OCR (Internet va API kalitsiz):**
   - **Desktop dasturda:** Windows 10/11 tizimining o'rnatilgan **Windows Native OCR** dvigateli ishlaydi (0 MB, tezkor, Krill va Lotin harflarini aniq taniydi).
   - **Veb-versiyada (GitHub Pages):** Brauzer ichida **WebAssembly (Tesseract.js)** orqali bepul oflayn matn ajratish ishlaydi.
2. **Ko'p Provayderli Sun'iy Intellekt (Multi-AI) & Smart Fallback:**
   - **Google Gemini:** `gemini-2.5-flash`, `gemini-2.5-pro`, `gemini-3.7-flash`, `gemini-3-flash`, `gemini-3-pro`, `gemini-3-deep-think`
   - **Groq AI (Ultra-Tez 500+ tok/s):** `llama-3.3-70b-versatile`, `llama-3.1-8b-instant`, `deepseek-r1-distill-llama-70b`
   - **OpenAI:** `gpt-4o`, `gpt-4o-mini`, `o3-mini`, `o1`
   - **Anthropic Claude:** `claude-3-7-sonnet`, `claude-3-5-sonnet`, `claude-3-5-haiku`
   - **DeepSeek:** `deepseek-chat` (V3), `deepseek-reasoner` (R1)
   - **Ollama (Lokal):** `llama3.2-vision`, `llava`, `qwen2.5-vl`
3. **AI Proofreader (Xatoliklarni Tekshirish & Review):**
   - Grammatika, buzilgan jadvallar va shrift xatolarini AI orqali tekshirish va tasdiqlash.
4. **3 Ta Tilda Qo'llab-quvvatlash:** O'zbekcha 🇺🇿, English 🇬🇧, Русский 🇷🇺.

---

## 🇬🇧 English Description (v0.0.2)

**MarkItDown Studio** is a universal document, image, and audio-to-Markdown conversion suite built with modern web technologies and a high-performance .NET 10 WPF desktop engine.

### 🌟 Key Features:
- **100% Offline Free OCR (No API Key Required):**
  - **Desktop:** Powered by high-speed native **Windows.Media.Ocr** engine.
  - **Web:** Client-side **WebAssembly OCR** in the browser.
- **Universal Multi-AI Engine:** Google Gemini, Groq AI (500+ tok/s), OpenAI GPT-4o, Anthropic Claude 3.7, DeepSeek R1/V3, and Ollama.
- **Smart Fallback:** Seamless auto-retry on rate limits.
- **AI Proofreader:** Review and fix broken tables, OCR typos, and formatting.
- **Trilingual Interface:** English, Uzbek, and Russian.

---

## 🇷🇺 Описание на русском (v0.0.2)

**MarkItDown Studio** — универсальный инструмент для преобразования любых документов, таблиц, сканированных PDF, изображений и аудиофайлов в чистый Markdown (`.md`).

### 🌟 Основные возможности:
- **100% Офлайн OCR (Без ключа API):** Встроенный движок **Windows Native OCR** на десктопе и **WebAssembly OCR** в браузере.
- **Мульти-ИИ интеграция:** Google Gemini, Groq AI (500+ токенов/сек), OpenAI GPT-4o, Claude 3.7, DeepSeek, Ollama.
- **Умное авто-переключение:** Автоматическая защита от лимитов.
- **Проверка с ИИ:** Поиск и исправление орфографии, битых таблиц и кодировок.
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

Or download the ready-to-run **[MarkItDownStudio.exe v0.0.2](https://github.com/aka-FinGo/MarkItDown-Studio/releases/tag/v0.0.2)** from Releases.

---

## 📜 License
Apache-2.0 License. Created by aka-FinGo.
