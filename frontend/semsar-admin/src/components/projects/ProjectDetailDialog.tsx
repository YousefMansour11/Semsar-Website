import { type Project, type Property, useStore } from "@/store";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { MapPin, Building, ImagePlus, Hash, Languages } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useState } from "react";
import { cn } from "@/lib/utils";

interface Props {
  project: Project | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onUnitClick: (unit: Property) => void;
}

const DESCRIPTION_LIMIT = 200;

function DescriptionText({ text }: { text: string }) {
  const [expanded, setExpanded] = useState(false);
  const needsTruncation = text.length > DESCRIPTION_LIMIT;
  return (
    <>
      <p className="text-sm text-muted-foreground whitespace-pre-wrap break-words">
        {needsTruncation && !expanded ? `${text.slice(0, DESCRIPTION_LIMIT)}...` : text}
      </p>
      {needsTruncation && (
        <button onClick={() => setExpanded(!expanded)} className="text-primary text-xs font-semibold mt-1 hover:underline">
          {expanded ? 'Show less' : 'Read more'}
        </button>
      )}
    </>
  );
}

export function ProjectDetailDialog({ project, open, onOpenChange, onUnitClick }: Props) {
  const properties = useStore(s => s.properties);
  const [lang, setLang] = useState<'en' | 'ar'>('en');

  if (!project) return null;

  const units = properties.filter((p) => p.projectId === project.id);
  const name = lang === 'ar' && project.nameAr ? project.nameAr : project.nameEn || project.name;
  const description = lang === 'ar' && project.descriptionAr ? project.descriptionAr : project.descriptionEn || project.description;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className={cn("bg-card max-w-2xl p-0 max-h-[90vh] overflow-y-auto", lang === 'ar' && 'text-right')}>
        <div className="bg-accent/50 p-6 space-y-3">
          <div className="flex items-start justify-between gap-2">
            <DialogHeader className="p-0">
              <DialogTitle className="text-2xl font-bold">{name}</DialogTitle>
            </DialogHeader>
            {project.nameAr && (
              <Button variant="ghost" size="sm" onClick={() => setLang(lang === 'en' ? 'ar' : 'en')} className="gap-1 text-xs">
                <Languages className="w-3 h-3" /> {lang === 'en' ? 'AR' : 'EN'}
              </Button>
            )}
          </div>
          <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
            <span className="flex items-center gap-1.5"><MapPin className="w-4 h-4" /> {lang === 'ar' && project.locationAr ? project.locationAr : project.location}</span>
            {project.developer && (
              <span className="flex items-center gap-1.5"><Building className="w-4 h-4" /> {project.developer}</span>
            )}
            <Badge variant="secondary">{project.unitCount} units</Badge>
          </div>
          {description && <DescriptionText text={description} />}
          {(lang === 'ar' && project.highlightsAr?.length ? project.highlightsAr : project.highlights).length > 0 && (
            <div className="flex flex-wrap gap-1.5 pt-1">
              {(lang === 'ar' && project.highlightsAr?.length ? project.highlightsAr : project.highlights).map((h, i) => <Badge key={i} variant="outline">{h}</Badge>)}
            </div>
          )}
          {project.propertyTypes?.length > 0 && (
            <div className="pt-2">
              <p className="text-xs text-muted-foreground mb-1">{lang === 'ar' ? 'أنواع العقارات' : 'Property Types'}</p>
              <div className="flex flex-wrap gap-1">
                {project.propertyTypes.map((t, i) => (
                  <Badge key={i} variant="outline">{t}</Badge>
                ))}
              </div>
            </div>
          )}
          {project.ownershipType && (
            <div className="pt-2">
              <Badge variant="secondary">{project.ownershipType === 'GreenContract' ? 'Green Contract' : project.ownershipType === 'Freehold' ? 'Freehold' : project.ownershipType === 'Leasehold' ? 'Leasehold' : project.ownershipType}</Badge>
            </div>
          )}
          {project.startingPrice != null && (
            <div className="pt-2">
              <span className="text-base font-semibold text-emerald-600">From {Number(project.startingPrice).toLocaleString()} EGP</span>
            </div>
          )}
          {project.totalArea != null && (
            <div className="pt-2">
              <span className="text-sm text-muted-foreground">{lang === 'ar' ? 'المساحة الإجمالية' : 'Total Area'}: {Number(project.totalArea).toLocaleString()} m²</span>
            </div>
          )}
          {project.latitude != null && project.longitude != null && (
            <div className="pt-2">
              <span className="text-xs text-muted-foreground">📍 {project.latitude}, {project.longitude}</span>
            </div>
          )}
          {(lang === 'ar' && project.nearbyPlacesAr?.length ? project.nearbyPlacesAr : project.nearbyPlaces)?.length > 0 && (
            <div className="pt-2">
              <p className="text-xs text-muted-foreground mb-1">{lang === 'ar' ? 'الأماكن القريبة' : 'Nearby'}</p>
              <div className="flex flex-wrap gap-1">
                {(lang === 'ar' && project.nearbyPlacesAr?.length ? project.nearbyPlacesAr : project.nearbyPlaces)!.map((p, i) => (
                  <Badge key={i} variant="secondary" className="text-[10px]">{p}</Badge>
                ))}
              </div>
            </div>
          )}
        </div>

        <div className="p-6 space-y-4">
          <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">Available Units</h3>
          {units.length === 0 ? (
            <div className="text-center py-12 border-2 border-dashed border-border rounded-xl">
              <Building className="mx-auto h-8 w-8 text-muted-foreground/30 mb-2" />
              <p className="text-sm text-muted-foreground">No units available</p>
            </div>
          ) : (
            <div className="grid gap-3 sm:grid-cols-2">
              {units.map((u) => (
                <div key={u.id} onClick={() => onUnitClick(u)}
                  className="group bg-background border border-border rounded-xl overflow-hidden cursor-pointer hover:border-primary/30 hover:shadow-md hover:shadow-primary/5 transition-all">
                  <div className="relative h-28 bg-accent overflow-hidden">
                    {u.images[0] ? (
                      <img src={u.images[0]} alt={u.title} loading="lazy" width={200} height={112} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-muted-foreground">
                        <ImagePlus className="w-6 h-6" />
                      </div>
                    )}
                    <div className="absolute top-2 left-2">
                      <Badge variant="outline" className="bg-background/80 backdrop-blur-sm text-[10px] font-mono">
                        <Hash className="w-2.5 h-2.5 mr-1" />{u.code}
                      </Badge>
                    </div>
                  </div>
                  <div className="p-3 space-y-1">
                    <h4 className="font-semibold text-sm truncate">{u.title}</h4>
                    <p className="text-base font-bold text-primary">
                      {u.listingType === 'Rental'
                        ? `${(u.rentPerMonth || u.price).toLocaleString()} ${u.currency}/mo`
                        : `${u.price.toLocaleString()} ${u.currency}`}
                    </p>
                    <Badge variant="outline" className="text-[10px]">{u.propertyType}</Badge>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
