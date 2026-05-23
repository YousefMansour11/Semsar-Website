import { lazy, Suspense, useEffect } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Route, Routes, Outlet } from "react-router-dom";
import { ScrollManager } from "./components/ScrollManager";
import { HelmetProvider } from "react-helmet-async";
import { TooltipProvider } from "@/components/ui/tooltip";
import { ThemeProvider } from "next-themes";
import { LanguageProvider, useLanguage } from "./i18n/LanguageContext";
import ErrorBoundary from "./components/ErrorBoundary";
import * as Sentry from "@sentry/react";
import { initTracker } from "./lib/tracker";
import { TrackerProvider } from "./components/TrackerProvider";

const Index = lazy(() => import("./pages/Index"));
const PropertyDetailsPage = lazy(() => import("./pages/PropertyDetailsPage"));
const ProjectDetailsPage = lazy(() => import("./pages/ProjectDetailsPage"));
const UnitDetailsPage = lazy(() => import("./pages/UnitDetailsPage"));
const AboutPage = lazy(() => import("./pages/AboutPage"));
const ContactPage = lazy(() => import("./pages/ContactPage"));
const NotFound = lazy(() => import("./pages/NotFound"));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function LangWrap({ lang }: { lang: string }) {
  const { setLanguage } = useLanguage();
  useEffect(() => {
    if (lang === 'en' || lang === 'ar') setLanguage(lang);
  }, [lang, setLanguage]);
  return <Outlet />;
}

function TrackerInit() {
  useEffect(() => { initTracker(); }, []);
  return null;
}

const App = () => (
  <Sentry.ErrorBoundary fallback={({ error }) => (
    <div className="min-h-screen flex items-center justify-center bg-background p-8">
      <div className="text-center space-y-4 max-w-lg">
        <h1 className="text-3xl sm:text-4xl font-bold text-foreground">Something went wrong</h1>
        <p className="text-muted-foreground text-sm">{error?.message}</p>
        <button onClick={() => window.location.reload()} className="px-8 py-3.5 bg-gold text-navy rounded-xl font-semibold hover:bg-gold-dark hover:text-white transition-colors">Reload Page</button>
      </div>
    </div>
  )}>
  <HelmetProvider>
    <QueryClientProvider client={queryClient}>
      <ThemeProvider attribute="class" defaultTheme="light" enableSystem={false}>
      <TooltipProvider>
        <LanguageProvider>
          <TrackerInit />
          <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
            <TrackerProvider>
              <ScrollManager>
                <Suspense fallback={null}>
                  <Routes>
                    {/* Language-prefixed routes */}
                    <Route path="/en" element={<LangWrap lang="en" />}>
                      <Route index element={<ErrorBoundary><Index /></ErrorBoundary>} />
                      <Route path="properties/:slug" element={<ErrorBoundary><PropertyDetailsPage /></ErrorBoundary>} />
                      <Route path="projects/:slug" element={<ErrorBoundary><ProjectDetailsPage /></ErrorBoundary>} />
                      <Route path="units/:slug" element={<ErrorBoundary><UnitDetailsPage /></ErrorBoundary>} />
                      <Route path="about" element={<ErrorBoundary><AboutPage /></ErrorBoundary>} />
                      <Route path="contact" element={<ErrorBoundary><ContactPage /></ErrorBoundary>} />
                      <Route path="*" element={<NotFound />} />
                    </Route>
                    <Route path="/ar" element={<LangWrap lang="ar" />}>
                      <Route index element={<ErrorBoundary><Index /></ErrorBoundary>} />
                      <Route path="properties/:slug" element={<ErrorBoundary><PropertyDetailsPage /></ErrorBoundary>} />
                      <Route path="projects/:slug" element={<ErrorBoundary><ProjectDetailsPage /></ErrorBoundary>} />
                      <Route path="units/:slug" element={<ErrorBoundary><UnitDetailsPage /></ErrorBoundary>} />
                      <Route path="about" element={<ErrorBoundary><AboutPage /></ErrorBoundary>} />
                      <Route path="contact" element={<ErrorBoundary><ContactPage /></ErrorBoundary>} />
                      <Route path="*" element={<NotFound />} />
                    </Route>
                    {/* Flat routes (backward compat) */}
                    <Route path="/" element={<ErrorBoundary><Index /></ErrorBoundary>} />
                    <Route path="/properties/:slug" element={<ErrorBoundary><PropertyDetailsPage /></ErrorBoundary>} />
                    <Route path="/projects/:slug" element={<ErrorBoundary><ProjectDetailsPage /></ErrorBoundary>} />
                    <Route path="/units/:slug" element={<ErrorBoundary><UnitDetailsPage /></ErrorBoundary>} />
                    <Route path="/about" element={<ErrorBoundary><AboutPage /></ErrorBoundary>} />
                    <Route path="/contact" element={<ErrorBoundary><ContactPage /></ErrorBoundary>} />
                    <Route path="*" element={<NotFound />} />
                  </Routes>
                </Suspense>
              </ScrollManager>
            </TrackerProvider>
          </BrowserRouter>
        </LanguageProvider>
      </TooltipProvider>
      </ThemeProvider>
    </QueryClientProvider>
  </HelmetProvider>
  </Sentry.ErrorBoundary>
);

export default App;
