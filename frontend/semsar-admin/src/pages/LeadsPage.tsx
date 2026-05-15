import { useState, useMemo, useEffect, useCallback, Fragment } from "react";
import { useStore } from "@/store";

import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Phone, MessageCircle, Copy, Trash2, Search, Loader2, Download, FileText, Home, ChevronDown, Eye, Monitor } from "lucide-react";
import { safeParseJson, safeHostname } from "@/lib/utils";

function safeUrl(url: string | null | undefined): string | undefined {
  if (!url) return undefined;
  try {
    const parsed = new URL(url);
    if (parsed.protocol === 'http:' || parsed.protocol === 'https:') return url;
    return undefined;
  } catch { return undefined; }
}

export default function LeadsPage() {
  const leads = useStore(s => s.leads);
  const properties = useStore(s => s.properties);
  const deleteLead = useStore(s => s.deleteLead);
  const loadLeads = useStore(s => s.loadLeads);
  const loadProperties = useStore(s => s.loadProperties);
  const [deleteTarget, setDeleteTarget] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [sourceFilter, setSourceFilter] = useState<string>("all");
  const [campaignFilter, setCampaignFilter] = useState<string>("all");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [boughtFilter, setBoughtFilter] = useState<"all" | "bought" | "not">("all");

  const [purchasedIds, setPurchasedIds] = useState<Set<string>>(() => {
    try {
      const stored = localStorage.getItem("purchasedLeads");
      return stored ? new Set(JSON.parse(stored)) : new Set();
    } catch { return new Set(); }
  });

  const togglePurchased = useCallback((id: string) => {
    setPurchasedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      localStorage.setItem("purchasedLeads", JSON.stringify([...next]));
      return next;
    });
  }, []);

  const [expandedId, setExpandedId] = useState<string | null>(null);

  const [initialLoading, setInitialLoading] = useState(true);

  useEffect(() => {
    setInitialLoading(true);
    Promise.all([loadLeads(), loadProperties()]).finally(() => setInitialLoading(false));
  }, [loadLeads, loadProperties]);

  const copyPhone = (phone: string) => {
    try {
      navigator.clipboard.writeText(phone);
      toast.success("Phone copied");
    } catch {
      toast.error("Failed to copy");
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    const apiId = parseInt(deleteTarget.replace("lead-", ""), 10);
    if (isNaN(apiId)) { setDeleteTarget(null); return; }
    try {
      await adminApi.deleteLead(apiId);
      deleteLead(deleteTarget);
      toast.success("Lead deleted");
    } catch { toast.error("Failed to delete"); }
    setDeleteTarget(null);
  };

  const formatCurrency = (v: number) => v ? v.toLocaleString() + " EGP" : "—";

  const formatDate = (iso: string) =>
    new Date(iso).toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });

  const enriched = useMemo(() => {
    const getPropByCode = (code?: string) => properties.find(p => p.code === code);
    return leads.map(l => {
      const prop = getPropByCode(l.propertyCode);
      return {
        ...l,
        id: `lead-${l.id}`,
        propertyType: prop?.propertyType,
        details: prop ? `${prop.propertyType} · ${prop.code}` : l.propertyCode || "—",
        priceLabel: prop ? formatCurrency(prop.price) : "—",
        priceValue: prop?.price ?? 0,
      };
    });
  }, [leads, properties]);

  const uniqueSources = useMemo(() => {
    const s = new Set(enriched.map(e => e.source));
    return ["all", ...Array.from(s).sort()];
  }, [enriched]);

  const uniqueCampaigns = useMemo(() => {
    const c = new Set(enriched.filter(e => e.campaign).map(e => e.campaign!));
    return ["all", ...Array.from(c).sort()];
  }, [enriched]);

  const filtered = useMemo(() => {
    let items = enriched;
    if (search) {
      const q = search.toLowerCase();
      items = items.filter(e => e.name.toLowerCase().includes(q) || e.phone.includes(q));
    }
    if (sourceFilter !== "all") items = items.filter(e => e.source === sourceFilter);
    if (campaignFilter !== "all") items = items.filter(e => e.campaign === campaignFilter);
    if (statusFilter !== "all") items = items.filter(e => e.status === statusFilter);
    if (boughtFilter === "bought") items = items.filter(e => purchasedIds.has(e.id));
    else if (boughtFilter === "not") items = items.filter(e => !purchasedIds.has(e.id));
    return items;
  }, [enriched, search, sourceFilter, campaignFilter, statusFilter, boughtFilter, purchasedIds]);

  const formatDuration = (sec?: number) => {
    if (!sec) return "";
    if (sec < 60) return `${sec}s`;
    if (sec < 3600) return `${Math.floor(sec / 60)}m ${sec % 60}s`;
    return `${Math.floor(sec / 3600)}h ${Math.floor((sec % 3600) / 60)}m`;
  };

  const exportExcel = async () => {
    try {
      const { saveAs } = await import("file-saver");
      const BOM = "\ufeff";
      const csv = BOM + ["Name,Phone", ...filtered.map(e => `"${e.name.replace(/"/g, '""')}","${e.phone.replace(/"/g, '""')}"`)].join("\r\n");
      saveAs(new Blob([csv], { type: "text/csv;charset=utf-8" }), `leads-${new Date().toISOString().slice(0, 10)}.csv`);
      toast.success("Leads exported to CSV");
    } catch { toast.error("Failed to export"); }
  };

  const exportPdf = async () => {
    try {
      const { jsPDF } = await import("jspdf");
      const doc = new jsPDF();
      doc.setFontSize(16);
      doc.text("Leads Report", 14, 20);
      doc.setFontSize(10);
      doc.text(`Date: ${new Date().toLocaleDateString()}`, 14, 28);
      let y = 38;
      doc.setFontSize(11);
      for (const e of filtered) {
        if (y > 275) { doc.addPage(); y = 20; }
        doc.setFont("helvetica", "bold");
        doc.text(e.name, 14, y);
        doc.setFont("helvetica", "normal");
        doc.text(e.phone, 80, y);
        y += 7;
      }
      const { saveAs } = await import("file-saver");
      saveAs(doc.output("blob"), `leads-${new Date().toISOString().slice(0, 10)}.pdf`);
      toast.success("Leads exported to PDF");
    } catch { toast.error("Failed to export PDF"); }
  };

  if (initialLoading) {
    return <div className="flex items-center justify-center py-20"><Loader2 className="w-8 h-8 animate-spin text-primary" /></div>;
  }

  return (
    <div className="space-y-6 animate-slide-in">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Leads</h2>
          <p className="text-muted-foreground mt-1">
            <span className="font-semibold text-foreground">{enriched.length}</span> total
            {purchasedIds.size > 0 && (
              <span className="ml-2 text-sm">· <span className="text-green-600 font-medium">{purchasedIds.size}</span> bought</span>
            )}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" className="gap-2 shrink-0" onClick={exportExcel}>
            <Download className="w-4 h-4" /> Excel
          </Button>
          <Button variant="outline" size="sm" className="gap-2 shrink-0" onClick={exportPdf}>
            <FileText className="w-4 h-4" /> PDF
          </Button>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative max-w-xs w-full">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Label htmlFor="leads-search" className="sr-only">Search leads</Label>
          <Input id="leads-search" autoComplete="off" placeholder="Search by name or phone..." value={search} onChange={e => setSearch(e.target.value)} className="pl-10" />
        </div>
        <label htmlFor="filter-source" className="sr-only">Filter by source</label>
        <Select value={sourceFilter} onValueChange={(v: string) => setSourceFilter(v)}>
          <SelectTrigger id="filter-source" className="w-36">
            <SelectValue placeholder="All Sources" />
          </SelectTrigger>
          <SelectContent>
            {uniqueSources.map(s => (
              <SelectItem key={s} value={s}>{s === "all" ? "All Sources" : s}</SelectItem>
            ))}
          </SelectContent>
        </Select>
        <label htmlFor="filter-campaign" className="sr-only">Filter by campaign</label>
        <Select value={campaignFilter} onValueChange={(v: string) => setCampaignFilter(v)}>
          <SelectTrigger id="filter-campaign" className="w-40">
            <SelectValue placeholder="All Campaigns" />
          </SelectTrigger>
          <SelectContent>
            {uniqueCampaigns.map(c => (
              <SelectItem key={c} value={c}>{c === "all" ? "All Campaigns" : c}</SelectItem>
            ))}
          </SelectContent>
        </Select>
        <label htmlFor="filter-status" className="sr-only">Filter by status</label>
        <Select value={statusFilter} onValueChange={(v: string) => setStatusFilter(v)}>
          <SelectTrigger id="filter-status" className="w-42">
            <SelectValue placeholder="All Statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="New">New</SelectItem>
            <SelectItem value="Contacted">Contacted</SelectItem>
            <SelectItem value="Interested">Interested</SelectItem>
            <SelectItem value="ViewingScheduled">Viewing Scheduled</SelectItem>
            <SelectItem value="Negotiating">Negotiating</SelectItem>
            <SelectItem value="ClosedWon">Closed Won</SelectItem>
            <SelectItem value="ClosedLost">Closed Lost</SelectItem>
          </SelectContent>
        </Select>
        <label htmlFor="filter-bought" className="sr-only">Filter by purchase status</label>
        <Select value={boughtFilter} onValueChange={(v: string) => setBoughtFilter(v)}>
          <SelectTrigger id="filter-bought" className="w-36">
            <SelectValue placeholder="All" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All</SelectItem>
            <SelectItem value="not">Not Bought</SelectItem>
            <SelectItem value="bought">Bought</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {filtered.length === 0 ? (
        <div className="text-center py-20 border-2 border-dashed border-border rounded-2xl">
          <Home className="mx-auto h-12 w-12 text-muted-foreground/30 mb-4" />
          <h3 className="text-lg font-medium">No leads found</h3>
        </div>
      ) : (
        <div className="border border-border rounded-xl overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow className="bg-accent/30">
                <TableHead className="w-10">Bought</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Phone</TableHead>
                <TableHead>Property</TableHead>
                <TableHead>Source</TableHead>
                <TableHead>Campaign</TableHead>
                <TableHead className="w-16">Views</TableHead>
                <TableHead>Referrer</TableHead>
                <TableHead>Date</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-28">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map(e => (
                <Fragment key={e.id}>
                  <TableRow className={purchasedIds.has(e.id) ? "bg-green-500/5" : ""}>
                  <TableCell>
                    <Checkbox checked={purchasedIds.has(e.id)} onCheckedChange={() => togglePurchased(e.id)} aria-label="Mark as bought" />
                  </TableCell>
                  <TableCell className="font-medium">{e.name}</TableCell>
                  <TableCell>
                    <a href={`tel:${e.phone}`} className="flex items-center gap-1 text-sm hover:text-primary">
                      <Phone className="w-3 h-3" />{e.phone}
                    </a>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground max-w-[200px] truncate" title={e.details}>
                    {e.details}
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className="text-xs font-mono">{e.source}</Badge>
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground max-w-[120px] truncate">{e.campaign || "—"}</TableCell>
                  <TableCell>
                    <Badge variant="outline" className="text-xs font-mono">{e.pageViews}</Badge>
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground max-w-[150px] truncate" title={safeHostname(e.referrer) || ""}>
                      {e.referrer ? (
                        <a href={safeUrl(e.referrer) ?? '#'} target="_blank" rel="noreferrer" className="hover:text-primary underline underline-offset-2 decoration-dotted">{safeHostname(e.referrer)}</a>
                      ) : "—"}
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground">{formatDate(e.createdAt)}</TableCell>
                  <TableCell>
                    <Badge variant="secondary" className={`text-xs whitespace-nowrap ${
                      ({
                        New: 'bg-blue-500/10 text-blue-600',
                        Contacted: 'bg-amber-500/10 text-amber-600',
                        Interested: 'bg-purple-500/10 text-purple-600',
                        ViewingScheduled: 'bg-cyan-500/10 text-cyan-600',
                        Negotiating: 'bg-orange-500/10 text-orange-600',
                        ClosedWon: 'bg-green-500/10 text-green-600',
                        ClosedLost: 'bg-rose-500/10 text-rose-600',
                      } as Record<string, string>)[e.status] || 'bg-muted text-muted-foreground'
                    }`}>{e.status}</Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button variant="outline" size="icon" className="h-8 w-8" asChild>
                        <a href={`tel:${e.phone}`} aria-label="Call"><Phone className="w-3 h-3" /></a>
                      </Button>
                      <Button variant="outline" size="icon" className="h-8 w-8" asChild>
                        <a href={`https://wa.me/${e.phone.replace(/\D/g, '')}`} target="_blank" rel="noreferrer" aria-label="WhatsApp">
                          <MessageCircle className="w-3 h-3 text-status-closed" />
                        </a>
                      </Button>
                      <Button variant="outline" size="icon" className="h-8 w-8" onClick={() => copyPhone(e.phone)} aria-label="Copy phone">
                        <Copy className="w-3 h-3" />
                      </Button>
                      <Button variant="outline" size="icon" className="h-8 w-8 text-destructive" onClick={() => setDeleteTarget(e.id)} aria-label="Delete">
                        <Trash2 className="w-3 h-3" />
                      </Button>
                      <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => setExpandedId(expandedId === e.id ? null : e.id)} aria-label="Tracking">
                        {expandedId === e.id ? <ChevronDown className="w-3 h-3" /> : <Eye className="w-3 h-3" />}
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
                {expandedId === e.id && (
                  <TableRow className="bg-accent/10">
                    <TableCell colSpan={11} className="p-4">
                      <div className="text-xs space-y-2">
                        <div className="font-medium text-sm mb-1 flex items-center gap-2">
                          <Monitor className="w-3.5 h-3.5" /> Tracking Details
                        </div>
                        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
                          <div>
                            <span className="text-muted-foreground">User Agent</span>
                            <p className="font-mono truncate" title={e.userAgent || ""}>{e.userAgent || "—"}</p>
                          </div>
                          <div>
                            <span className="text-muted-foreground">Session Duration</span>
                            <p className="font-mono">{formatDuration(e.sessionDuration) || "—"}</p>
                          </div>
                          <div>
                            <span className="text-muted-foreground">Last Referrer</span>
                            <p className="font-mono truncate" title={e.lastReferrer || ""}>{e.lastReferrer || "—"}</p>
                          </div>
                          <div>
                            <span className="text-muted-foreground">Landing Page</span>
                            <p className="font-mono truncate" title={e.landingPage || ""}>{e.landingPage || "—"}</p>
                          </div>
                          <div>
                            <span className="text-muted-foreground">First Visit</span>
                            <p className="font-mono">{e.firstVisitAt ? formatDate(e.firstVisitAt) : "—"}</p>
                          </div>
                          <div>
                            <span className="text-muted-foreground">Current Page</span>
                            <p className="font-mono truncate" title={e.currentPage || ""}>{e.currentPage || "—"}</p>
                          </div>
                        </div>
                            {(() => {
                              const history: Record<string, unknown>[] | null = safeParseJson(e.visitHistory, null);
                              if (!history) return null;
                              return (
                                <details className="mt-2">
                                  <summary className="cursor-pointer text-muted-foreground hover:text-foreground text-xs font-medium">Page Visit History ({history.length})</summary>
                                  <div className="mt-1 max-h-40 overflow-y-auto space-y-1">
                                    {history.map((v: Record<string, unknown>, _i: number) => (
                                      <div key={`${_i}-${v.timestamp ?? ''}-${v.path ?? ''}`} className="flex gap-2 text-xs text-muted-foreground border-b border-border/50 pb-1 last:border-0">
                                        <span className="font-mono shrink-0">{new Date(v.timestamp as string).toLocaleTimeString()}</span>
                                        <span className="truncate" title={v.path as string}>{v.path as string}</span>
                                        {v.title && <span className="truncate text-muted-foreground/60">— {v.title as string}</span>}
                                      </div>
                                    ))}
                                  </div>
                                </details>
                              );
                            })()}
                      </div>
                    </TableCell>
                  </TableRow>
                )}
              </Fragment>
            ))}
            </TableBody>
          </Table>
        </div>
      )}

      <AlertDialog open={!!deleteTarget} onOpenChange={o => !o && setDeleteTarget(null)}>
        <AlertDialogContent className="bg-card">
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this lead?</AlertDialogTitle>
            <AlertDialogDescription>This action cannot be undone.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-destructive hover:bg-destructive/90">Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}