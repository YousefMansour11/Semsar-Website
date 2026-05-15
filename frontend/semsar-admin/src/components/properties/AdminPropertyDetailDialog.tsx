import { type Property, useStore } from "@/store";
import { type UnitVariantDto } from "@/lib/api-types";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { MapPin, Hash, ImagePlus, User, Maximize2, Clock, Building, Bed, Bath, ArrowUpDown, Copy, Check, ChevronDown, ChevronUp, Video, Play, Eye, MessageCircle, Heart, Globe, Star } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useState } from "react";
import { toast } from "sonner";
import { cn, optimizeCloudinaryUrl } from "@/lib/utils";

interface Props {
  property: Property | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function AdminPropertyDetailDialog({ property: p, open, onOpenChange }: Props) {
  const contacts = useStore(s => s.contacts);
  const [imgIdx, setImgIdx] = useState(0);
  const [descExpanded, setDescExpanded] = useState(false);
  const [selVariantIdx, setSelVariantIdx] = useState(0);
  const [selPlanIdx] = useState(0);

  const handleCopyPhone = async (phone: string) => {
    try {
      await navigator.clipboard.writeText(phone);
      toast.success("Phone number copied");
    } catch {
      toast.error("Failed to copy");
    }
  };

  if (!p) return null;

  const contact = contacts.find((c) => c.id === p.contactId);
  const directName = p.contactName;
  const directPhone = p.contactPhone;
  const displayName = contact?.name || directName || '';
  const displayPhone = contact?.phone || directPhone || '';
  const contactType = contact?.type || '';
  const hasImages = (p.images?.length ?? 0) > 0;
  const enabledInstallments = p.installments?.filter(i => i.isEnabled) ?? [];

  const variants = (p as unknown as { variants?: UnitVariantDto[] }).variants;
  const hasVariants = variants && variants.length > 0;
  const selVariant = hasVariants ? variants![Math.min(selVariantIdx, variants!.length - 1)] : null;
  const variantPrice = selVariant?.price ?? 0;
  const basePrice = hasVariants ? variantPrice : (p.price || p.minPrice || 0);

  const selPlan = selVariant && enabledInstallments.length > 0
    ? enabledInstallments[Math.min(selPlanIdx, enabledInstallments.length - 1)]
    : null;
  const financing = selVariant && selPlan && selPlan.paymentType !== "Cash"
    ? {
        downPaymentAmount: variantPrice * selPlan.downPaymentPercent / 100,
        remainingAmount: variantPrice * (1 - selPlan.downPaymentPercent / 100),
        monthlyInstallment: selPlan.years > 0
          ? (variantPrice * (1 - selPlan.downPaymentPercent / 100)) / (selPlan.years * 12)
          : 0,
      }
    : null;

  const maxIdx = (p.images?.length ?? 1) - 1;
  const prev = () => setImgIdx((i) => (i > 0 ? i - 1 : maxIdx));
  const next = () => setImgIdx((i) => (i < maxIdx ? i + 1 : 0));

  return (
      <Dialog open={open} onOpenChange={(o) => { onOpenChange(o); setImgIdx(0); setDescExpanded(false); }}>
      <DialogContent className="bg-card max-w-4xl p-0 max-h-[90vh] overflow-y-auto overflow-x-hidden">
        <div className="relative h-64 bg-accent overflow-hidden">
          {hasImages ? (
            <>
              <img src={optimizeCloudinaryUrl(p.images[imgIdx] ?? p.images[0], 800)} alt={p.title} loading="lazy" width={800} height={600} className="w-full h-full object-cover" />
              {p.images.length > 1 && (
                <>
                  <button onClick={prev} aria-label="Previous image" className="absolute left-3 top-1/2 -translate-y-1/2 w-9 h-9 rounded-full bg-background/70 backdrop-blur-sm flex items-center justify-center hover:bg-background/90 transition-colors">
                    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" /></svg>
                  </button>
                  <button onClick={next} aria-label="Next image" className="absolute right-3 top-1/2 -translate-y-1/2 w-9 h-9 rounded-full bg-background/70 backdrop-blur-sm flex items-center justify-center hover:bg-background/90 transition-colors">
                    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" /></svg>
                  </button>
                  <div className="absolute bottom-3 left-1/2 -translate-x-1/2 flex gap-1.5">
                    {p.images.map((_, i) => (
                      <button key={i} onClick={() => setImgIdx(i)} aria-label={`Image ${i + 1}`}
                        className={cn("w-2 h-2 rounded-full transition-all", i === imgIdx ? "bg-primary w-5" : "bg-background/60")} />
                    ))}
                  </div>
                </>
              )}
            </>
          ) : (
            <div className="w-full h-full flex items-center justify-center text-muted-foreground">
              <ImagePlus className="w-12 h-12" />
            </div>
          )}
          <div className="absolute top-3 right-3 flex gap-2">
            {imgIdx === 0 && <Badge className="bg-amber-500 text-white text-xs border-0">Hero</Badge>}
            {p.isFeatured && <Badge className="bg-primary text-primary-foreground text-xs">Featured</Badge>}
            {p.isRecommended && <Badge className="bg-emerald-500 text-white text-xs"><Star className="w-3 h-3 mr-1" />Recommended</Badge>}
            <Badge variant="secondary" className="bg-background/80 backdrop-blur-sm text-xs">{p.listingType}</Badge>
          </div>
        </div>

        <div className="p-6 space-y-5">
          <DialogHeader className="p-0">
            <div className="flex items-center justify-between gap-2 mb-1">
              <div className="flex items-center gap-2 flex-wrap">
                <Badge variant="outline" className="text-xs font-mono"><Hash className="w-3 h-3 mr-1" />{p.code}</Badge>
                <Badge variant="outline" className="text-xs">{p.propertyType}</Badge>
                {p.size > 0 && <Badge variant="outline" className="text-xs gap-1"><Maximize2 className="w-3 h-3" /> {p.size} m²</Badge>}
                {p.projectName && <Badge variant="outline" className="text-xs gap-1"><Building className="w-3 h-3" /> {p.projectName}</Badge>}
              </div>
            </div>
            <DialogTitle className="text-2xl font-bold">{p.title}</DialogTitle>
          </DialogHeader>

          <div className="space-y-1">
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <MapPin className="w-4 h-4 shrink-0" /> {p.location}
            </div>
            {p.locationAr && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground" dir="rtl">
                <MapPin className="w-4 h-4 shrink-0" /> {p.locationAr}
              </div>
            )}
          </div>

          <p className="text-3xl font-bold text-primary">
            {hasVariants && selVariant
              ? `${basePrice.toLocaleString()} ${p.currency}`
              : p.listingType === 'Rental'
                ? `${(p.rentPerMonth || p.price).toLocaleString()} ${p.currency}/mo`
                : `${p.price.toLocaleString()} ${p.currency}`}
            {hasVariants && !selVariant && <span className="text-sm text-muted-foreground block font-normal">Select a variant below</span>}
          </p>

          {p.description && (
            <div>
              <p className="text-sm text-muted-foreground whitespace-pre-wrap break-all" style={{ maxHeight: descExpanded ? 'none' : '5.6em', overflow: 'hidden' }}>
                {descExpanded ? p.description : p.description.slice(0, 200)}{descExpanded ? '' : p.description.length > 200 ? '...' : ''}
              </p>
              {p.description.length > 200 && (
                <button onClick={() => setDescExpanded(!descExpanded)} className="flex items-center gap-1 text-xs text-primary mt-1 hover:underline">
                  {descExpanded ? <>Show less <ChevronUp className="w-3 h-3" /></> : <>Show more <ChevronDown className="w-3 h-3" /></>}
                </button>
              )}
            </div>
          )}

          {/* Admin metadata */}
          <div className="grid grid-cols-2 gap-3 text-xs">
            <div className="bg-accent/50 rounded-lg p-3">
              <span className="text-muted-foreground block">ID</span>
              <span className="font-mono font-medium">{p.id.replace('u-', '')}</span>
            </div>
            <div className="bg-accent/50 rounded-lg p-3">
              <span className="text-muted-foreground block">Code</span>
              <span className="font-mono font-medium">{p.code || '—'}</span>
            </div>
            <div className="bg-accent/50 rounded-lg p-3">
              <span className="text-muted-foreground block">Listing Type</span>
              <span className="font-medium">{p.listingType}</span>
            </div>
            <div className="bg-accent/50 rounded-lg p-3">
              <span className="text-muted-foreground block">Featured</span>
              <span className="font-medium">{p.isFeatured ? 'Yes' : 'No'}</span>
            </div>
            <div className="bg-accent/50 rounded-lg p-3">
              <span className="text-muted-foreground block">Recommended</span>
              <span className="font-medium">{p.isRecommended ? 'Yes' : 'No'}</span>
            </div>
            {p.deliveryText && (
              <div className="bg-accent/50 rounded-lg p-3">
                <span className="text-muted-foreground block">Delivery Text</span>
                <span className="font-medium">{p.deliveryText}</span>
              </div>
            )}
            {p.constructionStatus && (
              <div className="bg-accent/50 rounded-lg p-3">
                <span className="text-muted-foreground block">Construction Status</span>
                <span className="font-medium">{p.constructionStatus}</span>
              </div>
            )}
            {p.availabilityStatus && (
              <div className="bg-accent/50 rounded-lg p-3">
                <span className="text-muted-foreground block">Availability</span>
                <span className="font-medium">{p.availabilityStatus}</span>
              </div>
            )}
            {p.ownershipType && (
              <div className="bg-accent/50 rounded-lg p-3">
                <span className="text-muted-foreground block">Ownership</span>
                <span className="font-medium">{p.ownershipType}</span>
              </div>
            )}
            {p.projectName && (
              <div className="bg-accent/50 rounded-lg p-3">
                <span className="text-muted-foreground block">Project</span>
                <span className="font-medium">{p.projectName}</span>
              </div>
            )}
            {p.slug && (
              <div className={`bg-accent/50 rounded-lg p-3 ${p.projectName ? '' : 'col-span-2'}`}>
                <span className="text-muted-foreground block">Slug</span>
                <span className="font-mono font-medium break-all">/{p.slug}</span>
              </div>
            )}
          </div>

          {/* Engagement stats */}
          {(p.viewCount != null || p.inquiryCount != null || p.favoriteCount != null) && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Engagement</p>
              <div className="flex gap-3">
                {p.viewCount != null && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Eye className="w-4 h-4 text-muted-foreground" />
                    <div><span className="text-muted-foreground text-xs block">Views</span><span className="font-medium text-sm">{p.viewCount}</span></div>
                  </div>
                )}
                {p.inquiryCount != null && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <MessageCircle className="w-4 h-4 text-muted-foreground" />
                    <div><span className="text-muted-foreground text-xs block">Inquiries</span><span className="font-medium text-sm">{p.inquiryCount}</span></div>
                  </div>
                )}
                {p.favoriteCount != null && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Heart className="w-4 h-4 text-muted-foreground" />
                    <div><span className="text-muted-foreground text-xs block">Favorites</span><span className="font-medium text-sm">{p.favoriteCount}</span></div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Virtual Tour */}
          {p.virtualTourUrl && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Virtual Tour</p>
              <a href={p.virtualTourUrl} target="_blank" rel="noopener noreferrer"
                className="flex items-center gap-2 text-sm text-primary hover:underline">
                <Globe className="w-4 h-4" /> {p.virtualTourUrl}
              </a>
            </div>
          )}

          {/* Arabic highlights */}
          {p.highlightsAr && p.highlightsAr.length > 0 && (
            <div className="space-y-2" dir="rtl">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Highlights (AR)</p>
              <div className="flex flex-wrap gap-1.5">
                {p.highlightsAr.map((h, i) => <Badge key={i} variant="secondary">{h}</Badge>)}
              </div>
            </div>
          )}

          {/* Nearby places */}
          {p.nearbyPlaces && p.nearbyPlaces.length > 0 && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Nearby Places</p>
              <div className="flex flex-wrap gap-1.5">
                {p.nearbyPlaces.map((n, i) => <Badge key={i} variant="outline">{n}</Badge>)}
              </div>
            </div>
          )}

          {/* Nearby places AR */}
          {p.nearbyPlacesAr && p.nearbyPlacesAr.length > 0 && (
            <div className="space-y-2" dir="rtl">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Nearby Places (AR)</p>
              <div className="flex flex-wrap gap-1.5">
                {p.nearbyPlacesAr.map((n, i) => <Badge key={i} variant="outline">{n}</Badge>)}
              </div>
            </div>
          )}

          {/* Real Estate Details */}
          {((p.bedrooms != null && p.bedrooms > 0) || (p.bathrooms != null && p.bathrooms > 0) || p.floor != null || p.totalFloors != null || p.isFurnished || (p.view && p.view !== 'Unknown') || p.unitNumber || p.buildingNumber || p.deliveryDate || p.finishingType || p.hasBalcony || p.hasParking) && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Real Estate Details</p>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                {p.bedrooms != null && p.bedrooms > 0 && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Bed className="w-4 h-4 text-muted-foreground" />
                    <div><span className="text-muted-foreground text-xs block">Bedrooms</span><span className="font-medium text-sm">{p.bedrooms}</span></div>
                  </div>
                )}
                {p.bathrooms != null && p.bathrooms > 0 && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Bath className="w-4 h-4 text-muted-foreground" />
                    <div><span className="text-muted-foreground text-xs block">Bathrooms</span><span className="font-medium text-sm">{p.bathrooms}</span></div>
                  </div>
                )}
                {p.floor != null && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <ArrowUpDown className="w-4 h-4 text-muted-foreground" />
                    <div><span className="text-muted-foreground text-xs block">Floor</span><span className="font-medium text-sm">{p.floor}{p.totalFloors != null ? ` / ${p.totalFloors}` : ''}</span></div>
                  </div>
                )}
                {p.isFurnished && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Check className="w-4 h-4 text-muted-foreground shrink-0" />
                    <div><span className="text-muted-foreground text-xs block">Furnished</span><span className="font-medium text-sm">Yes</span></div>
                  </div>
                )}
                {p.view && p.view !== 'Unknown' && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <MapPin className="w-4 h-4 text-muted-foreground shrink-0" />
                    <div><span className="text-muted-foreground text-xs block">View</span><span className="font-medium text-sm">{p.view}</span></div>
                  </div>
                )}
                {p.unitNumber && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Hash className="w-4 h-4 text-muted-foreground shrink-0" />
                    <div><span className="text-muted-foreground text-xs block">Unit #</span><span className="font-medium text-sm">{p.unitNumber}</span></div>
                  </div>
                )}
                {p.buildingNumber && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Building className="w-4 h-4 text-muted-foreground shrink-0" />
                    <div><span className="text-muted-foreground text-xs block">Building #</span><span className="font-medium text-sm">{p.buildingNumber}</span></div>
                  </div>
                )}
                {p.deliveryDate && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Clock className="w-4 h-4 text-muted-foreground shrink-0" />
                    <div><span className="text-muted-foreground text-xs block">Delivery</span><span className="font-medium text-sm">{new Date(p.deliveryDate).toLocaleDateString()}</span></div>
                  </div>
                )}
                {p.finishingType && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Check className="w-4 h-4 text-muted-foreground shrink-0" />
                    <div><span className="text-muted-foreground text-xs block">Finishing</span><span className="font-medium text-sm">{p.finishingType}</span></div>
                  </div>
                )}
                {p.hasBalcony && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Check className="w-4 h-4 text-muted-foreground shrink-0" />
                    <div><span className="text-muted-foreground text-xs block">Balcony</span><span className="font-medium text-sm">Yes</span></div>
                  </div>
                )}
                {p.hasParking && (
                  <div className="bg-accent/50 rounded-lg p-3 flex items-center gap-2">
                    <Check className="w-4 h-4 text-muted-foreground shrink-0" />
                    <div><span className="text-muted-foreground text-xs block">Parking</span><span className="font-medium text-sm">Yes</span></div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Variant Display (Admin) */}
          {hasVariants && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Variants ({variants!.length})</p>
              {variants!.map((v, idx) => (
                <button key={idx} type="button" onClick={() => setSelVariantIdx(idx)}
                  className={`text-left w-full rounded-xl border-2 p-3 transition-all text-sm ${
                    selVariantIdx === idx
                      ? "border-primary bg-primary/5"
                      : "border-border bg-card hover:border-primary/30"
                  }`}>
                  <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
                    <div><span className="text-muted-foreground text-xs">Size</span><p className="font-semibold">{v.size ?? "--"} m<sup>2</sup></p></div>
                    <div><span className="text-muted-foreground text-xs">Price</span><p className="font-semibold">{(v.price ?? 0).toLocaleString()} {v.currency || p.currency}</p></div>
                    {v.bedrooms != null && <div><span className="text-muted-foreground text-xs">Beds</span><p className="font-semibold">{v.bedrooms}</p></div>}
                    {v.bathrooms != null && <div><span className="text-muted-foreground text-xs">Baths</span><p className="font-semibold">{v.bathrooms}</p></div>}
                    {v.view && <div><span className="text-muted-foreground text-xs">View</span><p className="font-semibold">{v.view}</p></div>}
                    {v.finishingType && <div><span className="text-muted-foreground text-xs">Finishing</span><p className="font-semibold">{v.finishingType}</p></div>}
                    {v.deliveryText && <div><span className="text-muted-foreground text-xs">Delivery</span><p className="font-semibold">{v.deliveryText}</p></div>}
                    <div><span className="text-muted-foreground text-xs">Active</span><p className="font-semibold">{(v.isActive ?? true) !== false ? "Yes" : "No"}</p></div>
                  </div>
                  {selVariantIdx === idx && <div className="mt-1 text-xs text-primary font-semibold">Selected</div>}
                </button>
              ))}
            </div>
          )}

          {/* Features */}
          {p.features.length > 0 && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Features</p>
              <div className="flex flex-wrap gap-1.5">
                {p.features.map((f, i) => <Badge key={f + '-' + i} variant="secondary">{f}</Badge>)}
              </div>
            </div>
          )}

          {/* Videos */}
          {p.videos && p.videos.length > 0 && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Videos</p>
              <div className="space-y-2">
                {p.videos.map((v) => {
                  const posterUrl = v.url?.includes('res.cloudinary.com')
                    ? v.url.replace('/upload/', '/upload/so_2.0,q_auto:good,w_320,f_jpg/').replace(/\.\w+$/, '.jpg')
                    : '';
                  return (
                    <div key={v.id} className="flex items-center gap-3 p-2 rounded-lg border border-border bg-accent/30">
                      <div className="relative w-20 h-12 shrink-0 rounded overflow-hidden bg-muted">
                        {posterUrl ? (
                          <img src={posterUrl} alt="" className="w-full h-full object-cover" loading="lazy"
                            onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }} />
                        ) : (
                          <div className="w-full h-full flex items-center justify-center"><Video className="w-5 h-5 text-muted-foreground/40" /></div>
                        )}
                        <div className="absolute inset-0 flex items-center justify-center">
                          <div className="w-5 h-5 rounded-full bg-black/50 flex items-center justify-center"><Play className="w-2.5 h-2.5 text-white" fill="currentColor" /></div>
                        </div>
                      </div>
                      <a href={v.url} target="_blank" rel="noopener noreferrer" className="flex-1 text-sm truncate text-primary hover:underline min-w-0">
                        {v.url.split('/').pop() || 'Video'}
                      </a>
                      <Badge variant="outline" className="text-[10px] shrink-0 gap-1 border-primary/30"><Video className="w-3 h-3" /> #{v.id}</Badge>
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Installments */}
          {enabledInstallments.length > 0 && (
            <div className="space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Installment Plans</p>
              <div className="space-y-2">
                {enabledInstallments.map((inst, i) => (
                  <div key={'inst-' + i} className="border border-border rounded-xl p-3 grid grid-cols-3 gap-2 text-sm">
                    <div><span className="text-muted-foreground text-xs block">Down payment</span><strong>{inst.downPaymentPercent}%</strong></div>
                    <div><span className="text-muted-foreground text-xs block">Years</span><strong>{inst.years}</strong></div>
                    <div><span className="text-muted-foreground text-xs block">Monthly</span><strong>{selVariant && financing && inst.paymentType !== "Cash" ? Math.round(financing.monthlyInstallment).toLocaleString() + " " + p.currency : inst.paymentType === "Cash" && variantPrice > 0 ? variantPrice.toLocaleString() + " " + p.currency + " (Cash)" : "—"}</strong></div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Contact - always shown when we have name+phone */}
          {(displayName || displayPhone) ? (
            <div className="bg-accent/50 rounded-xl p-4 space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Contact</p>
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-primary/20 flex items-center justify-center text-primary">
                  <User className="w-5 h-5" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-sm">{displayName}</p>
                  {contactType && <p className="text-xs text-muted-foreground">{contactType}</p>}
                </div>
                {displayPhone && (
                  <Button variant="outline" size="sm" className="gap-1.5" onClick={() => handleCopyPhone(displayPhone)}>
                    <Copy className="w-3.5 h-3.5" /> {displayPhone}
                  </Button>
                )}
              </div>
            </div>
          ) : contact ? (
            <div className="bg-accent/50 rounded-xl p-4 space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Contact</p>
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-primary/20 flex items-center justify-center text-primary">
                  <User className="w-5 h-5" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-sm">{contact.name}</p>
                  <p className="text-xs text-muted-foreground">{contact.type}</p>
                </div>
                <Button variant="outline" size="sm" className="gap-1.5" onClick={() => handleCopyPhone(contact.phone)}>
                  <Copy className="w-3.5 h-3.5" /> {contact.phone}
                </Button>
              </div>
            </div>
          ) : p.contactId ? (
            <div className="bg-accent/50 rounded-xl p-4 space-y-2">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Contact</p>
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-muted flex items-center justify-center text-muted-foreground">
                  <User className="w-5 h-5" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-sm">ID: {p.contactId}</p>
                  <p className="text-xs text-muted-foreground">(not synced)</p>
                </div>
              </div>
            </div>
          ) : null}

          {/* Timestamps & SEO in accordion-like view */}
          <details className="text-xs text-muted-foreground">
            <summary className="cursor-pointer font-medium">Additional Info</summary>
            <div className="mt-2 space-y-1">
              <p><span className="text-muted-foreground">Created: </span>{new Date(p.createdAt).toLocaleString()}</p>
              {p.seoTitle && <p><span className="text-muted-foreground">SEO Title: </span>{p.seoTitle}</p>}
              {p.seoDescription && <p><span className="text-muted-foreground">SEO Desc: </span>{p.seoDescription}</p>}
              {p.seoKeywords && <p><span className="text-muted-foreground">Keywords: </span>{p.seoKeywords}</p>}
            </div>
          </details>
        </div>
      </DialogContent>
    </Dialog>
  );
}
