import { useEffect, useRef } from "react";
import { useLocation } from "react-router-dom";
import { trackPageView } from "../lib/tracker";

export function TrackerProvider({ children }: { children: React.ReactNode }) {
  const { pathname } = useLocation();
  const lastPath = useRef(pathname);

  useEffect(() => {
    if (pathname === lastPath.current) return;
    lastPath.current = pathname;
    trackPageView();
  }, [pathname]);

  return <>{children}</>;
}
