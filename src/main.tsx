import { createRoot } from "react-dom/client";
import App from "./App.tsx";
import "./index.css";
import * as Sentry from "@sentry/react";

const SENTRY_DSN = import.meta.env.VITE_SENTRY_DSN as string | undefined;

if (SENTRY_DSN) {
  Sentry.init({
    dsn: SENTRY_DSN,
    environment: import.meta.env.MODE,
  });
}

window.addEventListener("error", (event) => {
  console.error("Global error:", event.error || event.message);
  Sentry.captureException(event.error || event.message);
});

window.addEventListener("unhandledrejection", (event) => {
  console.error("Unhandled promise rejection:", event.reason);
  Sentry.captureException(event.reason);
});

if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch(() => {});
  });
}

const rootEl = document.getElementById("root");
if (!rootEl) throw new Error("Root element #root not found");
createRoot(rootEl).render(<App />);
