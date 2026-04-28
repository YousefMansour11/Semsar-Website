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

export function ScrollManager({ children }: { children: React.ReactNode }) {
  const location = useLocation();
  const pathKey = location.pathname + location.search;
  const pathKeyRef = useRef(pathKey);
  const isPopRef = useRef(false);
  pathKeyRef.current = pathKey;

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

  // Clamp utility to keep footer out of view
  function clampScrollY(top: number): number {
    const footer = document.querySelector('footer');
    if (footer) {
      const footerTop = footer.getBoundingClientRect().top + window.scrollY;
      top = Math.min(top, footerTop - window.innerHeight);
    }
    return Math.max(0, top);
  }

  useLayoutEffect(() => {
    // Check for restoreScrollY (app-back with exact position)
    const historyUsr = (window.history.state as Record<string, unknown>)?.usr as Record<string, unknown> | undefined;
    const navState = (location.state || historyUsr || {}) as Record<string, unknown>;
    const restoreScrollY = navState.restoreScrollY as number | undefined;

    if (restoreScrollY != null) {
      window.history.replaceState({}, document.title);
      window.scrollTo({ top: clampScrollY(restoreScrollY), behavior: 'smooth' });
      return;
    }

    const scrollToSection = navState.scrollTo as string | undefined;
    if (scrollToSection) {
      window.history.replaceState({}, document.title);
      const el = document.getElementById(scrollToSection);
      if (el) {
        const footer = document.querySelector('footer');
        let top = el.getBoundingClientRect().top + window.scrollY;
        if (footer) {
          const footerTop = footer.getBoundingClientRect().top + window.scrollY;
          top = Math.min(top, footerTop - window.innerHeight);
        }
        window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
      }
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
      // No saved position — let the page handle its own restoration
      return;
    }

    // New navigation — scroll to top instantly
    window.scrollTo(0, 0);
  // location.state intentionally excluded — null during v7_startTransition, reads from window.history.state.usr instead
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pathKey]);

  // Save position on scroll (rAF-throttled, properly cancels on cleanup)
  useEffect(() => {
    let ticking = false;
    let rafId: number | null = null;

    const handleScroll = () => {
      if (!ticking) {
        rafId = requestAnimationFrame(() => {
          setPosition(pathKey, Math.max(0, window.scrollY));
          ticking = false;
          rafId = null;
        });
        ticking = true;
      }
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => {
      if (rafId !== null) cancelAnimationFrame(rafId);
      setPosition(pathKey, Math.max(0, window.scrollY));
      window.removeEventListener('scroll', handleScroll);
    };
  }, [pathKey]);

  return <>{children}</>;
}
