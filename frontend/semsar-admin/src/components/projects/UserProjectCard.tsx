import { memo } from "react";
import { type Project } from "@/store";
import { Badge } from "@/components/ui/badge";
import { MapPin, Building, ImagePlus } from "lucide-react";
import { optimizeCloudinaryUrl } from "@/lib/utils";

interface Props {
  project: Project;
  onClick?: () => void;
  lang?: 'en' | 'ar';
}

export const UserProjectCard = memo(function UserProjectCard({ project, onClick, lang }: Props) {
  const unitCount = project.unitCount;

  return (
    <div
      className="group relative block rounded-2xl overflow-hidden aspect-[3/4] isolate cursor-pointer bg-card"
      onClick={onClick}
    >
      {project.image ? (
        <img src={optimizeCloudinaryUrl(project.image, 400)} alt={project.name} loading="lazy" width={400} height={533}
          className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-110" />
      ) : (
        <div className="absolute inset-0 flex items-center justify-center bg-accent">
          <ImagePlus className="w-10 h-10 text-muted-foreground" />
        </div>
      )}
      <div className="absolute inset-0 bg-gradient-to-t from-navy/95 via-navy/40 to-transparent transition-opacity duration-300 group-hover:opacity-90" />

      <div className="absolute inset-x-0 bottom-0 p-6 flex flex-col justify-end">
        <div className="flex items-center gap-2 text-white/80 mb-2 text-sm font-medium">
          <MapPin className="w-4 h-4" />
          <span>{lang === 'ar' && project.locationAr ? project.locationAr : project.location}</span>
        </div>

        <h3 className="font-display text-2xl font-bold text-white mb-2 leading-tight">
          {project.name}
        </h3>

        {project.developer && (
          <p className="text-white/70 text-sm mb-1 flex items-center gap-1.5">
            <Building className="w-3.5 h-3.5" />
            {project.developer}
          </p>
        )}

        <p className="text-white/50 line-clamp-2 text-sm mb-4 opacity-0 translate-y-4 transition-all duration-300 group-hover:opacity-100 group-hover:translate-y-0">
          {project.description}
        </p>

        <div className="flex items-center justify-between">
          <span className="text-gold font-semibold text-sm">
            {unitCount} unit{unitCount !== 1 ? 's' : ''}
          </span>
          {project.startingPrice != null && (
            <span className="text-emerald-400 font-semibold text-xs">
              From {Number(project.startingPrice).toLocaleString()} EGP
            </span>
          )}
        </div>
        {project.totalArea != null && (
          <p className="text-white/60 text-xs mt-1">
            {Number(project.totalArea).toLocaleString()} m²
          </p>
        )}
        {project.ownershipType && (
          <p className="text-white/60 text-[10px] mt-0.5">
            {project.ownershipType === 'GreenContract' ? 'Green Contract' : project.ownershipType === 'Freehold' ? 'Freehold' : project.ownershipType === 'Leasehold' ? 'Leasehold' : project.ownershipType}
          </p>
        )}
        {project.propertyTypes?.length > 0 && (
          <div className="flex flex-wrap gap-1 mt-1">
            {project.propertyTypes.slice(0, 2).map((t, i) => (
              <Badge key={i} variant="outline" className="text-[9px] border-white/20 text-white/70">{t}</Badge>
            ))}
          </div>
        )}
        <div className="flex items-center justify-between mt-1">
          <span />
          {project.highlights.length > 0 && (
            <div className="flex gap-1">
              {(lang === 'ar' && project.highlightsAr?.length ? project.highlightsAr : project.highlights).slice(0, 2).map((h, i) => (
                <Badge key={h + '-' + i} variant="outline" className="text-[9px] border-white/20 text-white/70">{h}</Badge>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
});
