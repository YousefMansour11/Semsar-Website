import { type Dispatch, type SetStateAction, useRef } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { ImagePlus, X } from "lucide-react";
import { adminApi } from "@/lib/admin-api";
import { toast } from "sonner";
import { PROPERTY_TYPES } from "@/store";

interface ProjectForm {
  nameEn: string; nameAr: string;
  descriptionEn: string; descriptionAr: string;
  location: string; locationAr: string; developer: string; unitCount: string;
  image: string;
  highlights: string[]; highlightInput: string;
  highlightsAr: string[]; highlightsArInput: string;
  startingPrice: string;
  nearbyPlaces: string[]; nearbyPlaceInput: string;
  nearbyPlacesAr: string[]; nearbyPlaceArInput: string;
  propertyTypes: string[];
  latitude: string;
  longitude: string;
  totalArea: string;
  ownershipType: string;
  deliveryText: string;
  deliveryTextAr: string;
  isRecommended: boolean;
  constructionStatus: string;
  availabilityStatus: string;
  virtualTourUrl: string;
  slug: string; slugIsAuto: boolean;
  seoTitle: string; seoDescription: string; seoKeywords: string;
  seoTitleAr: string; seoDescriptionAr: string; seoKeywordsAr: string;
}

import type { ReactNode } from "react";

interface Props {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  form: ProjectForm;
  setForm: Dispatch<SetStateAction<ProjectForm>>;
  onSave: () => void;
  title: string;
  saveLabel: string;
  videoUploadZone?: ReactNode;
}

function autoSlug(s: string) {
  return s.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
}

