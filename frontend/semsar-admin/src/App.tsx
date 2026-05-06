import { lazy, Suspense } from "react";
import { BrowserRouter, Route, Routes, Navigate } from "react-router-dom";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { ThemeProvider } from "next-themes";
import { DashboardLayout } from "@/components/DashboardLayout";
import { ProtectedRoute } from "@/components/ProtectedRoute";
import ErrorBoundary from "@/components/ErrorBoundary";
import * as Sentry from "@sentry/react";
import { Loader2 } from "lucide-react";

const LoginPage = lazy(() => import("@/pages/LoginPage"));
const OverviewPage = lazy(() => import("@/pages/OverviewPage"));
const PropertiesPage = lazy(() => import("@/pages/PropertiesPage"));
const LeadsPage = lazy(() => import("@/pages/LeadsPage"));
const ProjectsPage = lazy(() => import("@/pages/ProjectsPage"));
const LandRequestsPage = lazy(() => import("@/pages/LandRequestsPage"));
const BookingsPage = lazy(() => import("@/pages/BookingsPage"));
const SettingsPage = lazy(() => import("@/pages/SettingsPage"));
const ContactsPage = lazy(() => import("@/pages/ContactsPage"));
const NotFound = lazy(() => import("@/pages/NotFound"));

const PageLoader = () => (
  <div className="flex items-center justify-center py-20">
    <Loader2 className="w-8 h-8 animate-spin text-primary" />
  </div>
);

const App = () => (
  <Sentry.ErrorBoundary fallback={({ error }) => (
    <div className="min-h-screen flex items-center justify-center bg-background p-8">
      <div className="text-center space-y-4 max-w-lg">
        <h1 className="text-3xl sm:text-4xl font-bold text-foreground">Something went wrong</h1>
        <p className="text-muted-foreground text-sm">{error?.message}</p>
        <button onClick={() => window.location.reload()} className="px-8 py-3.5 bg-primary text-primary-foreground rounded-xl font-semibold hover:opacity-90 transition-opacity">Reload Page</button>
      </div>
    </div>
  )}>
  <ErrorBoundary>
    <ThemeProvider attribute="class" defaultTheme="dark" enableSystem={false}>
      <TooltipProvider>
        <Sonner />
        <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
          <Suspense fallback={<PageLoader />}>
            <Routes>
              <Route path="/admin/login" element={<ErrorBoundary><LoginPage /></ErrorBoundary>} />
              <Route
                path="/admin"
                element={
                  <ProtectedRoute>
                    <DashboardLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<ErrorBoundary><OverviewPage /></ErrorBoundary>} />
                <Route path="properties" element={<ErrorBoundary><PropertiesPage /></ErrorBoundary>} />
                <Route path="leads" element={<ErrorBoundary><LeadsPage /></ErrorBoundary>} />
                <Route path="bookings" element={<ErrorBoundary><BookingsPage /></ErrorBoundary>} />
                <Route path="projects" element={<ErrorBoundary><ProjectsPage /></ErrorBoundary>} />
                <Route path="land-requests" element={<ErrorBoundary><LandRequestsPage /></ErrorBoundary>} />
                <Route path="settings" element={<ErrorBoundary><SettingsPage /></ErrorBoundary>} />
                <Route path="contacts" element={<ErrorBoundary><ContactsPage /></ErrorBoundary>} />
              </Route>
              <Route path="/" element={<Navigate to="/admin/login" replace />} />
              <Route path="*" element={<ErrorBoundary><NotFound /></ErrorBoundary>} />
            </Routes>
          </Suspense>
        </BrowserRouter>
      </TooltipProvider>
    </ThemeProvider>
  </ErrorBoundary>
  </Sentry.ErrorBoundary>
);

export default App;
