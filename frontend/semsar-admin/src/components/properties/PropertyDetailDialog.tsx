import { type Property } from "@/store";
import { type UnitVariantDto } from "@/lib/api-types";
import {
  Dialog, DialogContent, DialogTitle,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { MapPin, Square, Check, MessageCircle, PhoneCall, Calendar, CreditCard, ImagePlus, ChevronLeft, ChevronRight, Languages, Bed, Bath, ArrowUpDown, Hash, Building, Tag, Eye, Sofa, Wrench, TreeDeciduous, Car, Star, Globe, Heart } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useState } from "react";
import { cn, optimizeCloudinaryUrl } from "@/lib/utils";

interface Props {
  property: Property | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  whatsappNumber?: string;
  phoneNumber?: string;
}

const DESCRIPTION_LIMIT = 200;

function DescriptionText({ text }: { text: string }) {
  const [expanded, setExpanded] = useState(false);
  const needsTruncation = text.length > DESCRIPTION_LIMIT;
  return (
    <>
      <p className="text-muted-foreground text-base sm:text-lg leading-relaxed whitespace-pre-wrap break-words">
        {needsTruncation && !expanded ? `${text.slice(0, DESCRIPTION_LIMIT)}...` : text}
      </p>
      {needsTruncation && (
        <button onClick={() => setExpanded(!expanded)} className="text-primary text-sm font-semibold mt-1 hover:underline">
          {expanded ? 'Show less' : 'Read more'}
        </button>
      )}
    </>
  );
}

