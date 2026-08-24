import React, { useState, useEffect, useRef } from "react";
import { X, LogOut, User, CheckCircle2, ShieldCheck, Key, Settings2 } from "lucide-react";
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
    return localStorage.getItem("markitdown_google_client_id") || "1056586071477-d4j85g7u8hbgmvd9m9j1n520eav4m61r.apps.googleusercontent.com";
  });
  const [showConfig, setShowConfig] = useState(false);
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

  // Render official Google Sign In Button
  useEffect(() => {
    if (isOpen && !user && window.google?.accounts?.id) {
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
        console.warn("Google GIS ishga tushirishda ogohlantirish:", err);
      }
    }
  }, [isOpen, user, googleClientId]);

  if (!isOpen) return null;

  const handleSaveClientId = (newId: string) => {
    setGoogleClientId(newId);
    localStorage.setItem("markitdown_google_client_id", newId);
    setShowConfig(false);
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
              <h3 className="text-sm font-bold text-zinc-900">Google OAuth 2.0 Bilan Kirish</h3>
              <p className="text-[11px] text-zinc-500">Haqiqiy Google hisobingiz bilan avtorizatsiya</p>
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
          // Logged in state: Display Real User Profile
          <div className="space-y-4">
            <div className="flex items-center space-x-3.5 p-3.5 bg-emerald-50/90 rounded-2xl border border-emerald-200/80">
              <img
                src={user.picture}
                alt={user.name}
                className="w-13 h-13 rounded-full border-2 border-emerald-500 shadow-sm object-cover"
              />
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-1.5">
                  <h4 className="text-sm font-bold text-zinc-900 truncate">{user.name}</h4>
                  <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                </div>
                <p className="text-xs text-zinc-600 truncate">{user.email}</p>
                <div className="mt-1 flex items-center gap-1">
                  <span className="text-[10px] bg-emerald-600 text-white px-2 py-0.5 rounded-full font-medium">
                    Google Bilan Tasdiqlangan
                  </span>
                </div>
              </div>
            </div>

            <div className="flex items-center justify-between pt-2 border-t border-zinc-100">
              <span className="text-[11px] text-zinc-400">ID: {user.id.substring(0, 14)}...</span>
              <div className="flex items-center space-x-2">
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
          </div>
        ) : (
          // Not logged in: Official Google Sign-In Button
          <div className="space-y-4">
            <div className="p-3 bg-zinc-50 rounded-xl border border-zinc-200/60 text-xs text-zinc-600 space-y-1.5">
              <div className="flex items-center gap-2 text-indigo-700 font-semibold">
                <ShieldCheck className="w-4 h-4" />
                <span>Google OAuth 2.0 Xavfsiz Kirish:</span>
              </div>
              <p className="text-[11px] leading-relaxed">
                Tizimga kirish orqali siz o'zingizning haqiqiy Google profilingiz (ism, familiya, rasm va email) bilan ulanasiz.
              </p>
            </div>

            {/* Official Google Identity Services Button Container */}
            <div className="flex flex-col items-center justify-center py-2 min-h-[50px]">
              <div ref={googleBtnRef} className="w-full flex justify-center" />
            </div>

            {/* Google Client ID Settings Drawer */}
            <div className="pt-2 border-t border-zinc-100">
              <button
                onClick={() => setShowConfig(!showConfig)}
                className="flex items-center gap-1.5 text-[11px] text-zinc-500 hover:text-zinc-700 font-medium cursor-pointer"
              >
                <Settings2 className="w-3.5 h-3.5" />
                <span>Google Client ID sozlamalari</span>
              </button>

              {showConfig && (
                <div className="mt-2 p-3 bg-zinc-50 rounded-xl border border-zinc-200 space-y-2">
                  <label className="block text-[11px] font-semibold text-zinc-700">
                    Google Cloud OAuth Client ID:
                  </label>
                  <input
                    type="text"
                    defaultValue={googleClientId}
                    onBlur={(e) => handleSaveClientId(e.target.value.trim())}
                    placeholder="xxxx.apps.googleusercontent.com"
                    className="w-full px-2.5 py-1.5 text-[11px] font-mono border border-zinc-200 rounded-lg bg-white"
                  />
                  <p className="text-[10px] text-zinc-500">
                    O'zingizning shaxsiy Google Cloud Client ID-ingizni kiritishingiz mumkin.
                  </p>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
