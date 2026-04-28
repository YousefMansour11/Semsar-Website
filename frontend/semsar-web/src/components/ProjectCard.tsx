import { memo } from 'react';
import { Link } from 'react-router-dom';
import { MapPin, ArrowRight } from 'lucide-react';
import { Project } from '../types/property';
import { useLanguage } from '../i18n/LanguageContext';
import { localizedPath } from '../lib/paths';
import { PremiumImage } from './PremiumImage';

export const ProjectCard = memo(function ProjectCard({ project }: { project: Project }) {
  const { t, language, fmtNum } = useLanguage();
  const name = language === 'ar' ? project.nameAr : project.nameEn;
  const description = language === 'ar' ? (project.descriptionAr || project.descriptionEn) : project.descriptionEn;
  const imgSrc = project.images?.[0] || project.image || '/placeholder.svg';

  return (
    <Link
      to={localizedPath(`/projects/${project.slug}`, language)}
      className="group relative block rounded-2xl overflow-hidden aspect-[3/4] isolate bg-muted hover:shadow-2xl hover:-translate-y-1 transition-[transform,box-shadow] duration-300 active:scale-[0.98]"
      style={{ backfaceVisibility: 'hidden' }}
    >
      <div className="absolute inset-0">
        <PremiumImage
          src={imgSrc}
          alt={name}
          width={1200}
          height={1600}
          profile="card"
          className="w-full h-full"
          imgClassName="transition-[transform] duration-700 ease-out group-hover:scale-110"
        />
      </div>
      <div className="absolute inset-0 bg-gradient-to-t from-navy/95 via-navy/30 to-transparent" />
      <div className="absolute inset-0 bg-gradient-to-b from-navy/10 via-transparent to-transparent transition-opacity duration-500 group-hover:opacity-0" />

      <div className="absolute inset-x-0 bottom-0 p-6 sm:p-8 flex flex-col justify-end">
        <div className="flex items-center gap-2 text-white/80 mb-2 text-sm font-medium">
          <MapPin className="w-4 h-4" />
          <span>{language === 'ar' ? (project.locationAr || project.location) : project.location}</span>
        </div>

        <h3 className="font-display text-2xl sm:text-3xl font-bold text-white mb-2 leading-tight">
          {name}
        </h3>

        <p className="text-white/70 line-clamp-2 text-sm mb-6 opacity-0 translate-y-4 transition-[opacity,transform] duration-300 group-hover:opacity-100 group-hover:translate-y-0">
          {description}
        </p>

        <div className="flex items-center justify-between">
          <span className="text-gold font-semibold text-sm">
            {fmtNum(project.unitCount ?? project.units?.length ?? 0)} {t('projects.unitsAvailable')}
          </span>
          {project.startingPrice != null && (
            <span className="text-emerald-400 font-semibold text-xs">
              {t('projects.fromPrice', undefined, { price: fmtNum(project.startingPrice) })}
            </span>
          )}
        </div>
        {project.totalArea != null && (
          <p className="text-white/60 text-xs mt-1">
            {fmtNum(project.totalArea)} m²
          </p>
        )}
        {project.propertyTypes?.length > 0 && (
          <div className="flex flex-wrap gap-1 mt-1">
            {project.propertyTypes.slice(0, 2).map((t, i) => (
              <span key={i} className="text-[10px] px-2 py-0.5 rounded-full bg-white/10 text-white/70 border border-white/20">{t}</span>
            ))}
          </div>
        )}
        <div className="flex items-center justify-between mt-1">
          <span />
          <div className="w-10 h-10 rounded-full bg-white/10 backdrop-blur-md flex items-center justify-center text-white border border-white/20 transition-[background-color,border-color,transform] duration-300 group-hover:bg-gold group-hover:border-gold group-hover:-translate-y-0.5 group-hover:shadow-lg">
            <ArrowRight className={`w-5 h-5 ${language === 'ar' ? 'rotate-180' : ''}`} />
          </div>
        </div>
      </div>
    </Link>
  );
});