export function PropertyDetailDialog({ property: p, open, onOpenChange, whatsappNumber, phoneNumber }: Props) {
  const [imgIdx, setImgIdx] = useState(0);
  const [lang, setLang] = useState<'en' | 'ar'>('en');
  const [selVariantIdx, setSelVariantIdx] = useState(0);
  const [selPlanIdx, setSelPlanIdx] = useState(0);

  if (!p) return null;

  const variants = (p as unknown as { variants?: UnitVariantDto[] }).variants;
  const hasVariants = variants && variants.length > 0;
  const selVariant = hasVariants ? variants![Math.min(selVariantIdx, variants!.length - 1)] : null;
  const gallery = p.images.length > 0 ? p.images : [];
  const title = lang === 'ar' && p.titleAr ? p.titleAr : p.titleEn || p.title;
  const description = lang === 'ar' && p.descriptionAr ? p.descriptionAr : p.descriptionEn || p.description;
  const enabledInstallments = p.installments?.filter(i => i.isEnabled) ?? [];

  const variantPrice = selVariant?.price ?? 0;
  const basePrice = hasVariants ? variantPrice : (p.price || p.minPrice || 0);
  const displayPrice = p.listingType === 'Rental'
    ? `${(p.rentPerMonth || p.price || 0).toLocaleString()} ${p.currency}/mo`
    : `${(basePrice).toLocaleString()} ${p.currency}`;

  const selPlan = selVariant && enabledInstallments.length > 0
    ? enabledInstallments[Math.min(selPlanIdx, enabledInstallments.length - 1)]
    : null;
  const financing = selVariant && selPlan && selPlan.paymentType !== 'Cash'
    ? {
        downPaymentAmount: variantPrice * selPlan.downPaymentPercent / 100,
        remainingAmount: variantPrice * (1 - selPlan.downPaymentPercent / 100),
        monthlyInstallment: selPlan.years > 0
          ? (variantPrice * (1 - selPlan.downPaymentPercent / 100)) / (selPlan.years * 12)
          : 0,
      }
    : null;

  const prev = () => setImgIdx((i) => (i > 0 ? i - 1 : gallery.length - 1));
  const next = () => setImgIdx((i) => (i < gallery.length - 1 ? i + 1 : 0));

  const listingBadgeColor = p.listingType === 'Rental'
    ? 'bg-emerald-500/90 text-white'
    : p.listingType === 'Project'
      ? 'bg-gold/90 text-white'
      : 'bg-amber-500/90 text-white';

  return (
    <Dialog open={open} onOpenChange={(o) => { onOpenChange(o); setImgIdx(0); }}>
      <DialogContent className={cn("bg-card max-w-4xl p-0 max-h-[90vh] overflow-y-auto", lang === 'ar' && 'text-right')}>
        <DialogTitle className="sr-only">{title}</DialogTitle>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-0">
          {/* Left: Main Content */}
          <div className="lg:col-span-2 space-y-6 p-6">
            {/* Gallery */}
            <div className="rounded-2xl overflow-hidden border border-border relative group">
              <div className="aspect-[16/10] bg-muted relative overflow-hidden">
                {gallery.length > 0 ? (
                  <>
                    <img src={optimizeCloudinaryUrl(gallery[imgIdx], 800)} alt={title} loading="lazy" width={800} height={500} className="w-full h-full object-cover transition-all duration-500" />
                    <div className="absolute top-4 left-4 flex gap-2">
                      <span className={`px-4 py-1.5 rounded-lg text-sm font-bold backdrop-blur-md shadow-sm ${listingBadgeColor}`}>
                        {p.listingType}
                      </span>
                      {p.isRecommended && (
                        <span className="px-4 py-1.5 rounded-lg text-sm font-bold backdrop-blur-md shadow-sm bg-emerald-500/90 text-white flex items-center gap-1">
                          <Star className="w-3.5 h-3.5" /> Recommended
                        </span>
                      )}
                    </div>
                    <div className="absolute bottom-4 right-4 px-3 py-1.5 rounded-lg bg-black/50 backdrop-blur-sm text-white text-xs font-medium">
                      {imgIdx + 1} / {gallery.length}
                    </div>
                    {gallery.length > 1 && (
                      <>
                        <button onClick={prev} aria-label="Previous image"
                          className="absolute left-3 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-background/80 backdrop-blur-sm shadow-lg flex items-center justify-center hover:bg-background transition-all opacity-0 group-hover:opacity-100">
                          <ChevronLeft className="w-5 h-5" />
                        </button>
                        <button onClick={next} aria-label="Next image"
                          className="absolute right-3 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-background/80 backdrop-blur-sm shadow-lg flex items-center justify-center hover:bg-background transition-all opacity-0 group-hover:opacity-100">
                          <ChevronRight className="w-5 h-5" />
                        </button>
                      </>
                    )}
                  </>
                ) : (
                  <div className="w-full h-full flex items-center justify-center text-muted-foreground">
                    <ImagePlus className="w-12 h-12" />
                  </div>
                )}
              </div>
              {gallery.length > 1 && (
                <div className="flex gap-2 p-2 overflow-x-auto bg-card">
                  {gallery.map((img, i) => (
                    <button key={i} onClick={() => setImgIdx(i)} aria-label={`View image ${i + 1}`} className={`shrink-0 w-20 h-16 rounded-lg overflow-hidden border-2 transition-all ${i === imgIdx ? 'border-secondary' : 'border-transparent opacity-70 hover:opacity-100'}`}>
                      <img src={optimizeCloudinaryUrl(img, 160)} alt="" loading="lazy" width={80} height={60} className="w-full h-full object-cover" />
                    </button>
                  ))}
                </div>
              )}
            </div>

            {/* Title & Price */}
            <div>
              <div className="flex flex-col md:flex-row md:items-start justify-between gap-4 mb-6">
                <div>
                  <h1 className="font-display text-2xl sm:text-3xl font-bold text-foreground mb-2">{title}</h1>
                  <div className="flex items-center gap-2 text-muted-foreground text-sm">
                    <MapPin className="w-5 h-5 text-gold" />
                    <span>{lang === 'ar' && p.locationAr ? p.locationAr : p.location}</span>
                  </div>
                </div>
                <div className="text-start md:text-end shrink-0">
                  {p.titleAr && (
                    <Button variant="ghost" size="sm" onClick={() => setLang(lang === 'en' ? 'ar' : 'en')} className="gap-1 text-xs mb-2">
                      <Languages className="w-3 h-3" /> {lang === 'en' ? 'AR' : 'EN'}
                    </Button>
                  )}
                  <div className="text-2xl sm:text-3xl font-bold text-foreground">{displayPrice}</div>
                </div>
              </div>

              {/* Specs */}
              <div className="grid grid-cols-[repeat(auto-fill,minmax(160px,1fr))] gap-4 sm:gap-6 py-4 sm:py-6 border-y border-border">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-secondary/10 flex items-center justify-center text-secondary">
                    <Tag className="w-5 h-5" />
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground uppercase tracking-wide">Type</div>
                    <div className="font-bold text-sm sm:text-base">{p.propertyType}</div>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-gold/10 flex items-center justify-center text-gold">
                    <Square className="w-5 h-5" />
                  </div>
                  <div>
                    <div className="text-xs text-muted-foreground uppercase tracking-wide">Size</div>
                    <div className="font-bold text-sm sm:text-base">{p.size} m²</div>
                  </div>
                </div>
                {p.bedrooms != null && p.bedrooms > 0 && (
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-blue-500/10 flex items-center justify-center text-blue-600">
                      <Bed className="w-5 h-5" />
                    </div>
                    <div>
                      <div className="text-xs text-muted-foreground uppercase tracking-wide">Bedrooms</div>
                      <div className="font-bold text-sm sm:text-base">{p.bedrooms}</div>
                    </div>
                  </div>
                )}
                {p.bathrooms != null && p.bathrooms > 0 && (
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-cyan-500/10 flex items-center justify-center text-cyan-600">
                      <Bath className="w-5 h-5" />
                    </div>
                    <div>
                      <div className="text-xs text-muted-foreground uppercase tracking-wide">Bathrooms</div>
                      <div className="font-bold text-sm sm:text-base">{p.bathrooms}</div>
                    </div>
                  </div>
                )}
                {p.floor != null && (
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-purple-500/10 flex items-center justify-center text-purple-600">
                      <ArrowUpDown className="w-5 h-5" />
                    </div>
                    <div>
                      <div className="text-xs text-muted-foreground uppercase tracking-wide">Floor</div>
                      <div className="font-bold text-sm sm:text-base">{p.floor}{p.totalFloors != null ? `/${p.totalFloors}` : ''}</div>
                    </div>
                  </div>
                )}
                {p.view && p.view !== 'Unknown' && (
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-amber-500/10 flex items-center justify-center text-amber-600">
                      <Eye className="w-5 h-5" />
                    </div>
                    <div>
                      <div className="text-xs text-muted-foreground uppercase tracking-wide">View</div>
                      <div className="font-bold text-sm sm:text-base">{p.view}</div>
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Variant Selector */}
            {hasVariants && (
              <div>
                <h3 className="font-display text-lg font-bold mb-3">Available Variants</h3>
                <div className="grid grid-cols-1 gap-3">
                  {variants!.map((v, idx) => (
                    <button key={idx} type="button"
                      onClick={() => { setSelVariantIdx(idx); setSelPlanIdx(0); }}
                      className={`text-left w-full rounded-xl border-2 p-4 transition-all ${
                        selVariantIdx === idx
                          ? 'border-gold bg-gold/5 shadow-md'
                          : 'border-border bg-card hover:border-gold/50'
                      }`}
                    >
                      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-sm">
                        <div><span className="text-muted-foreground text-xs">Size</span><p className="font-semibold">{v.size} m²</p></div>
                        <div><span className="text-muted-foreground text-xs">Price</span><p className="font-semibold">{(v.price ?? 0).toLocaleString()} {v.currency || p.currency}</p></div>
                        {v.bedrooms != null && <div><span className="text-muted-foreground text-xs">Bedrooms</span><p className="font-semibold">{v.bedrooms}</p></div>}
                        {v.bathrooms != null && <div><span className="text-muted-foreground text-xs">Bathrooms</span><p className="font-semibold">{v.bathrooms}</p></div>}
                        {v.view && <div><span className="text-muted-foreground text-xs">View</span><p className="font-semibold">{v.view}</p></div>}
                        {v.finishingType && <div><span className="text-muted-foreground text-xs">Finishing</span><p className="font-semibold">{v.finishingType}</p></div>}
                        {v.deliveryText && <div><span className="text-muted-foreground text-xs">Delivery</span><p className="font-semibold">{v.deliveryText}</p></div>}
                        <div><span className="text-muted-foreground text-xs">Availability</span><p className="font-semibold">{(v.isActive ?? true) !== false ? 'Available' : 'Sold'}</p></div>
                      </div>
                      {selVariantIdx === idx && <div className="mt-2 text-xs text-gold font-semibold">Selected</div>}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* Additional Details */}
            {(p.isFurnished || p.unitNumber || p.buildingNumber || p.finishingType || p.deliveryDate || p.hasBalcony || p.hasParking || p.deliveryText || p.constructionStatus || p.availabilityStatus || p.ownershipType) && (
              <div>
                <h3 className="font-display text-lg font-bold mb-3">Additional Details</h3>
                <div className="grid grid-cols-[repeat(auto-fill,minmax(160px,1fr))] gap-4 sm:gap-6 py-4 sm:py-6 border-y border-border">
                  {p.isFurnished && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-emerald-500/10 flex items-center justify-center text-emerald-600">
                        <Sofa className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Furnished</div>
                        <div className="font-bold text-sm sm:text-base">Yes</div>
                      </div>
                    </div>
                  )}
                  {p.unitNumber && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-orange-500/10 flex items-center justify-center text-orange-600">
                        <Hash className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Unit #</div>
                        <div className="font-bold text-sm sm:text-base">{p.unitNumber}</div>
                      </div>
                    </div>
                  )}
                  {p.buildingNumber && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-indigo-500/10 flex items-center justify-center text-indigo-600">
                        <Building className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Building #</div>
                        <div className="font-bold text-sm sm:text-base">{p.buildingNumber}</div>
                      </div>
                    </div>
                  )}
                  {p.finishingType && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-pink-500/10 flex items-center justify-center text-pink-600">
                        <Wrench className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Finishing</div>
                        <div className="font-bold text-sm sm:text-base">{p.finishingType}</div>
                      </div>
                    </div>
                  )}
                  {p.deliveryDate && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-rose-500/10 flex items-center justify-center text-rose-600">
                        <Calendar className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Delivery</div>
                        <div className="font-bold text-sm sm:text-base">{new Date(p.deliveryDate).toLocaleDateString()}</div>
                      </div>
                    </div>
                  )}
                  {p.hasBalcony && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-teal-500/10 flex items-center justify-center text-teal-600">
                        <TreeDeciduous className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Balcony</div>
                        <div className="font-bold text-sm sm:text-base">Yes</div>
                      </div>
                    </div>
                  )}
                  {p.hasParking && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-slate-500/10 flex items-center justify-center text-slate-600">
                        <Car className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Parking</div>
                        <div className="font-bold text-sm sm:text-base">Yes</div>
                      </div>
                    </div>
                  )}
                  {p.deliveryText && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-amber-500/10 flex items-center justify-center text-amber-600">
                        <Calendar className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Delivery</div>
                        <div className="font-bold text-sm sm:text-base">{p.deliveryText}</div>
                      </div>
                    </div>
                  )}
                  {p.constructionStatus && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-blue-500/10 flex items-center justify-center text-blue-600">
                        <Building className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Construction</div>
                        <div className="font-bold text-sm sm:text-base">{p.constructionStatus}</div>
                      </div>
                    </div>
                  )}
                  {p.availabilityStatus && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-teal-500/10 flex items-center justify-center text-teal-600">
                        <Check className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Availability</div>
                        <div className="font-bold text-sm sm:text-base">{p.availabilityStatus}</div>
                      </div>
                    </div>
                  )}
                  {p.ownershipType && (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-xl bg-purple-500/10 flex items-center justify-center text-purple-600">
                        <Tag className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground uppercase tracking-wide">Ownership</div>
                        <div className="font-bold text-sm sm:text-base">{p.ownershipType}</div>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Description */}
            {description && (
              <div>
                <h3 className="font-display text-xl sm:text-2xl font-bold mb-2">Description</h3>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <DescriptionText text={description} />
              </div>
            )}

            {/* Installment Plans */}
            {enabledInstallments.length > 0 && (
              <div>
                <h3 className="font-display text-xl sm:text-2xl font-bold mb-2">Installment Plans</h3>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className="space-y-4">
                  {enabledInstallments.map((inst, idx) => (
                    <div key={idx} className="bg-gradient-to-r from-gold/5 to-gold/10 border border-gold/20 rounded-2xl p-4 sm:p-6">
                      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 sm:gap-6">
                        <div className="text-center p-3 sm:p-4 bg-card rounded-xl border border-gold/20">
                          <CreditCard className="w-6 h-6 text-gold mx-auto mb-2" />
                          <div className="text-sm text-muted-foreground mb-1">Down Payment</div>
                          <div className="text-xl sm:text-2xl font-bold text-foreground">{inst.downPaymentPercent}%</div>
                          <div className="text-sm text-muted-foreground">{(basePrice * inst.downPaymentPercent / 100).toLocaleString()} {p.currency}</div>
                        </div>
                        <div className="text-center p-3 sm:p-4 bg-card rounded-xl border border-gold/20">
                          <Calendar className="w-6 h-6 text-gold mx-auto mb-2" />
                          <div className="text-sm text-muted-foreground mb-1">Years</div>
                          <div className="text-xl sm:text-2xl font-bold text-foreground">{inst.years}</div>
                          <div className="text-sm text-muted-foreground">years</div>
                        </div>
                        {hasVariants && selVariant && financing && inst.paymentType !== "Cash" ? (
                            <>
                              <div className="text-center p-3 sm:p-4 bg-card rounded-xl border border-gold/20">
                                <div className="w-6 h-6 text-gold mx-auto mb-2 font-bold text-lg">$</div>
                                <div className="text-sm text-muted-foreground mb-1">Remaining</div>
                                <div className="text-xl sm:text-2xl font-bold text-foreground">{Math.round(financing.remainingAmount).toLocaleString()} {p.currency}</div>
                              </div>
                              <div className="text-center p-3 sm:p-4 bg-card rounded-xl border border-gold/20">
                                <div className="w-6 h-6 text-gold mx-auto mb-2 font-bold text-lg">$</div>
                                <div className="text-sm text-muted-foreground mb-1">Monthly</div>
                                <div className="text-xl sm:text-2xl font-bold text-foreground">{Math.round(financing.monthlyInstallment).toLocaleString()} {p.currency}</div>
                                <div className="text-sm text-muted-foreground">/ mo</div>
                              </div>
                            </>
                          ) : inst.paymentType === "Cash" && variantPrice > 0 ? (
                            <div className="text-center p-3 sm:p-4 bg-card rounded-xl border border-gold/20 sm:col-span-2">
                              <div className="w-6 h-6 text-gold mx-auto mb-2 font-bold text-lg">$</div>
                              <div className="text-sm text-muted-foreground mb-1">Cash Price</div>
                              <div className="text-xl sm:text-2xl font-bold text-foreground">{variantPrice.toLocaleString()} {p.currency}</div>
                            </div>
                          ) : null}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Features */}
            {(lang === 'ar' && p.featuresAr?.length ? p.featuresAr : p.features).length > 0 && (
              <div>
                <h3 className="font-display text-xl sm:text-2xl font-bold mb-2">{lang === 'ar' ? 'المميزات' : 'Features'}</h3>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 sm:gap-3">
                  {(lang === 'ar' && p.featuresAr?.length ? p.featuresAr : p.features).map((feature, idx) => (
                    <div key={idx} className="flex items-center gap-3 bg-muted/30 px-3 sm:px-4 py-2.5 sm:py-3 rounded-xl border border-border">
                      <div className="w-6 h-6 rounded-full bg-emerald-500/15 text-emerald-600 flex items-center justify-center shrink-0">
                        <Check className="w-3.5 h-3.5" />
                      </div>
                      <span className="font-medium text-foreground text-sm">{feature}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Engagement Stats */}
            {(p.viewCount != null || p.inquiryCount != null || p.favoriteCount != null) && (
              <div>
                <h3 className="font-display text-xl sm:text-2xl font-bold mb-2">Engagement</h3>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                  {p.viewCount != null && (
                    <div className="flex items-center gap-3 bg-muted/30 px-4 py-3 rounded-xl border border-border">
                      <Eye className="w-5 h-5 text-muted-foreground" />
                      <div>
                        <div className="text-xs text-muted-foreground">Views</div>
                        <div className="font-bold text-lg">{p.viewCount}</div>
                      </div>
                    </div>
                  )}
                  {p.inquiryCount != null && (
                    <div className="flex items-center gap-3 bg-muted/30 px-4 py-3 rounded-xl border border-border">
                      <MessageCircle className="w-5 h-5 text-muted-foreground" />
                      <div>
                        <div className="text-xs text-muted-foreground">Inquiries</div>
                        <div className="font-bold text-lg">{p.inquiryCount}</div>
                      </div>
                    </div>
                  )}
                  {p.favoriteCount != null && (
                    <div className="flex items-center gap-3 bg-muted/30 px-4 py-3 rounded-xl border border-border">
                      <Heart className="w-5 h-5 text-muted-foreground" />
                      <div>
                        <div className="text-xs text-muted-foreground">Favorites</div>
                        <div className="font-bold text-lg">{p.favoriteCount}</div>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Virtual Tour */}
            {p.virtualTourUrl && (
              <div>
                <h3 className="font-display text-xl sm:text-2xl font-bold mb-2">Virtual Tour</h3>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <a href={p.virtualTourUrl} target="_blank" rel="noopener noreferrer"
                  className="flex items-center gap-2 text-primary hover:underline font-medium">
                  <Globe className="w-5 h-5" /> View Virtual Tour
                </a>
              </div>
            )}

            {/* Arabic Highlights */}
            {p.highlightsAr && p.highlightsAr.length > 0 && (
              <div>
                <h3 className="font-display text-xl sm:text-2xl font-bold mb-2">Highlights (AR)</h3>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2" dir="rtl">
                  {p.highlightsAr.map((h, idx) => (
                    <div key={idx} className="flex items-center gap-3 bg-muted/30 px-3 sm:px-4 py-2.5 sm:py-3 rounded-xl border border-border">
                      <div className="w-6 h-6 rounded-full bg-gold/15 text-gold flex items-center justify-center shrink-0">
                        <Star className="w-3.5 h-3.5" />
                      </div>
                      <span className="font-medium text-foreground text-sm">{h}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Nearby Places */}
            {p.nearbyPlaces && p.nearbyPlaces.length > 0 && (
              <div>
                <h3 className="font-display text-xl sm:text-2xl font-bold mb-2">Nearby Places</h3>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 sm:gap-3">
                  {p.nearbyPlaces.map((n, idx) => (
                    <div key={idx} className="flex items-center gap-3 bg-muted/30 px-3 sm:px-4 py-2.5 sm:py-3 rounded-xl border border-border">
                      <div className="w-6 h-6 rounded-full bg-gold/15 text-gold flex items-center justify-center shrink-0">
                        <MapPin className="w-3.5 h-3.5" />
                      </div>
                      <span className="font-medium text-foreground text-sm">{n}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Nearby Places AR */}
            {p.nearbyPlacesAr && p.nearbyPlacesAr.length > 0 && (
              <div>
                <h3 className="font-display text-xl sm:text-2xl font-bold mb-2">Nearby Places (AR)</h3>
                <div className="w-10 h-1 bg-gold rounded-full mb-5" />
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 sm:gap-3" dir="rtl">
                  {p.nearbyPlacesAr.map((n, idx) => (
                    <div key={idx} className="flex items-center gap-3 bg-muted/30 px-3 sm:px-4 py-2.5 sm:py-3 rounded-xl border border-border">
                      <div className="w-6 h-6 rounded-full bg-gold/15 text-gold flex items-center justify-center shrink-0">
                        <MapPin className="w-3.5 h-3.5" />
                      </div>
                      <span className="font-medium text-foreground text-sm">{n}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {p.slug && (
              <p className="text-[11px] text-muted-foreground font-mono break-all">/{p.slug}</p>
            )}
          </div>

          {/* Right: Sidebar */}
          <div className="lg:col-span-1 border-l border-border p-6 bg-muted/30">
            <div className="bg-card border border-border shadow-2xl rounded-2xl overflow-hidden sticky top-6">
              <div className="bg-navy px-5 sm:px-7 py-5 sm:py-6">
                <h3 className="font-display text-lg sm:text-xl font-bold text-white mb-1">Interested?</h3>
              </div>
              <div className="p-4 sm:p-6 space-y-3">
                {whatsappNumber && (
                  <a href={`https://wa.me/${whatsappNumber.replace(/\D/g, '')}?text=${encodeURIComponent(`Hello, I'm interested in (${title})`)}`}
                    target="_blank" rel="noopener noreferrer"
                    className="flex items-center justify-center gap-2 w-full py-3 sm:py-3.5 bg-green-500 text-white rounded-xl font-semibold shadow-lg shadow-green-500/25 hover:bg-green-600 transition-all text-sm">
                    <MessageCircle className="w-5 h-5" /> WhatsApp
                  </a>
                )}
                {phoneNumber && (
                  <a href={`tel:${phoneNumber}`}
                    className="flex items-center justify-center gap-2 w-full py-3 sm:py-3.5 bg-secondary text-white rounded-xl font-semibold shadow-lg shadow-secondary/25 hover:bg-secondary/90 transition-all text-sm">
                    <PhoneCall className="w-5 h-5" /> Call Now
                  </a>
                )}
              </div>
            </div>

            <div className="mt-4 flex flex-wrap gap-1.5">
              <Badge variant="outline" className="text-xs">{p.propertyType}</Badge>
              {p.size > 0 && (
                <Badge variant="outline" className="text-xs">{p.size} m²</Badge>
              )}
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
