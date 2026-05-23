import { Link } from 'react-router-dom';
import { MapPin, ArrowRight } from 'lucide-react';
import { Project } from '../types/property';
import { useLanguage } from '../i18n/LanguageContext';
import { PremiumImage } from './PremiumImage';

export function ProjectCard({ project }: { project: Project }) {
  const { t, language, fmtNum } = useLanguage();
  const name = language === 'ar' ? project.nameAr : project.nameEn;
  const description = language === 'ar' ? project.descriptionAr : project.descriptionEn;
  const imgSrc = project.images?.[0] || project.image || '/placeholder.svg';

  return (
    <Link
      to={`/projects/${project.slug}`}
      className="group relative block rounded-2xl overflow-hidden aspect-[3/4] isolate bg-muted hover:shadow-2xl hover:-translate-y-1 transition-[transform,box-shadow] duration-300 active:scale-[0.98]"
    >
      <div className="absolute inset-0">
        <PremiumImage
          src={imgSrc}
          alt={name}
          width={1200}
          height={1600}
          options={{ quality: 'best', gravity: 'center', sharpen: 'medium' }}
          className="w-full h-full"
          imgClassName="transition-[transform] duration-700 group-hover:scale-110"
          srcsetWidths={[480, 640, 828, 1080, 1200, 1600]}
          sizes="(max-width: 640px) 85vw, (max-width: 1024px) 45vw, 25vw"
        />
      </div>
      <div className="absolute inset-0 bg-gradient-to-t from-navy/95 via-navy/40 to-transparent transition-opacity duration-300 group-hover:opacity-90" />

      <div className="absolute inset-x-0 bottom-0 p-6 sm:p-8 flex flex-col justify-end">
        <div className="flex items-center gap-2 text-white/80 mb-2 text-sm font-medium">
          <MapPin className="w-4 h-4" />
          <span>{language === 'ar' ? (project.locationAr || project.location) : project.location}</span>
        </div>

        <h3 className="font-display text-2xl sm:text-3xl font-bold text-white mb-2 leading-tight">
          {name}
        </h3>

        <p className="text-white/70 line-clamp-2 text-sm mb-6 opacity-0 translate-y-4 transition-all duration-300 group-hover:opacity-100 group-hover:translate-y-0">
          {description}
        </p>

        <div className="flex items-center justify-between">
          <span className="text-gold font-semibold text-sm">
            {fmtNum(project.unitCount ?? project.units?.length ?? 0)} {t('projects.unitsAvailable')}
          </span>
          <div className="w-10 h-10 rounded-full bg-white/10 backdrop-blur-md flex items-center justify-center text-white border border-white/20 transition-all duration-300 group-hover:bg-gold group-hover:border-gold group-hover:-translate-y-0.5 group-hover:shadow-lg">
            <ArrowRight className={`w-5 h-5 ${language === 'ar' ? 'rotate-180' : ''}`} />
          </div>
        </div>
      </div>
    </Link>
  );
}
