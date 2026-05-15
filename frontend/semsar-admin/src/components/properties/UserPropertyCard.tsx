import { memo } from "react";
import { type Property } from "@/store";
import { MapPin, Square, Bed, ImagePlus } from "lucide-react";
import { optimizeCloudinaryUrl } from "@/lib/utils";

interface Props {
  property: Property;
  onClick?: () => void;
  lang?: 'en' | 'ar';
}

export const UserPropertyCard = memo(function UserPropertyCard({ property: p, onClick, lang }: Props) {
  const isAr = lang === 'ar';
  const displayPrice = p.listingType === 'Rental'
    ? `${(p.rentPerMonth || p.price).toLocaleString()} ${p.currency}/mo`
    : `${p.price.toLocaleString()} ${p.currency}`;

  const title = isAr && p.titleAr ? p.titleAr : (p.titleEn || p.title);

  const enabledInstallment = p.installments?.find(i => i.isEnabled);

  const listingBadgeColor = p.listingType === 'Rental'
    ? 'bg-emerald-500/90 text-white'
    : p.listingType === 'Project'
      ? 'bg-gold/90 text-white'
      : 'bg-amber-500/90 text-white';

  return (
    <div
      className="group block bg-card rounded-2xl overflow-hidden border border-border hover:shadow-xl hover:-translate-y-1 transition-all duration-300 cursor-pointer"
      onClick={onClick}
    >
      <div className="relative aspect-[4/3] overflow-hidden">
        {p.images[0] ? (
          <img
            src={optimizeCloudinaryUrl(p.images[0], 400)}
            alt={title}
            loading="lazy"
            className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-muted-foreground bg-accent">
            <ImagePlus className="w-10 h-10" />
          </div>
        )}
        <div className="absolute top-3 left-3 flex flex-col gap-2">
          {p.installments?.some(i => i.isEnabled) && (
            <span className="flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] font-semibold bg-gold/90 text-white backdrop-blur-md">
              Installment
            </span>
          )}
        </div>
        <div className="absolute top-3 right-3">
          <span className={`px-2.5 py-1 rounded-full text-[10px] font-semibold backdrop-blur-md shadow-sm ${listingBadgeColor}`}>
            {p.listingType}
          </span>
        </div>
        <div className="absolute bottom-0 left-0 right-0 p-4 bg-gradient-to-t from-black/80 to-transparent">
          <p className="text-white font-bold text-lg">{displayPrice}</p>
          {enabledInstallment && (
            <p className="text-gold text-[10px] font-semibold mt-0.5">
              {enabledInstallment.downPaymentPercent}% Down &middot; {enabledInstallment.years} years
            </p>
          )}
        </div>
      </div>

      <div className="p-5">
        <div className="flex items-center gap-2 text-muted-foreground mb-2 text-sm">
          <MapPin aria-hidden="true" className="w-3.5 h-3.5 shrink-0" />
          <span className="truncate">{isAr && p.locationAr ? p.locationAr : p.location}</span>
          <span className="text-border">·</span>
          <span className="truncate">{p.propertyType}</span>
        </div>
        <h3 className="font-display font-bold text-base mb-4 text-foreground line-clamp-1 group-hover:text-secondary transition-colors">
          {title}
        </h3>

        <div className="flex items-center justify-between pt-4 border-t border-border/50 text-sm text-muted-foreground mb-4">
          <div className="flex items-center gap-1.5">
            <Bed aria-hidden="true" className="w-4 h-4" />
            <span>{p.bedrooms != null && p.bedrooms > 0 ? p.bedrooms : '—'}</span>
          </div>
          <div className="flex items-center gap-1.5">
            <Square aria-hidden="true" className="w-4 h-4" />
            <span>{p.size} m²</span>
          </div>
        </div>

        <span className="block w-full text-center py-2.5 bg-navy text-white rounded-xl text-sm font-semibold group-hover:bg-navy-light transition-colors">
          View Details
        </span>
      </div>
    </div>
  );
});
