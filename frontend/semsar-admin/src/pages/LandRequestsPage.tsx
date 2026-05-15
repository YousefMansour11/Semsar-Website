import { useState, useMemo, useEffect, Fragment } from "react";
import { useStore } from "@/store";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from "@/components/ui/dialog";
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Plus, Search, Globe, Phone, Trash2, Loader2, ChevronDown, Eye, Monitor, Copy, MessageCircle } from "lucide-react";
import { safeParseJson, safeHostname } from "@/lib/utils";

export default function LandRequestsPage() {
  const landRequests = useStore(s => s.landRequests);
  const addLandRequest = useStore(s => s.addLandRequest);
  const deleteLandRequest = useStore(s => s.deleteLandRequest);
  const loadLandRequests = useStore(s => s.loadLandRequests);

  const [initialLoading, setInitialLoading] = useState(true);

  useEffect(() => {
    setInitialLoading(true);
    loadLandRequests().finally(() => setInitialLoading(false));
  }, [loadLandRequests]);
  const [search, setSearch] = useState("");
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: '', phone: '', location: '',
    minPrice: '', maxPrice: '', minArea: '', maxArea: '', notes: '',
  });

  const filtered = useMemo(() =>
    landRequests
      .filter(r => r.name.toLowerCase().includes(search.toLowerCase()) || r.location.toLowerCase().includes(search.toLowerCase()))
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()),
    [landRequests, search]
  );

  const handleAdd = async () => {
    if (!form.name || !form.phone || !form.location) {
      toast.error("Please fill in required fields");
      return;
    }
    try {
      await adminApi.createLandRequest({
        name: form.name, phone: form.phone, location: form.location,
        minPrice: Number(form.minPrice) || undefined,
        maxPrice: Number(form.maxPrice) || undefined,
        minArea: Number(form.minArea) || undefined,
        maxArea: Number(form.maxArea) || undefined,
        notes: form.notes || undefined,
      });
      addLandRequest({
        name: form.name, phone: form.phone, location: form.location,
        minPrice: Number(form.minPrice) || 0, maxPrice: Number(form.maxPrice) || 0,
        minArea: Number(form.minArea) || 0, maxArea: Number(form.maxArea) || 0,
        notes: form.notes,
      });
      toast.success("Land request created");
      setIsAddOpen(false);
      setForm({ name: '', phone: '', location: '', minPrice: '', maxPrice: '', minArea: '', maxArea: '', notes: '' });
    } catch {
      toast.error("Failed to create land request");
    }
  };

  const handleDelete = async () => {
    if (deleteId) {
      const apiId = parseInt(deleteId, 10);
      if (!isNaN(apiId)) {
        try {
          await adminApi.deleteLandRequest(apiId);
        } catch {
          toast.error("Failed to delete land request");
          return;
        }
      }
      deleteLandRequest(deleteId);
      toast.success("Land request deleted");
      setDeleteId(null);
    }
  };

  const formatDate = (iso: string) => new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  const formatDuration = (sec?: number) => {
    if (!sec) return "";
    if (sec < 60) return `${sec}s`;
    if (sec < 3600) return `${Math.floor(sec / 60)}m ${sec % 60}s`;
    return `${Math.floor(sec / 3600)}h ${Math.floor((sec % 3600) / 60)}m`;
  };
  const copyPhone = (phone: string) => {
    try {
      navigator.clipboard.writeText(phone);
      toast.success("Phone copied");
    } catch {
      toast.error("Failed to copy");
    }
  };

  const fmt = (n: number) => n ? n.toLocaleString() : '—';

  if (initialLoading) {
    return <div className="flex items-center justify-center py-20"><Loader2 className="w-8 h-8 animate-spin text-primary" /></div>;
  }

  return (
    <div className="space-y-6 animate-slide-in">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Land Requests</h2>
          <p className="text-muted-foreground mt-1">{filtered.length} request{filtered.length !== 1 ? 's' : ''}</p>
        </div>
        <Button onClick={() => setIsAddOpen(true)} className="bg-primary hover:bg-primary/90">
          <Plus className="mr-2 h-4 w-4" /> Add Request
        </Button>
      </div>

      <div className="flex flex-wrap gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Label htmlFor="land-requests-search" className="sr-only">Search land requests</Label>
          <Input id="land-requests-search" autoComplete="off" placeholder="Search by name or location..." value={search} onChange={(e) => setSearch(e.target.value)} className="pl-10" />
        </div>
      </div>

      {filtered.length === 0 ? (
        <div className="text-center py-20 border-2 border-dashed border-border rounded-2xl">
          <Globe className="mx-auto h-12 w-12 text-muted-foreground/30 mb-4" />
          <h3 className="text-lg font-medium">No land requests found</h3>
        </div>
      ) : (
        <div className="border border-border rounded-xl overflow-x-auto">
          <Table>
            <caption className="sr-only">Land Requests</caption>
            <TableHeader>
              <TableRow className="bg-accent/30">
                <TableHead>Date</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Phone</TableHead>
                <TableHead>Location</TableHead>
                <TableHead>Price Range</TableHead>
                <TableHead>Area Range</TableHead>
                <TableHead>Source</TableHead>
                <TableHead>Campaign</TableHead>
                <TableHead className="w-16">Views</TableHead>
                <TableHead>Referrer</TableHead>
                <TableHead>Notes</TableHead>
                <TableHead className="w-24">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map(r => (
                <Fragment key={r.id}>
                  <TableRow>
                    <TableCell className="text-xs text-muted-foreground">{formatDate(r.createdAt)}</TableCell>
                    <TableCell className="font-medium">{r.name}</TableCell>
                    <TableCell>
                      <a href={`tel:${r.phone}`} className="flex items-center gap-1 text-sm hover:text-primary">
                        <Phone className="w-3 h-3" />{r.phone}
                      </a>
                    </TableCell>
                    <TableCell>{r.location}</TableCell>
                    <TableCell className="text-sm">{fmt(r.minPrice)} – {fmt(r.maxPrice)}</TableCell>
                    <TableCell className="text-sm">{fmt(r.minArea)} – {fmt(r.maxArea)}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className="text-xs font-mono">{r.source}</Badge>
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground max-w-[120px] truncate">{r.campaign || "—"}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className="text-xs font-mono">{r.pageViews}</Badge>
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground max-w-[150px] truncate" title={r.referrer || ""}>
                      {r.referrer ? (
                        <a href={r.referrer} target="_blank" rel="noreferrer" className="hover:text-primary underline underline-offset-2 decoration-dotted">{safeHostname(r.referrer)}</a>
                      ) : "—"}
                    </TableCell>
                    <TableCell className="max-w-[150px] truncate text-sm text-muted-foreground">{r.notes}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button variant="outline" size="icon" className="h-8 w-8" asChild>
                          <a href={`tel:${r.phone}`} aria-label="Call"><Phone className="w-3 h-3" /></a>
                        </Button>
                        <Button variant="outline" size="icon" className="h-8 w-8" asChild>
                          <a href={`https://wa.me/${r.phone.replace(/\D/g, '')}`} target="_blank" rel="noreferrer" aria-label="WhatsApp">
                            <MessageCircle className="w-3 h-3 text-status-closed" />
                          </a>
                        </Button>
                        <Button variant="outline" size="icon" className="h-8 w-8" onClick={() => copyPhone(r.phone)} aria-label="Copy phone">
                          <Copy className="w-3 h-3" />
                        </Button>
                        <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive" onClick={() => setDeleteId(r.id)} aria-label="Delete request">
                          <Trash2 className="w-3.5 h-3.5" />
                        </Button>
                        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => setExpandedId(expandedId === r.id ? null : r.id)} aria-label="View tracking">
                          {expandedId === r.id ? <ChevronDown className="w-3 h-3" /> : <Eye className="w-3 h-3" />}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                  {expandedId === r.id && (
                    <TableRow className="bg-accent/10">
                      <TableCell colSpan={12} className="p-4">
                        <div className="text-xs space-y-2">
                          <div className="font-medium text-sm mb-1 flex items-center gap-2">
                            <Monitor className="w-3.5 h-3.5" /> Tracking Details
                          </div>
                          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
                            <div>
                              <span className="text-muted-foreground">Medium</span>
                              <p className="font-mono">{r.medium || "—"}</p>
                            </div>
                            <div>
                              <span className="text-muted-foreground">Term</span>
                              <p className="font-mono">{r.term || "—"}</p>
                            </div>
                            <div>
                              <span className="text-muted-foreground">Content</span>
                              <p className="font-mono">{r.content || "—"}</p>
                            </div>
                            <div>
                              <span className="text-muted-foreground">User Agent</span>
                              <p className="font-mono truncate" title={r.userAgent || ""}>{r.userAgent || "—"}</p>
                            </div>
                            <div>
                              <span className="text-muted-foreground">Session Duration</span>
                              <p className="font-mono">{formatDuration(r.sessionDuration) || "—"}</p>
                            </div>
                            <div>
                              <span className="text-muted-foreground">Last Referrer</span>
                              <p className="font-mono truncate" title={r.lastReferrer || ""}>{r.lastReferrer || "—"}</p>
                            </div>
                            <div>
                              <span className="text-muted-foreground">Landing Page</span>
                              <p className="font-mono truncate" title={r.landingPage || ""}>{r.landingPage || "—"}</p>
                            </div>
                            <div>
                              <span className="text-muted-foreground">First Visit</span>
                              <p className="font-mono">{r.firstVisitAt ? formatDate(r.firstVisitAt) : "—"}</p>
                            </div>
                            <div>
                              <span className="text-muted-foreground">Current Page</span>
                              <p className="font-mono truncate" title={r.currentPage || ""}>{r.currentPage || "—"}</p>
                            </div>
                          </div>
                          {(() => {
                            const history: Record<string, unknown>[] | null = safeParseJson(r.visitHistory, null);
                            if (!history) return null;
                            return (
                              <details className="mt-2">
                                <summary className="cursor-pointer text-muted-foreground hover:text-foreground text-xs font-medium">Page Visit History ({history.length})</summary>
                                <div className="mt-1 max-h-40 overflow-y-auto space-y-1">
                                  {history.map((v: Record<string, unknown>, _i: number) => (
                                    <div key={(v.timestamp as string) + '-' + (v.path as string)} className="flex gap-2 text-xs text-muted-foreground border-b border-border/50 pb-1 last:border-0">
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

      <Dialog open={isAddOpen} onOpenChange={setIsAddOpen}>
        <DialogContent className="sm:max-w-[500px] bg-card">
          <DialogHeader><DialogTitle>Add Land Request</DialogTitle></DialogHeader>
          <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-2"><Label htmlFor="lr-name">Name</Label><Input id="lr-name" autoComplete="name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></div>
                <div className="space-y-2"><Label htmlFor="lr-phone">Phone</Label><Input id="lr-phone" autoComplete="tel" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} placeholder="+201XXXXXXXXX" /></div>
              </div>
              <div className="space-y-2"><Label htmlFor="lr-location">Location</Label><Input id="lr-location" autoComplete="street-address" value={form.location} onChange={(e) => setForm({ ...form, location: e.target.value })} /></div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-2"><Label htmlFor="lr-min-price">Min Price (EGP)</Label><Input id="lr-min-price" autoComplete="off" type="number" value={form.minPrice} onChange={(e) => setForm({ ...form, minPrice: e.target.value })} /></div>
                <div className="space-y-2"><Label htmlFor="lr-max-price">Max Price (EGP)</Label><Input id="lr-max-price" autoComplete="off" type="number" value={form.maxPrice} onChange={(e) => setForm({ ...form, maxPrice: e.target.value })} /></div>
                <div className="space-y-2"><Label htmlFor="lr-min-area">Min Area (m²)</Label><Input id="lr-min-area" autoComplete="off" type="number" value={form.minArea} onChange={(e) => setForm({ ...form, minArea: e.target.value })} /></div>
                <div className="space-y-2"><Label htmlFor="lr-max-area">Max Area (m²)</Label><Input id="lr-max-area" autoComplete="off" type="number" value={form.maxArea} onChange={(e) => setForm({ ...form, maxArea: e.target.value })} /></div>
              </div>
              <div className="space-y-2"><Label htmlFor="lr-notes">Notes</Label><Textarea id="lr-notes" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} rows={3} /></div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddOpen(false)}>Cancel</Button>
            <Button onClick={handleAdd} className="bg-primary hover:bg-primary/90">Create Request</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <AlertDialog open={!!deleteId} onOpenChange={(o) => !o && setDeleteId(null)}>
        <AlertDialogContent className="bg-card">
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Request?</AlertDialogTitle>
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