export function AddProjectDialog({ isOpen, onOpenChange, form, setForm, onSave, title, saveLabel, videoUploadZone }: Props) {
  const projectFileRef = useRef<HTMLInputElement>(null);

  const addHighlight = () => {
    if (form.highlightInput.trim()) {
      setForm(prev => ({ ...prev, highlights: [...prev.highlights, prev.highlightInput.trim()], highlightInput: '' }));
    }
  };
  const addHighlightAr = () => {
    if (form.highlightsArInput.trim()) {
      setForm(prev => ({ ...prev, highlightsAr: [...prev.highlightsAr, prev.highlightsArInput.trim()], highlightsArInput: '' }));
    }
  };
  const addNearbyPlace = () => {
    if (form.nearbyPlaceInput.trim()) {
      setForm(prev => ({ ...prev, nearbyPlaces: [...prev.nearbyPlaces, prev.nearbyPlaceInput.trim()], nearbyPlaceInput: '' }));
    }
  };
  const addNearbyPlaceAr = () => {
    if (form.nearbyPlaceArInput.trim()) {
      setForm(prev => ({ ...prev, nearbyPlacesAr: [...prev.nearbyPlacesAr, prev.nearbyPlaceArInput.trim()], nearbyPlaceArInput: '' }));
    }
  };

  const handleImageUpload = async (files: FileList | null) => {
    if (!files) return;
    for (const file of Array.from(files)) {
      try {
        const result = await adminApi.uploadImage(file, 'projects');
        setForm((prev) => ({ ...prev, image: result.url }));
      } catch {
        toast.error("Failed to upload image");
      }
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[700px] max-h-[90vh] overflow-y-auto bg-card">
        <DialogHeader><DialogTitle>{title}</DialogTitle></DialogHeader>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2"><Label htmlFor="proj-add-name-en">Name (EN) *</Label><Input id="proj-add-name-en" autoComplete="off" value={form.nameEn} onChange={(e) => setForm({ ...form, nameEn: e.target.value })} /></div>
            <div className="space-y-2"><Label htmlFor="proj-add-name-ar">Name (AR)</Label><Input id="proj-add-name-ar" autoComplete="off" dir="rtl" value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} /></div>
            <div className="space-y-2"><Label htmlFor="proj-add-location">Location *</Label><Input id="proj-add-location" autoComplete="off" value={form.location} onChange={(e) => setForm({ ...form, location: e.target.value })} /></div>
            <div className="space-y-2"><Label htmlFor="proj-add-location-ar">الموقع (Arabic)</Label><Input id="proj-add-location-ar" autoComplete="off" dir="rtl" value={form.locationAr} onChange={(e) => setForm({ ...form, locationAr: e.target.value })} /></div>
            <div className="space-y-2"><Label htmlFor="proj-add-developer">Developer</Label><Input id="proj-add-developer" autoComplete="organization" value={form.developer} onChange={(e) => setForm({ ...form, developer: e.target.value })} /></div>
            <div className="space-y-2"><Label htmlFor="proj-add-units">Total Units</Label><Input id="proj-add-units" autoComplete="off" type="number" value={form.unitCount} onChange={(e) => setForm({ ...form, unitCount: e.target.value })} /></div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="proj-add-desc-en">Description (EN)</Label>
              <Textarea id="proj-add-desc-en" value={form.descriptionEn} onChange={(e) => setForm({ ...form, descriptionEn: e.target.value })} rows={5} className="min-h-[120px]" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="proj-add-desc-ar">Description (AR)</Label>
              <Textarea id="proj-add-desc-ar" dir="rtl" value={form.descriptionAr} onChange={(e) => setForm({ ...form, descriptionAr: e.target.value })} rows={5} className="min-h-[120px]" />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="proj-add-highlights">Highlights</Label>
            <div className="flex gap-2">
              <Input id="proj-add-highlights" autoComplete="off" value={form.highlightInput} onChange={(e) => setForm({ ...form, highlightInput: e.target.value })}
                placeholder="e.g. Swimming Pool" onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addHighlight())} />
              <Button type="button" variant="outline" size="sm" onClick={addHighlight}>Add</Button>
            </div>
            <div className="flex flex-wrap gap-1.5">
              {form.highlights.map((h, i) => (
                <Badge key={i} variant="secondary" className="gap-1">
                  {h}
                  <button onClick={() => setForm(prev => ({ ...prev, highlights: prev.highlights.filter((_, idx) => idx !== i) }))} aria-label="Remove highlight"><X className="w-3 h-3" /></button>
                </Badge>
              ))}
            </div>
          </div>
          <div className="space-y-2" dir="rtl">
            <Label htmlFor="proj-add-highlights-ar">المميزات (Arabic)</Label>
            <div className="flex gap-2">
              <Input id="proj-add-highlights-ar" autoComplete="off" value={form.highlightsArInput} onChange={(e) => setForm({ ...form, highlightsArInput: e.target.value })}
                placeholder="e.g. حمام سباحة" onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addHighlightAr())} />
              <Button type="button" variant="outline" size="sm" onClick={addHighlightAr}>Add</Button>
            </div>
            <div className="flex flex-wrap gap-1.5">
              {form.highlightsAr.map((h, i) => (
                <Badge key={i} variant="secondary" className="gap-1">
                  {h}
                  <button onClick={() => setForm(prev => ({ ...prev, highlightsAr: prev.highlightsAr.filter((_, idx) => idx !== i) }))} aria-label="Remove highlight"><X className="w-3 h-3" /></button>
                </Badge>
              ))}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="proj-add-starting-price">Starting Price (EGP)</Label>
              <Input id="proj-add-starting-price" autoComplete="off" type="number" value={form.startingPrice} onChange={(e) => setForm({ ...form, startingPrice: e.target.value })} placeholder="e.g. 1200000" />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="proj-add-nearby-places">Nearby Places</Label>
            <div className="flex gap-2">
              <Input id="proj-add-nearby-places" autoComplete="off" value={form.nearbyPlaceInput} onChange={(e) => setForm({ ...form, nearbyPlaceInput: e.target.value })}
                placeholder="e.g. Mall, School" onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addNearbyPlace())} />
              <Button type="button" variant="outline" size="sm" onClick={addNearbyPlace}>Add</Button>
            </div>
            <div className="flex flex-wrap gap-1.5">
              {form.nearbyPlaces.map((p, i) => (
                <Badge key={i} variant="secondary" className="gap-1">
                  {p}
                  <button onClick={() => setForm(prev => ({ ...prev, nearbyPlaces: prev.nearbyPlaces.filter((_, idx) => idx !== i) }))} aria-label="Remove place"><X className="w-3 h-3" /></button>
                </Badge>
              ))}
            </div>
          </div>
          <div className="space-y-2" dir="rtl">
            <Label htmlFor="proj-add-nearby-places-ar">الأماكن القريبة (Arabic)</Label>
            <div className="flex gap-2">
              <Input id="proj-add-nearby-places-ar" autoComplete="off" value={form.nearbyPlaceArInput} onChange={(e) => setForm({ ...form, nearbyPlaceArInput: e.target.value })}
                placeholder="e.g. مول، مدرسة" onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addNearbyPlaceAr())} />
              <Button type="button" variant="outline" size="sm" onClick={addNearbyPlaceAr}>Add</Button>
            </div>
            <div className="flex flex-wrap gap-1.5">
              {form.nearbyPlacesAr.map((p, i) => (
                <Badge key={i} variant="secondary" className="gap-1">
                  {p}
                  <button onClick={() => setForm(prev => ({ ...prev, nearbyPlacesAr: prev.nearbyPlacesAr.filter((_, idx) => idx !== i) }))} aria-label="Remove place"><X className="w-3 h-3" /></button>
                </Badge>
              ))}
            </div>
          </div>
          <div className="space-y-2">
            <Label className="text-sm font-medium">Property Types</Label>
            <div className="flex flex-wrap gap-1.5">
              {PROPERTY_TYPES.map(pt => {
                const selected = form.propertyTypes.includes(pt.value);
                return (
                  <Badge key={pt.value} variant={selected ? "default" : "outline"} className="cursor-pointer"
                    onClick={() => setForm(prev => ({
                      ...prev,
                      propertyTypes: selected
                        ? prev.propertyTypes.filter(v => v !== pt.value)
                        : [...prev.propertyTypes, pt.value]
                    }))}>
                    {pt.label}
                  </Badge>
                );
              })}
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="proj-add-ownership-type">Ownership Type</Label>
            <select id="proj-add-ownership-type"
              className="flex h-10 w-full rounded-xl border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              value={form.ownershipType} onChange={(e) => setForm({ ...form, ownershipType: e.target.value })}>
              <option value="">None</option>
              <option value="GreenContract">Green Contract</option>
              <option value="Freehold">Freehold</option>
              <option value="Leasehold">Leasehold</option>
            </select>
          </div>
          <div className="grid grid-cols-3 gap-4">
            <div className="space-y-2">
              <Label htmlFor="proj-add-latitude">Latitude</Label>
              <Input id="proj-add-latitude" autoComplete="off" type="number" step="any" value={form.latitude}
                onChange={(e) => setForm({ ...form, latitude: e.target.value })} placeholder="e.g. 27.2578" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="proj-add-longitude">Longitude</Label>
              <Input id="proj-add-longitude" autoComplete="off" type="number" step="any" value={form.longitude}
                onChange={(e) => setForm({ ...form, longitude: e.target.value })} placeholder="e.g. 33.8116" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="proj-add-total-area">Total Area (sqm)</Label>
              <Input id="proj-add-total-area" autoComplete="off" type="number" step="any" value={form.totalArea}
                onChange={(e) => setForm({ ...form, totalArea: e.target.value })} placeholder="e.g. 50000" />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="proj-add-delivery-text">Delivery Text</Label>
              <Input id="proj-add-delivery-text" autoComplete="off" value={form.deliveryText} onChange={(e) => setForm({ ...form, deliveryText: e.target.value })} placeholder="e.g. Delivered Q2 2026" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="proj-add-delivery-text-ar">Delivery Text (AR)</Label>
              <Input id="proj-add-delivery-text-ar" autoComplete="off" dir="rtl" value={form.deliveryTextAr} onChange={(e) => setForm({ ...form, deliveryTextAr: e.target.value })} placeholder="مثال: التسليم الربع الثاني 2026" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="proj-add-virtual-tour-url">Virtual Tour URL</Label>
              <Input id="proj-add-virtual-tour-url" autoComplete="off" value={form.virtualTourUrl} onChange={(e) => setForm({ ...form, virtualTourUrl: e.target.value })} placeholder="https://tour.example.com" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="proj-add-construction-status">Construction Status</Label>
              <select id="proj-add-construction-status"
                className="flex h-10 w-full rounded-xl border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                value={form.constructionStatus} onChange={(e) => setForm({ ...form, constructionStatus: e.target.value })}>
                <option value="">None</option>
                <option value="Planned">Planned</option>
                <option value="UnderConstruction">Under Construction</option>
                <option value="NearDelivery">Near Delivery</option>
                <option value="Delivered">Delivered</option>
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="proj-add-availability-status">Availability Status</Label>
              <Input id="proj-add-availability-status" autoComplete="off" value={form.availabilityStatus} onChange={(e) => setForm({ ...form, availabilityStatus: e.target.value })} placeholder="Available" />
            </div>
          </div>
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <Switch id="proj-add-recommended" checked={form.isRecommended} onCheckedChange={(v) => setForm({ ...form, isRecommended: v })} />
              <Label htmlFor="proj-add-recommended">{"Recommended"}</Label>
            </div>
          </div>
          <div className="space-y-2">
            <Label className="text-sm font-medium">Project Image</Label>
            {form.image ? (
              <div className="relative w-full h-40 rounded-xl overflow-hidden border border-border">
                <img src={form.image} alt={form.nameEn || 'Project image'} loading="lazy" width={400} height={160} className="w-full h-full object-cover" />
                <button onClick={() => setForm({ ...form, image: '' })} aria-label="Remove image"
                  className="absolute top-2 right-2 p-1 bg-background/80 rounded-md"><X className="w-4 h-4 text-destructive" /></button>
              </div>
            ) : (
              <button onClick={() => projectFileRef.current?.click()} aria-label="Upload Image"
                className="w-full h-32 rounded-xl border-2 border-dashed border-border hover:border-primary flex flex-col items-center justify-center text-muted-foreground hover:text-primary transition-colors">
                <ImagePlus className="w-6 h-6" />
                <span className="text-xs mt-1">Upload Image</span>
              </button>
            )}
            <input ref={projectFileRef} id="apd-imageUpload" name="imageUpload" type="file" accept="image/*" className="hidden" onChange={(e) => handleImageUpload(e.target.files)} />
          </div>
          <Accordion type="single" collapsible>
            <AccordionItem value="seo">
              <AccordionTrigger className="text-sm">SEO & Slug</AccordionTrigger>
              <AccordionContent className="space-y-3 pt-2">
                <div className="flex items-center justify-between">
                  <Label htmlFor="proj-add-auto-slug" className="text-xs">Auto-generate slug from name</Label>
                  <Switch id="proj-add-auto-slug" checked={form.slugIsAuto} onCheckedChange={(v) => setForm({ ...form, slugIsAuto: v })} />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="proj-add-slug" className="text-xs">Slug</Label>
                  <Input id="proj-add-slug" autoComplete="off"
                    value={form.slugIsAuto ? autoSlug(form.nameEn) : form.slug}
                    disabled={form.slugIsAuto}
                    onChange={(e) => setForm({ ...form, slug: e.target.value })}
                  />
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1"><Label htmlFor="proj-add-seo-title-en" className="text-xs">SEO Title (EN)</Label><Input id="proj-add-seo-title-en" autoComplete="off" value={form.seoTitle} onChange={(e) => setForm({ ...form, seoTitle: e.target.value })} placeholder="Auto-generated if empty" /></div>
                  <div className="space-y-1"><Label htmlFor="proj-add-seo-title-ar" className="text-xs">SEO Title (AR)</Label><Input id="proj-add-seo-title-ar" autoComplete="off" dir="rtl" value={form.seoTitleAr} onChange={(e) => setForm({ ...form, seoTitleAr: e.target.value })} /></div>
                  <div className="space-y-1"><Label htmlFor="proj-add-seo-desc-en" className="text-xs">SEO Description (EN)</Label><Textarea id="proj-add-seo-desc-en" value={form.seoDescription} onChange={(e) => setForm({ ...form, seoDescription: e.target.value })} rows={2} /></div>
                  <div className="space-y-1"><Label htmlFor="proj-add-seo-desc-ar" className="text-xs">SEO Description (AR)</Label><Textarea id="proj-add-seo-desc-ar" dir="rtl" value={form.seoDescriptionAr} onChange={(e) => setForm({ ...form, seoDescriptionAr: e.target.value })} rows={2} /></div>
                  <div className="space-y-1"><Label htmlFor="proj-add-seo-keywords-en" className="text-xs">Keywords (EN)</Label><Input id="proj-add-seo-keywords-en" autoComplete="off" value={form.seoKeywords} onChange={(e) => setForm({ ...form, seoKeywords: e.target.value })} placeholder="comma, separated" /></div>
                  <div className="space-y-1"><Label htmlFor="proj-add-seo-keywords-ar" className="text-xs">Keywords (AR)</Label><Input id="proj-add-seo-keywords-ar" autoComplete="off" dir="rtl" value={form.seoKeywordsAr} onChange={(e) => setForm({ ...form, seoKeywordsAr: e.target.value })} /></div>
                </div>
                <p className="text-[11px] text-muted-foreground">Note: backend auto-generates SEO when fields are empty.</p>
              </AccordionContent>
            </AccordionItem>
          </Accordion>
          {videoUploadZone && <div className="border-t border-border pt-4">{videoUploadZone}</div>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button onClick={onSave} className="bg-primary hover:bg-primary/90">{saveLabel}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
