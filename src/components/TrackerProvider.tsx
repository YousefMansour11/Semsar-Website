import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import { trackPageView } from "../lib/tracker";

export function TrackerProvider({ children }: { children: React.ReactNode }) {
  const location = useLocation();

  useEffect(() => {
    trackPageView();
  }, [location]);

  return <>{children}</>;
}
