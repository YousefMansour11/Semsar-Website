import { Link, useLocation, useNavigate, Outlet } from "react-router-dom";
import {
  LayoutDashboard, Home, Users, Building2, Menu, X, Eye, EyeOff,
  Globe, LogOut, Settings, Phone, CalendarCheck,
} from "lucide-react";
import { useState } from "react";
import { cn } from "@/lib/utils";
import { useStore } from "@/store";
import { useAuthStore } from "@/lib/admin-api";
import { Button } from "@/components/ui/button";

const navItems = [
  { href: "/admin", label: "Dashboard", icon: LayoutDashboard },
  { href: "/admin/properties", label: "Properties", icon: Home },
  { href: "/admin/projects", label: "Projects", icon: Building2 },
  { href: "/admin/leads", label: "Leads", icon: Users },
  { href: "/admin/bookings", label: "Bookings", icon: CalendarCheck },
  { href: "/admin/land-requests", label: "Land Requests", icon: Globe },
  { href: "/admin/contacts", label: "Contacts", icon: Phone },
  { href: "/admin/settings", label: "Settings", icon: Settings },
];

export function DashboardLayout() {
  const location = useLocation();
  const navigate = useNavigate();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const previewMode = useStore(s => s.previewMode);
  const togglePreviewMode = useStore(s => s.togglePreviewMode);
  const { user, logout } = useAuthStore();
  const adminName = user?.username || 'Admin';

  const isActive = (href: string) =>
    href === "/admin" ? location.pathname === "/admin" : location.pathname.startsWith(href);

  const handleLogout = () => {
    logout();
    navigate("/admin/login", { replace: true });
  };

  return (
    <div className="flex h-screen overflow-hidden">
      {sidebarOpen && (
        <div className="fixed inset-0 z-40 bg-background/80 backdrop-blur-sm lg:hidden" onClick={() => setSidebarOpen(false)} />
      )}

      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-50 w-64 border-r border-border bg-card flex flex-col transition-transform duration-300 lg:relative lg:translate-x-0",
          sidebarOpen ? "translate-x-0" : "-translate-x-full"
        )}
      >
        <div className="h-16 flex items-center justify-between px-6 border-b border-border">
          <h1 className="text-xl font-bold tracking-tight">
            <span className="text-primary">SEM</span>
            <span className="text-foreground">SAR</span>
          </h1>
          <button type="button" className="lg:hidden text-muted-foreground" onClick={() => setSidebarOpen(false)} aria-label="Close sidebar">
            <X className="w-5 h-5" />
          </button>
        </div>

        <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
          {navItems.map((item) => (
            <Link
              key={item.href}
              to={item.href}
              onClick={() => setSidebarOpen(false)}
              className={cn(
                "flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all duration-200",
                isActive(item.href)
                  ? "bg-primary/10 text-primary shadow-sm"
                  : "text-muted-foreground hover:bg-accent hover:text-foreground"
              )}
            >
              <item.icon className="w-5 h-5" />
              {item.label}
            </Link>
          ))}
        </nav>

        <div className="p-4 border-t border-border space-y-2">
          <button
            type="button"
            onClick={handleLogout}
            className="flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium text-destructive hover:bg-destructive/10 transition-all duration-200 w-full"
          >
            <LogOut className="w-5 h-5" />
            Logout
          </button>
          <div className="flex items-center gap-3 px-4 py-2">
            <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center text-primary text-sm font-bold">
              {adminName[0]}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium truncate">{adminName}</p>
              <p className="text-xs text-muted-foreground truncate">{user?.role || 'Admin'}</p>
            </div>
          </div>
        </div>
      </aside>

      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        <header className="h-16 flex items-center gap-4 px-6 border-b border-border bg-card/50 backdrop-blur-sm shrink-0">
          <button type="button" className="lg:hidden text-muted-foreground" onClick={() => setSidebarOpen(true)} aria-label="Open sidebar">
            <Menu className="w-6 h-6" />
          </button>
          <div className="flex-1" />

          <Button
            variant={previewMode ? "default" : "outline"}
            size="sm"
            onClick={togglePreviewMode}
            className={cn(
              "gap-2 transition-all duration-200",
              previewMode && "bg-primary text-primary-foreground shadow-lg shadow-primary/20"
            )}
          >
            {previewMode ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
            {previewMode ? "Exit Preview" : "Preview as User"}
          </Button>
        </header>

        <main className="flex-1 overflow-y-auto overflow-x-hidden p-6 scrollbar-thin">
          {previewMode && (
            <div className="mb-4 px-4 py-2 bg-primary/10 border border-primary/20 rounded-xl text-sm text-primary flex items-center gap-2">
              <Eye className="w-4 h-4" />
              <span>You are viewing as a <strong>user</strong> — admin controls are hidden.</span>
            </div>
          )}
          <Outlet />
        </main>
      </div>
    </div>
  );
}
