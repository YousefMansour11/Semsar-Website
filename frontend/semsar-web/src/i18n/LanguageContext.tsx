import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { translations, type Language } from './translations';

interface LanguageContextType {
  language: Language;
  setLanguage: (lang: Language) => void;
  t: (key: string, fallback?: string, params?: Record<string, string>) => string;
  fmtNum: (n: number) => string;
  fmtPrice: (n: number, currency?: string) => string;
  fmtDate: (date: string | Date) => string;
  dir: 'ltr' | 'rtl';
  localeStr: string;
}

const LanguageContext = createContext<LanguageContextType | undefined>(undefined);

function getInitialLanguage(): Language {
  try {
    const stored = localStorage.getItem('semsar_lang');
    if (stored === 'ar' || stored === 'en') return stored;
  } catch { /* localStorage not available */ }
  return 'en';
}

export function LanguageProvider({ children }: { children: React.ReactNode }) {
  const [language, setLanguageState] = useState<Language>(getInitialLanguage);

  const setLanguage = useCallback((lang: Language) => {
    setLanguageState(lang);
    try { localStorage.setItem('semsar_lang', lang); } catch { /* localStorage not available */ }
  }, []);

  useEffect(() => {
    document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.lang = language;
  }, [language]);

  const t = useCallback((key: string, fallback?: string, params?: Record<string, string>): string => {
    let value = (translations[language] as Record<string, string>)[key] || (translations['en'] as Record<string, string>)[key] || fallback || key;
    if (params) {
      Object.entries(params).forEach(([k, v]) => { value = value.replace(`{${k}}`, v); });
    }
    return value;
  }, [language]);

  const localeStr = language === 'ar' ? 'ar-EG' : 'en-US';
  const dir = language === 'ar' ? 'rtl' : 'ltr';

  const fmtNum = useCallback((n: number): string => {
    return new Intl.NumberFormat(localeStr).format(n);
  }, [localeStr]);

  const fmtPrice = useCallback((n: number, currency: string = 'EGP'): string => {
    return new Intl.NumberFormat(localeStr, {
      style: 'currency', currency, maximumFractionDigits: 0,
    }).format(n);
  }, [localeStr]);

  const fmtDate = useCallback((date: string | Date): string => {
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString(localeStr, { month: 'short', day: 'numeric', year: 'numeric' });
  }, [localeStr]);

  return (
    <LanguageContext.Provider value={{ language, setLanguage, t, fmtNum, fmtPrice, fmtDate, dir, localeStr }}>
      {children}
    </LanguageContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useLanguage() {
  const context = useContext(LanguageContext);
  if (!context) throw new Error('useLanguage must be used within a LanguageProvider');
  return context;
}
