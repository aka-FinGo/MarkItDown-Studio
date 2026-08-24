import React, { useState, useEffect, useRef } from "react";
import { X, LogOut, User, CheckCircle2, ShieldCheck, Settings2, Sparkles, AlertTriangle } from "lucide-react";
import { UserProfile } from "../types";

declare global {
  interface Window {
    google?: any;
  }
}

interface GoogleAuthModalProps {
  isOpen: boolean;
  onClose: () => void;
  user: UserProfile | null;
  onLogin: (user: UserProfile) => void;
  onLogout: () => void;
}

export const GoogleAuthModal: React.FC<GoogleAuthModalProps> = ({
  isOpen,
  onClose,
  user,
  onLogin,
  onLogout,
}) => {
  const [googleClientId, setGoogleClientId] = useState<string>(() => {
    return localStorage.getItem("markitdown_google_client_id") || "";
  });
  const [showConfig, setShowConfig] = useState(false);
  const [showManualForm, setShowManualForm] = useState(false);
  const [manualName, setManualName] = useState("");
  const [manualEmail, setManualEmail] = useState("");
  const googleBtnRef = useRef<HTMLDivElement>(null);

  // Decode JWT from Google Credential Response
  const parseJwt = (token: string) => {
    try {
      const base64Url = token.split(".")[1];
      const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split("")
          .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
          .join("")
      );
      return JSON.parse(jsonPayload);
    } catch (e) {
      console.error("JWT dekodlashda xatolik:", e);
      return null;
    }
  };

  const handleCredentialResponse = (response: any) => {
    if (response?.credential) {
      const payload = parseJwt(response.credential);
      if (payload) {
        const realUser: UserProfile = {
          id: payload.sub || `google_${Date.now()}`,
          name: payload.name || payload.given_name || "Google Foydalanuvchi",
          email: payload.email || "",
          picture: payload.picture || "https://lh3.googleusercontent.com/a/default-user=s96-c",
        };
        onLogin(realUser);
        onClose();
      }
    }
  };

  // Render official Google Sign In Button when client ID is present
  useEffect(() => {
    if (isOpen && !user && googleClientId && window.google?.accounts?.id) {
      try {
        window.google.accounts.id.initialize({
          client_id: googleClientId,
          callback: handleCredentialResponse,
          auto_select: false,
        });

        if (googleBtnRef.current) {
          googleBtnRef.current.innerHTML = "";
          window.google.accounts.id.renderButton(googleBtnRef.current, {
            theme: "outline",
            size: "large",
            width: "360",
            text: "signin_with",
            shape: "pill",
            logo_alignment: "left",
          });
        }
      } catch (err) {
        console.warn("Google GIS ogohlantirish:", err);
      }
    }
  }, [isOpen, user, googleClientId]);

  if (!isOpen) return null;

  const handleSaveClientId = (newId: string) => {
    setGoogleClientId(newId);
    localStorage.setItem("markitdown_google_client_id", newId);
    setShowConfig(false);
  };

  const handleManualLogin = (e: React.FormEvent) => {
    e.preventDefault();
    if (!manualName.trim()) return;
    const customUser: UserProfile = {
      id: `user_${Date.now()}`,
      name: manualName.trim(),
      email: manualEmail.trim() || `${manualName.toLowerCase().replace(/\s+/g, "")}@gmail.com`,
      picture: `https://api.dicebear.com/7.x/bottts/svg?seed=${encodeURIComponent(manualName)}`,
    };
    onLogin(customUser);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-zinc-900/60 backdrop-blur-xs animate-in fade-in duration-150">
      <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-zinc-200 space-y-5">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-zinc-100 pb-3">
          <div className="flex items-center space-x-2.5">
            <div className="w-8 h-8 rounded-xl bg-indigo-50 flex items-center justify-center text-indigo-600">
              <User className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-zinc-900">Google Profil bilan Kirish</h3>
              <p className="text-[11px] text-zinc-500">Shaxsiy profil va sozlamalarni saqlash</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-lg text-zinc-400 hover:text-zinc-600 hover:bg-zinc-100 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {user ? (
          // Logged in state
          <div className="space-y-4">
            <div className="flex items-center space-x-3.5 p-3.5 bg-emerald-50/90 rounded-2xl border border-emerald-200/80">
              <img
                src={user.picture}
                alt={user.name}
                className="w-13 h-13 rounded-full border-2 border-emerald-500 shadow-sm object-cover bg-white"
              />
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-1.5">
                  <h4 className="text-sm font-bold text-zinc-900 truncate">{user.name}</h4>
                  <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                </div>
                <p className="text-xs text-zinc-600 truncate">{user.email}</p>
                <div className="mt-1 flex items-center gap-1">
                  <span className="text-[10px] bg-emerald-600 text-white px-2 py-0.5 rounded-full font-medium">
                    Profil Faol
                  </span>
                </div>
              </div>
            </div>

            <div className="flex items-center justify-between pt-2 border-t border-zinc-100">
              <button
                onClick={onClose}
                className="px-4 py-2 text-xs font-semibold text-zinc-600 hover:bg-zinc-100 rounded-xl"
              >
                Yopish
              </button>
              <button
                onClick={() => {
                  onLogout();
                  onClose();
                }}
                className="flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-red-50 text-red-700 hover:bg-red-100 rounded-xl transition-all cursor-pointer"
              >
                <LogOut className="w-3.5 h-3.5" />
                <span>Hisobdan Chiqish</span>
              </button>
            </div>
          </div>
        ) : (
          // Not logged in state
          <div className="space-y-4">
            {/* Quick Profile Setup Form */}
            <form onSubmit={handleManualLogin} className="space-y-3 p-4 bg-zinc-50 rounded-2xl border border-zinc-200/80">
              <div className="flex items-center gap-1.5 text-xs font-bold text-zinc-800">
                <Sparkles className="w-3.5 h-3.5 text-indigo-600" />
                <span>Profilingizni kiriting:</span>
              </div>
              <div className="space-y-2">
                <input
                  type="text"
                  required
                  value={manualName}
                  onChange={(e) => setManualName(e.target.value)}
                  placeholder="Ism / Taxallus (masalan: FinGo)"
                  className="w-full px-3 py-2 text-xs border border-zinc-200 rounded-xl bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 font-medium"
                />
                <input
                  type="email"
                  value={manualEmail}
                  onChange={(e) => setManualEmail(e.target.value)}
                  placeholder="Emailingiz (ixtiyoriy, masalan: you@gmail.com)"
                  className="w-full px-3 py-2 text-xs border border-zinc-200 rounded-xl bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 font-medium"
                />
              </div>
              <button
                type="submit"
                className="w-full py-2 px-4 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-bold shadow-xs transition-all cursor-pointer"
              >
                Profilni Saqlash va Kirish
              </button>
            </form>

            {/* Official Google OAuth Section if Client ID is configured */}
            {googleClientId ? (
              <div className="space-y-2">
                <div className="text-center text-[11px] text-zinc-400 font-semibold uppercase tracking-wider">
                  Yoki Google OAuth
                </div>
                <div className="flex justify-center min-h-[44px]">
                  <div ref={googleBtnRef} className="w-full flex justify-center" />
                </div>
              </div>
            ) : null}

            {/* Google Cloud Client ID Config Drawer */}
            <div className="pt-2 border-t border-zinc-100">
              <button
                type="button"
                onClick={() => setShowConfig(!showConfig)}
                className="flex items-center gap-1.5 text-[11px] text-zinc-500 hover:text-zinc-700 font-medium cursor-pointer"
              >
                <Settings2 className="w-3.5 h-3.5" />
                <span>Google OAuth 2.0 Client ID ulash</span>
              </button>

              {showConfig && (
                <div className="mt-2.5 p-3.5 bg-zinc-50 rounded-xl border border-zinc-200 space-y-2 text-xs">
                  <div className="flex items-start gap-1.5 text-amber-700 text-[11px] font-medium">
                    <AlertTriangle className="w-3.5 h-3.5 shrink-0 mt-0.5" />
                    <span>Nega "Access blocked" chiqadi?</span>
                  </div>
                  <p className="text-[11px] text-zinc-600 leading-relaxed">
                    Google OAuth ishlashi uchun Google Cloud Console da sizning GitHub Pages domeningiz (<code>https://aka-fingo.github.io</code>) ruxsat etilgan bo'lishi shart.
                  </p>
                  <label className="block text-[11px] font-semibold text-zinc-700 mt-1">
                    Shaxsiy Google Client ID:
                  </label>
                  <input
                    type="text"
                    defaultValue={googleClientId}
                    onBlur={(e) => handleSaveClientId(e.target.value.trim())}
                    placeholder="xxxx.apps.googleusercontent.com"
                    className="w-full px-2.5 py-1.5 text-[11px] font-mono border border-zinc-200 rounded-lg bg-white"
                  />
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
