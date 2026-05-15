import { useEffect, useState, useMemo, useCallback } from "react";
import { useStore, PROPERTY_TYPES } from "@/store";
import { useNavigate } from "react-router-dom";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Building2, Users, TrendingUp, Home, DollarSign, Key, Calendar, Globe, Plus, Eye, Star, Loader2,
} from "lucide-react";
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, BarChart, Bar,
} from "recharts";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "@/components/ui/chart";
import { adminApi } from "@/lib/admin-api";
import { toast } from "sonner";

export default function OverviewPage() {
  const properties = useStore(s => s.properties);
  const leads = useStore(s => s.leads);
  const projects = useStore(s => s.projects);
  const bookings = useStore(s => s.bookings);
  const landRequests = useStore(s => s.landRequests);
  const togglePreviewMode = useStore(s => s.togglePreviewMode);
  const loadProperties = useStore(s => s.loadProperties);
  const loadLeads = useStore(s => s.loadLeads);
  const loadProjects = useStore(s => s.loadProjects);
  const loadBookings = useStore(s => s.loadBookings);
  const loadLandRequests = useStore(s => s.loadLandRequests);
  const loading = useStore(s => s.loading);
  const apiError = useStore(s => s.apiError);
  const clearApiError = useStore(s => s.clearApiError);
  const navigate = useNavigate();
  const [apiStats, setApiStats] = useState<Record<string, unknown> | null>(null);
  const [statsLoading, setStatsLoading] = useState(true);
  const [statsError, setStatsError] = useState(false);

  const fetchStats = useCallback(() => {
    setStatsLoading(true);
    setStatsError(false);
    adminApi.getStats().then(setApiStats).catch(() => { setStatsError(true); toast.error("Failed to load stats"); }).finally(() => setStatsLoading(false));
  }, []);

  useEffect(() => {
    (async () => {
      await loadProperties();
      await Promise.all([loadProjects(), loadLeads()]);
      await Promise.all([loadBookings(), loadLandRequests()]);
      fetchStats();
    })();
  }, [loadProperties, loadProjects, loadLeads, loadBookings, loadLandRequests, fetchStats]);

  useEffect(() => { if (apiError) { toast.error(apiError); clearApiError(); } }, [apiError, clearApiError]);

  const leadsChart = useMemo(() => {
    const buckets: Record<string, number> = {};
    leads.forEach(l => {
      const d = new Date(l.createdAt);
      const key = `${d.getMonth()+1}/${d.getDate()}`;
      buckets[key] = (buckets[key] || 0) + 1;
    });
    const entries = Object.entries(buckets).sort(([a], [b]) => {
      const [mA, dA] = a.split('/').map(Number);
      const [mB, dB] = b.split('/').map(Number);
      return mA - mB || dA - dB;
    });
    return entries.slice(-8).map(([date, count]) => ({ date, count }));
  }, [leads]);

  const api = apiStats || {};
  const stats = {
    totalProperties: api.totalProperties ?? properties.filter(p => !p.projectId).length,
    totalProjects: api.totalProjects ?? projects.length,
    rentalProperties: api.rentalProperties ?? properties.filter(p => p.listingType === 'Rental').length,
    resaleProperties: api.resaleProperties ?? properties.filter(p => p.listingType === 'Resale').length,
    projectUnits: api.projectUnits ?? properties.filter(p => !!p.projectId).length,
    totalLeads: api.totalLeads ?? leads.length,
    totalBookings: bookings.length,
    totalLandRequests: landRequests.length,
    featuredProperties: api.featuredProperties ?? properties.filter(p => p.isFeatured).length,
  };

  const statCards = [
    { title: "Properties", value: stats.totalProperties, icon: Home, desc: "Standalone listings", color: "text-primary", bg: "bg-primary/10" },
    { title: "Projects", value: stats.totalProjects, icon: Building2, desc: "Developments", color: "text-primary", bg: "bg-primary/10" },
    { title: "Project Units", value: stats.projectUnits, icon: Building2, desc: "Linked to projects", color: "text-status-contacted", bg: "bg-status-contacted/10" },
    { title: "Resale", value: stats.resaleProperties, icon: DollarSign, desc: "Resale listings", color: "text-status-closed", bg: "bg-status-closed/10" },
    { title: "Rental", value: stats.rentalProperties, icon: Key, desc: "Rental listings", color: "text-status-contacted", bg: "bg-status-contacted/10" },
    { title: "Featured", value: stats.featuredProperties, icon: Star, desc: "Pinned on homepage", color: "text-primary", bg: "bg-primary/10" },
    { title: "Leads", value: stats.totalLeads, icon: Users, desc: "All time", color: "text-status-new", bg: "bg-status-new/10" },
    { title: "Bookings", value: stats.totalBookings, icon: Calendar, desc: "Viewing requests", color: "text-status-new", bg: "bg-status-new/10" },
    { title: "Land Requests", value: stats.totalLandRequests, icon: Globe, desc: "All time", color: "text-muted-foreground", bg: "bg-accent" },
  ];

  const propertyByType = PROPERTY_TYPES.map(t => ({
    type: t.label,
    count: properties.filter(p => p.propertyType === t.value).length,
  })).filter(x => x.count > 0);

  const recentBookings = [...bookings].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()).slice(0, 5);
  const recentLandRequests = [...landRequests].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()).slice(0, 5);

  const formatDate = (iso: string) => new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });

  if (loading && !statsLoading && properties.length === 0) {
    return <div className="flex items-center justify-center py-20"><Loader2 className="w-8 h-8 animate-spin text-primary" /></div>;
  }

  return (
    <div className="space-y-6 animate-slide-in">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Dashboard</h2>
          <p className="text-muted-foreground mt-1">Your real estate command center.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => navigate('/admin/properties')}>
            <Plus className="mr-2 h-4 w-4" /> Add Property
          </Button>
          <Button variant="outline" size="sm" onClick={() => navigate('/admin/projects')}>
            <Plus className="mr-2 h-4 w-4" /> Add Project
          </Button>
          <Button size="sm" onClick={togglePreviewMode} className="bg-primary hover:bg-primary/90">
            <Eye className="mr-2 h-4 w-4" /> Preview Website
          </Button>
          {statsError && !statsLoading && (
            <Button variant="destructive" size="sm" onClick={fetchStats}>
              Retry Stats
            </Button>
          )}
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-3">
        {statCards.map((card, i) => (
          <Card key={i} className="bg-card border-border">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">{card.title}</CardTitle>
              <div className={`p-2 rounded-xl ${card.bg}`}>
                <card.icon className={`h-4 w-4 ${card.color}`} />
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold">{card.value}</div>
              <p className="text-xs text-muted-foreground mt-1">{card.desc}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-4 lg:grid-cols-7">
        <Card className="lg:col-span-4 bg-card border-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <TrendingUp className="h-5 w-5 text-primary" /> Lead Generation Trend
            </CardTitle>
          </CardHeader>
          <CardContent>
            <ChartContainer config={{ count: { label: "Leads", color: "hsl(var(--primary))" } }} className="w-full">
              <LineChart data={leadsChart.length ? leadsChart : [{ date: "No data", count: 0 }]}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                <XAxis dataKey="date" stroke="hsl(var(--muted-foreground))" fontSize={12} tickLine={false} axisLine={false} />
                <YAxis stroke="hsl(var(--muted-foreground))" fontSize={12} tickLine={false} axisLine={false} />
                <ChartTooltip content={<ChartTooltipContent />} />
                <Line type="monotone" dataKey="count" stroke="hsl(var(--primary))" strokeWidth={3} dot={false} activeDot={{ r: 6 }} />
              </LineChart>
            </ChartContainer>
          </CardContent>
        </Card>

        <Card className="lg:col-span-3 bg-card border-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Building2 className="h-5 w-5 text-primary" /> Properties by Type
            </CardTitle>
          </CardHeader>
          <CardContent>
            <ChartContainer config={{ count: { label: "Count", color: "hsl(var(--primary))" } }} className="w-full">
              <BarChart data={propertyByType}>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
                <XAxis dataKey="type" stroke="hsl(var(--muted-foreground))" fontSize={12} tickLine={false} axisLine={false} />
                <YAxis stroke="hsl(var(--muted-foreground))" fontSize={12} tickLine={false} axisLine={false} />
                <ChartTooltip content={<ChartTooltipContent />} />
                <Bar dataKey="count" fill="hsl(var(--primary))" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ChartContainer>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="bg-card border-border">
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2">
              <Calendar className="h-5 w-5 text-primary" /> Recent Booking Requests
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {recentBookings.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-8">No recent bookings</p>
            ) : recentBookings.map(b => (
              <div key={b.id} className="flex items-center justify-between p-3 rounded-xl bg-accent/30 border border-border">
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-medium truncate">{b.name}</p>
                  <p className="text-xs text-muted-foreground">{b.propertyCode} &bull; {formatDate(b.createdAt)}</p>
                </div>
                <Badge variant="outline" className="text-xs">{formatDate(b.preferredDate)}</Badge>
              </div>
            ))}
          </CardContent>
        </Card>

        <Card className="bg-card border-border">
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2">
              <Globe className="h-5 w-5 text-primary" /> Recent Land Requests
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {recentLandRequests.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-8">No recent land requests</p>
            ) : recentLandRequests.map(r => (
              <div key={r.id} className="flex items-center justify-between p-3 rounded-xl bg-accent/30 border border-border">
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-medium truncate">{r.name}</p>
                  <p className="text-xs text-muted-foreground">{r.location} &bull; {formatDate(r.createdAt)}</p>
                </div>
                <Badge variant="outline" className="text-xs">{r.maxPrice ? `${(r.maxPrice / 1000000).toFixed(1)}M EGP` : '\u2014'}</Badge>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
