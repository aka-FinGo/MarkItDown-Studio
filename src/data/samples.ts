export interface SampleFile {
  name: string;
  type: string;
  format: string;
  description: string;
  content: string;
  mimeType: string;
}

export const SAMPLE_FILES: SampleFile[] = [
  {
    name: "moliyaviy_hisobot.csv",
    type: "CSV Jadvali",
    format: "CSV",
    description: "Choraklik savdo ko'rsatkichlari, tushum, yalpi foyda va o'sish dinamikasi",
    mimeType: "text/csv",
    content: `Chorak,Hudud,Mahsulot turi,Sotilgan soni,Tushum (UZS),Xarajat (UZS),Yalpi Foyda,Marja %,Yillik O'sish
1-chorak 2026,Toshkent shahri,Bulutli Xizmatlar,14200,7100000000,2130000000,4970000000,70.0%,+24.5%
1-chorak 2026,Samarqand,Bulutli Xizmatlar,9800,4900000000,1568000000,3332000000,68.0%,+18.2%
2-chorak 2026,Toshkent shahri,Sun'iy Intellekt Vositalari,18500,9250000000,2312500000,6937500000,75.0%,+42.1%
2-chorak 2026,Farg'ona vodiysi,Sun'iy Intellekt Vositalari,12400,6200000000,1674000000,4526000000,73.0%,+36.8%
3-chorak 2026,Toshkent shahri,Ma'lumotlar Tahlili,22100,11050000000,2652000000,8398000000,76.0%,+29.4%
Jami 2026,O'zbekiston,Barcha Mahsulotlar,77000,38500000000,10336500000,28163500000,73.15%,+30.2%`,
  },
  {
    name: "foydalanuvchilar_api.json",
    type: "JSON Ma'lumotlari",
    format: "JSON",
    description: "Foydalanuvchi profillari, obuna turlari va statistik ko'rsatkichlar",
    mimeType: "application/json",
    content: JSON.stringify(
      {
        xizmat: "MarkItDown O'zbekiston",
        holati: "faol",
        vaqt: "2026-08-23T09:00:00Z",
        statistika: {
          bugungiKonvertatsiyalar: 142589,
          muvaffaqiyatFoizi: "99.94%",
          ortachaVaqtMs: 312,
          qollabQuvvatlanuvchiFormatlar: ["PDF", "DOCX", "PPTX", "XLSX", "CSV", "JSON", "HTML", "Rasmlar (OCR)", "Audio Ovoz"],
        },
        faolFoydalanuvchilar: [
          { id: "usr_991", ism: "Anvar_dev", tarif: "Enterprise", oylikFayllar: 42300, kvota: "42.3%" },
          { id: "usr_992", ism: "Madina_tahlil", tarif: "Pro", oylikFayllar: 8910, kvota: "89.1%" },
          { id: "usr_993", ism: "Javohir_team", tarif: "Jamoa", oylikFayllar: 18200, kvota: "60.6%" },
        ],
      },
      null,
      2
    ),
  },
  {
    name: "dasturiy_hujjat.html",
    type: "HTML Sahifa",
    format: "HTML",
    description: "Jadvallar, ro'yxatlar va texnik kod bloklarini o'z ichiga olgan veb sahifa",
    mimeType: "text/html",
    content: `<!DOCTYPE html>
<html>
<head><title>MarkItDown Tizim Arxitekturasi</title></head>
<body>
  <h1>MarkItDown Tizim Qo'llanmasi</h1>
  <p>Ushbu tizim har qanday hujjat, rasm va audio fayllarni yuqori tezlikda <strong>toza Markdown (.md)</strong> formatiga o'tkazish uchun mo'ljallangan.</p>
  
  <h2>Asosiy Xususiyatlar</h2>
  <ul>
    <li><strong>Lokal Dvigatel:</strong> Oddiy hujjatlar uchun 0 AI token sarfi bilan mutlaqo bepul.</li>
    <li><strong>Rasmdan Matn O'qish (OCR):</strong> Skrinshot va skanerlangan hujjatlardan matnlarni aniq ajratib olish.</li>
    <li><strong>Audio Nutq Transkripsiyasi:</strong> Ovozli xabarlar va audio yozuvlarni matnga aylantirish.</li>
  </ul>

  <h2>Tezlik Taqqoslashi</h2>
  <table border="1">
    <tr><th>Fayl turi</th><th>O'girish vaqti</th><th>Natija sifati</th><th>AI Token Sarfi</th></tr>
    <tr><td>Word / DOCX</td><td>0.1 soniya</td><td>100% GFM Markdown</td><td>0 token</td></tr>
    <tr><td>Excel / CSV</td><td>0.05 soniya</td><td>Mukammal jadval</td><td>0 token</td></tr>
    <tr><td>Rasm (OCR)</td><td>1.2 soniya</td><td>Toza matn va sarlavhalar</td><td>Minimal</td></tr>
    <tr><td>Audio / Ovoz</td><td>1.5 soniya</td><td>To'liq nutq transkripsiyasi</td><td>Minimal</td></tr>
  </table>
</body>
</html>`,
  },
  {
    name: "server_jurnali.log",
    type: "Log Jurnali",
    format: "LOG",
    description: "Tizim ishga tushishi, xavfsizlik va jarayon holatlari jurnali",
    mimeType: "text/plain",
    content: `[2026-08-23 09:01:12] [MA'LUMOT] MarkItDown universal konvertatsiya tizimi ishga tushdi
[2026-08-23 09:01:13] [MA'LUMOT] Yuklangan modullar: [PDF, DOCX, XLSX, CSV, JSON, HTML, OCR, Nutq Ovoz]
[2026-08-23 09:01:14] [MA'LUMOT] Gemini 3.7 Flash multimodal modeli ulandi
[2026-08-23 09:01:15] [MA'LUMOT] Tizim API porti 3000 da tinglamoqda
[2026-08-23 09:01:16] [MUVAFFAQIN] Barcha ishchi oqimlar faol va fayllarni qabul qilishga tayyor`,
  },
];
