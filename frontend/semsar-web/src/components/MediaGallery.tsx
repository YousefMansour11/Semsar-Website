import { useState, useRef, useEffect, useCallback } from 'react';
import { Play, Video, ChevronLeft, ChevronRight } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import { MediaLightbox } from './MediaLightbox';
import { PremiumImage } from './PremiumImage';
import { MiniPlayer } from './MiniPlayer';
import { usePrefersReducedMotion } from '../hooks/usePrefersReducedMotion';
import type { MediaItem } from '../types/media';

interface MediaGalleryProps {
  items: MediaItem[];
  heroIndex: number;
  title?: string;
}

function MediaThumbnail({ item, onClick, index, total, isHero }: { item: MediaItem; onClick: () => void; index: number; total: number; isHero: boolean }) {
  const { t } = useLanguage();
  const imgRef = useRef<HTMLDivElement>(null);
  const [inView, setInView] = useState(false);

  useEffect(() => {
    const el = imgRef.current;
    if (!el) return;
    const obs = new IntersectionObserver(([entry]) => { if (entry.isIntersecting) { setInView(true); obs.disconnect(); } }, { rootMargin: '200px' });
    obs.observe(el);
    return () => obs.disconnect();
  }, []);

  return (
    <div ref={imgRef} className={`shrink-0 snap-start ${isHero ? 'w-full' : 'w-32 sm:w-40'}`}>
      <button
        onClick={onClick}
        className={`relative overflow-hidden rounded-lg border group cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-secondary ${isHero ? 'w-full aspect-[16/10]' : 'w-full aspect-video'} ${!isHero ? 'border-border/40 hover:border-secondary transition-colors' : 'border-border/60'}`}
        aria-label={isHero ? t('gallery.openLightbox') : t('gallery.counter', undefined, { current: String(index + 1), total: String(total) })}
      >
        {item.type === 'video' ? (
          <>
            <div className="absolute inset-0 bg-gradient-to-br from-gray-800 to-gray-900" />
            {inView && item.thumbnailUrl ? (
              <>
                <div className="absolute inset-0 bg-gradient-to-br from-gray-700 to-gray-800 animate-pulse" />
                <img src={item.thumbnailUrl} alt="" loading="lazy" className="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-105" />
              </>
            ) : (
              <div className="absolute inset-0 bg-gradient-to-br from-gray-800 to-gray-900 flex items-center justify-center">
                <div className="w-8 h-8 rounded-full bg-white/5 flex items-center justify-center"><Video className="w-4 h-4 text-white/20" /></div>
              </div>
            )}
            {!isHero && <div className="absolute inset-0 bg-black/10 flex items-center justify-center"><Play className="w-5 h-5 text-white/70" fill="currentColor" /></div>}
            {isHero && (
              <>
                <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-black/10 group-hover:from-black/40 transition-colors duration-500" />
                <div className="absolute inset-0 flex items-center justify-center">
                  <div className="w-16 h-16 rounded-full bg-white/95 backdrop-blur-sm flex items-center justify-center shadow-xl shadow-black/30 group-hover:scale-110 group-hover:bg-white transition-[transform,background-color] duration-300 ease-out">
                    <Play className="w-7 h-7 text-navy ml-0.5" fill="currentColor" />
                  </div>
                </div>
              </>
            )}
          </>
        ) : (
          <PremiumImage
            src={item.url}
            alt=""
            width={isHero ? 1600 : 320}
            height={isHero ? 1000 : 180}
            profile={isHero ? 'gallery' : 'thumbnail'}
            className="w-full h-full"
            imgClassName={isHero ? 'transition-transform duration-700 ease-out' : ''}
          />
        )}
      </button>
    </div>
  );
}

