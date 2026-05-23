import { useLanguage } from '../i18n/LanguageContext';

function Pulse({ className = '' }: { className?: string }) {
  return <div className={`bg-muted/60 animate-pulse rounded-xl ${className}`} />;
}

export function PropertyCardSkeleton() {
  return (
    <div className="bg-card rounded-2xl overflow-hidden border border-border">
      <div className="aspect-[4/3]">
        <Pulse className="w-full h-full rounded-none" />
      </div>
      <div className="p-4 sm:p-5 space-y-3">
        <div className="flex gap-2">
          <Pulse className="h-3 w-24" />
          <Pulse className="h-3 w-16" />
        </div>
        <Pulse className="h-5 w-3/4" />
        <div className="flex justify-between pt-3 border-t border-border/50">
          <Pulse className="h-4 w-20" />
          <Pulse className="h-4 w-16" />
        </div>
        <Pulse className="h-12 w-full rounded-xl" />
      </div>
    </div>
  );
}

export function ProjectCardSkeleton() {
  return (
    <div className="rounded-2xl overflow-hidden aspect-[3/4] bg-muted">
      <Pulse className="w-full h-full rounded-none" />
    </div>
  );
}

export function PageSkeleton() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="flex flex-col items-center gap-4">
        <div className="w-12 h-12 rounded-2xl bg-muted/60 animate-pulse" />
        <Pulse className="h-4 w-40" />
      </div>
    </div>
  );
}

export function PropertyDetailSkeleton() {
  return (
    <div className="min-h-screen bg-background pt-20">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-8">
        <Pulse className="h-4 w-32 mb-8" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 sm:gap-8 lg:gap-12">
          <div className="lg:col-span-2 space-y-8">
            <Pulse className="aspect-[16/10] w-full rounded-2xl" />
            <div className="space-y-4">
              <Pulse className="h-8 w-3/4" />
              <Pulse className="h-4 w-1/2" />
            </div>
            <div className="flex flex-wrap gap-4">
              {[1, 2, 3, 4].map(i => (
                <div key={i} className="flex items-center gap-3">
                  <Pulse className="w-12 h-12 rounded-xl" />
                  <div className="space-y-1.5">
                    <Pulse className="h-3 w-16" />
                    <Pulse className="h-4 w-20" />
                  </div>
                </div>
              ))}
            </div>
          </div>
          <div className="lg:col-span-1">
            <Pulse className="h-64 w-full rounded-2xl" />
          </div>
        </div>
      </div>
    </div>
  );
}

export function ProjectDetailSkeleton() {
  return (
    <div className="min-h-screen bg-background">
      <Pulse className="h-[65vh] w-full mt-16 rounded-none" />
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Pulse className="h-4 w-24 mb-8" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
          <div className="lg:col-span-2 space-y-8">
            <Pulse className="h-6 w-48" />
            <Pulse className="h-4 w-full" />
            <Pulse className="h-4 w-5/6" />
            <Pulse className="h-4 w-4/6" />
          </div>
          <div>
            <Pulse className="h-64 w-full rounded-2xl" />
          </div>
        </div>
      </div>
    </div>
  );
}

export function SectionSkeleton({ count = 3 }: { count?: number }) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-8">
      {Array.from({ length: count }).map((_, i) => (
        <PropertyCardSkeleton key={i} />
      ))}
    </div>
  );
}

export function ProjectsSliderSkeleton() {
  const { language } = useLanguage();
  return (
    <div className="flex gap-4 px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto overflow-hidden" dir={language === 'ar' ? 'rtl' : 'ltr'}>
      {[1, 2, 3, 4].map(i => (
        <div key={i} className="min-w-[260px] sm:min-w-[300px] flex-shrink-0">
          <ProjectCardSkeleton />
        </div>
      ))}
    </div>
  );
}
