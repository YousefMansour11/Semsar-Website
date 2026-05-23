import { useCallback, useEffect, useState } from 'react';
import useEmblaCarousel from 'embla-carousel-react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { ProjectCard } from './ProjectCard';
import { Project } from '../types/property';
import { useLanguage } from '../i18n/LanguageContext';

export function ProjectsSlider({ projects }: { projects: Project[] }) {
  const { language, t } = useLanguage();
  const [emblaRef, emblaApi] = useEmblaCarousel({
    align: 'start',
    containScroll: 'trimSnaps',
    direction: language === 'ar' ? 'rtl' : 'ltr'
  });
  const [prevEnabled, setPrevEnabled] = useState(false);
  const [nextEnabled, setNextEnabled] = useState(true);

  const scrollPrev = useCallback(() => emblaApi?.scrollPrev(), [emblaApi]);
  const scrollNext = useCallback(() => emblaApi?.scrollNext(), [emblaApi]);

  const onSelect = useCallback(() => {
    if (!emblaApi) return;
    setPrevEnabled(emblaApi.canScrollPrev());
    setNextEnabled(emblaApi.canScrollNext());
  }, [emblaApi]);

  useEffect(() => {
    if (!emblaApi) return;
    onSelect();
    emblaApi.on('select', onSelect);
    emblaApi.on('reInit', onSelect);
    return () => {
      emblaApi.off('select', onSelect);
      emblaApi.off('reInit', onSelect);
    };
  }, [emblaApi, onSelect]);

  return (
    <div className="relative">
      <div className="overflow-hidden" ref={emblaRef}>
        <div className="flex gap-4 sm:gap-6 ml-4 sm:ml-6 lg:ml-8 mr-4 sm:mr-6 lg:mr-8 py-8">
          {projects.map((project) => (
            <div key={project.id} className="flex-[0_0_75%] sm:flex-[0_0_40%] lg:flex-[0_0_22%] min-w-0">
              <ProjectCard project={project} />
            </div>
          ))}
        </div>
      </div>

      <div className="absolute top-1/2 -translate-y-1/2 left-0 right-0 flex justify-between px-2 sm:px-4 pointer-events-none">
        <button
          onClick={scrollPrev}
          disabled={!prevEnabled}
          aria-label={t('projects.slider.prev')}
          className={`w-11 h-11 rounded-full bg-white/90 backdrop-blur shadow-lg flex items-center justify-center text-foreground disabled:opacity-0 transition-all duration-200 pointer-events-auto hover:bg-secondary hover:text-white hover:shadow-xl active:scale-[0.95] ${language === 'ar' ? 'order-2' : ''}`}
        >
          {language === 'ar' ? <ChevronRight className="w-6 h-6" /> : <ChevronLeft className="w-6 h-6" />}
        </button>
        <button
          onClick={scrollNext}
          disabled={!nextEnabled}
          aria-label={t('projects.slider.next')}
          className={`w-11 h-11 rounded-full bg-white/90 backdrop-blur shadow-lg flex items-center justify-center text-foreground disabled:opacity-0 transition-all duration-200 pointer-events-auto hover:bg-secondary hover:text-white hover:shadow-xl active:scale-[0.95] ${language === 'ar' ? 'order-1' : ''}`}
        >
          {language === 'ar' ? <ChevronLeft className="w-6 h-6" /> : <ChevronRight className="w-6 h-6" />}
        </button>
      </div>
    </div>
  );
}