export function MediaGallery({ items, heroIndex, title }: MediaGalleryProps) {
  const { t, language } = useLanguage();
  const rtl = language === 'ar';
  const prefersReducedMotion = usePrefersReducedMotion();
  const [activeIndex, setActiveIndex] = useState(heroIndex);
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [minimizedVideo, setMinimizedVideo] = useState<{ item: MediaItem; currentTime: number; isMuted: boolean } | null>(null);
  const heroRef = useRef<HTMLDivElement>(null);
  const touchStartX = useRef(0);
  const touchStartY = useRef(0);
  const openLightbox = useCallback((index: number) => {
    setActiveIndex(index);
    setLightboxOpen(true);
    setMinimizedVideo(null);
  }, []);

  const handlePrev = useCallback(() => {
    setActiveIndex(prev => (prev > 0 ? prev - 1 : items.length - 1));
  }, [items.length]);

  const handleNext = useCallback(() => {
    setActiveIndex(prev => (prev < items.length - 1 ? prev + 1 : 0));
  }, [items.length]);

  const handleMinimize = useCallback((currentTime: number, isMuted: boolean) => {
    const activeItem = items[activeIndex];
    setMinimizedVideo({ item: activeItem, currentTime, isMuted });
    setLightboxOpen(false);
  }, [items, activeIndex]);

  const handleRestore = useCallback(() => {
    if (!minimizedVideo) return;
    const idx = items.findIndex(i => i.id === minimizedVideo.item.id);
    if (idx >= 0) setActiveIndex(idx);
    setLightboxOpen(true);
    setMinimizedVideo(null);
  }, [minimizedVideo, items]);

  const handleCloseMinimized = useCallback(() => {
    setMinimizedVideo(null);
  }, []);

  const handleTouchStart = (e: React.TouchEvent) => {
    touchStartX.current = e.touches[0].clientX;
    touchStartY.current = e.touches[0].clientY;
  };

  const handleTouchEnd = (e: React.TouchEvent) => {
    if (items.length <= 1) return;
    const dx = e.changedTouches[0].clientX - touchStartX.current;
    const dy = e.changedTouches[0].clientY - touchStartY.current;
    if (Math.abs(dx) > Math.abs(dy) && Math.abs(dx) > 50) {
      if (dx < 0) {
        if (rtl) handlePrev(); else handleNext();
      } else {
        if (rtl) handleNext(); else handlePrev();
      }
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (items.length <= 1) return;
    if (e.key === 'ArrowLeft') {
      e.preventDefault();
      if (rtl) handleNext(); else handlePrev();
    } else if (e.key === 'ArrowRight') {
      e.preventDefault();
      if (rtl) handlePrev(); else handleNext();
    }
  };

  if (!items || items.length === 0) return null;

  const activeItem = items[activeIndex];

  return (
    <div className="rounded-2xl overflow-hidden shadow-lg border border-border relative group">
      {/* Hero display with nav arrows */}
      <div
        ref={heroRef}
        className="aspect-[16/10] bg-muted relative overflow-hidden will-change-transform"
        onTouchStart={handleTouchStart}
        onTouchEnd={handleTouchEnd}
        onKeyDown={handleKeyDown}
        tabIndex={0}
        role="region"
        aria-label={title || t('gallery.openLightbox')}
        aria-roledescription="carousel"
      >
        <div key={activeIndex} className={`${prefersReducedMotion ? '' : 'animate-heroFade'}`}>
          <MediaThumbnail
            item={activeItem}
            onClick={() => openLightbox(activeIndex)}
            index={activeIndex}
            total={items.length}
            isHero
          />
        </div>

        {items.length > 1 && (
          <>
            <button onClick={(e) => { e.stopPropagation(); handlePrev(); }} aria-label={t('gallery.previous')}
              className={`absolute ${rtl ? 'right-3' : 'left-3'} top-1/2 -translate-y-1/2 w-11 h-11 rounded-full bg-white/80 backdrop-blur-sm shadow-lg flex items-center justify-center text-navy hover:bg-white transition-colors duration-200 active:scale-90 z-10 opacity-0 group-hover:opacity-100 focus-visible:opacity-100`}>
              {rtl ? <ChevronRight className="w-5 h-5" /> : <ChevronLeft className="w-5 h-5" />}
            </button>
            <button onClick={(e) => { e.stopPropagation(); handleNext(); }} aria-label={t('gallery.next')}
              className={`absolute ${rtl ? 'left-3' : 'right-3'} top-1/2 -translate-y-1/2 w-11 h-11 rounded-full bg-white/80 backdrop-blur-sm shadow-lg flex items-center justify-center text-navy hover:bg-white transition-colors duration-200 active:scale-90 z-10 opacity-0 group-hover:opacity-100 focus-visible:opacity-100`}>
              {rtl ? <ChevronLeft className="w-5 h-5" /> : <ChevronRight className="w-5 h-5" />}
            </button>
          </>
        )}

        <div className={`absolute bottom-3 px-2.5 py-1 rounded-lg bg-black/50 backdrop-blur-sm text-white text-xs font-medium z-10 pointer-events-none ${rtl ? 'left-3' : 'right-3'}`}>
          {activeIndex + 1} / {items.length}
        </div>
      </div>

      {/* Thumbnail strip — clicks switch hero display */}
      {items.length > 1 && (
        <div className="flex gap-2 p-2 overflow-x-auto overflow-y-hidden bg-card scrollbar-thin scrollbar-thumb-rounded-full scrollbar-track-transparent snap-x snap-mandatory">
          {items.map((item, idx) => (
            <MediaThumbnail
              key={item.id}
              item={item}
              onClick={() => setActiveIndex(idx)}
              index={idx}
              total={items.length}
              isHero={false}
            />
          ))}
        </div>
      )}

      <MediaLightbox
        open={lightboxOpen}
        items={items}
        activeIndex={activeIndex}
        onClose={() => setLightboxOpen(false)}
        onPrev={handlePrev}
        onNext={handleNext}
        title={title}
        onMinimize={handleMinimize}
      />

      {minimizedVideo && (
        <MiniPlayer
          video={minimizedVideo.item}
          currentTime={minimizedVideo.currentTime}
          isMuted={minimizedVideo.isMuted}
          onRestore={handleRestore}
          onClose={handleCloseMinimized}
        />
      )}


    </div>
  );
}
