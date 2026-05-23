import { useEffect, useLayoutEffect, useRef } from 'react';
import { useLocation } from 'react-router-dom';

const STORAGE_KEY = 'semsar_scroll';

function getPositions(): Record<string, number> {
  try {
    return JSON.parse(sessionStorage.getItem(STORAGE_KEY) || '{}');
  } catch {
    return {};
  }
}

function setPosition(key: string, pos: number) {
  try {
    const all = getPositions();
    all[key] = pos;
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(all));
  } catch {
    // sessionStorage may be unavailable
  }
}

function removePosition(key: string) {
  try {
    const all = getPositions();
    delete all[key];
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(all));
  } catch {
    // sessionStorage may be unavailable
  }
}

export function ScrollManager({ children }: { children: React.ReactNode }) {
  const location = useLocation();
  const locationKey = location.key;
  const pathKey = location.pathname + location.search;
  const pathKeyRef = useRef(pathKey);
  const isPopRef = useRef(false);
  const scrollToRef = useRef<string | null>(null);

  pathKeyRef.current = pathKey;

  // Track scrollTo from navigation state (e.g. nav links scroll to sections)
  scrollToRef.current = (location.state as Record<string, unknown>)?.scrollTo as string | null ?? null;

  // Initialise manual scroll restoration once
  useEffect(() => {
    if ('scrollRestoration' in history) {
      history.scrollRestoration = 'manual';
    }

    const handlePopState = () => {
      isPopRef.current = true;
      setPosition(pathKeyRef.current, window.scrollY);
    };

    window.addEventListener('popstate', handlePopState);
    return () => window.removeEventListener('popstate', handlePopState);
  }, []);

  useLayoutEffect(() => {
    // Handle scrollTo state first (takes priority)
    const targetId = scrollToRef.current;
    if (targetId) {
      scrollToRef.current = null;
      document.getElementById(targetId)?.scrollIntoView({ behavior: 'smooth' });
      window.history.replaceState({}, document.title);
      return;
    }

    if (isPopRef.current) {
      isPopRef.current = false;
      const all = getPositions();
      const saved = all[pathKey];
      if (saved != null && saved > 0) {
        window.scrollTo(0, saved);
        return;
      }
    }

    // New navigation — scroll to top instantly
    window.scrollTo(0, 0);
  }, [pathKey]);

  // Save position on scroll (rAF-throttled) + cleanup on unmount
  useEffect(() => {
    let ticking = false;

    const handleScroll = () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          setPosition(pathKey, Math.max(0, window.scrollY));
          ticking = false;
        });
        ticking = true;
      }
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => {
      setPosition(pathKey, Math.max(0, window.scrollY));
      window.removeEventListener('scroll', handleScroll);
      removePosition(locationKey);
    };
  }, [pathKey, locationKey]);

  return <>{children}</>;
}
