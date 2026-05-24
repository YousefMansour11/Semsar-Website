import { useEffect, useCallback, useRef, useState } from 'react';
import { X, ChevronLeft, ChevronRight } from 'lucide-react';
import { useLanguage } from '../../i18n/LanguageContext';
import { PremiumVideo } from './PremiumVideo';
import type { VideoItem } from '../../types/property';

interface VideoLightboxProps {
  open: boolean;
  videos: VideoItem[];
  activeIndex: number;
  onClose: () => void;
  onPrev: () => void;
  onNext: () => void;
  title?: string;
}

export function VideoLightbox({ open, videos, activeIndex, onClose, onPrev, onNext, title }: VideoLightboxProps) {
  const { t, language } = useLanguage();
  const dialogRef = useRef<HTMLDivElement>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);
  const touchStartX = useRef(0);
  const touchStartY = useRef(0);
  const [isTransitioning, setIsTransitioning] = useState(false);
  const [slideDirection, setSlideDirection] = useState<'left' | 'right'>('right');
  const rtl = language === 'ar';

  // Focus trap
  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if (e.key === 'Escape') { onClose(); return; }

    if (e.key === 'ArrowLeft') {
      e.preventDefault();
      if (rtl) handleNext(); else handlePrev();
      return;
    }
    if (e.key === 'ArrowRight') {
      e.preventDefault();
      if (rtl) handlePrev(); else handleNext();
      return;
    }

    if (e.key === 'Tab') {
      const dialog = dialogRef.current;
      if (!dialog) return;
      const focusable = dialog.querySelectorAll<HTMLElement>(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      );
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (e.shiftKey) {
        if (document.activeElement === first) { e.preventDefault(); last?.focus(); }
      } else {
        if (document.activeElement === last) { e.preventDefault(); first?.focus(); }
      }
    }
  }, [onClose, activeIndex, videos.length, rtl]);

  useEffect(() => {
    if (!open) return;
    previousFocusRef.current = document.activeElement as HTMLElement;
    window.addEventListener('keydown', handleKeyDown);
    document.body.style.overflow = 'hidden';
    document.body.style.paddingRight = 'var(--scrollbar-width, 0px)';

    setTimeout(() => {
      const dialog = dialogRef.current;
      if (dialog) {
        const closeBtn = dialog.querySelector<HTMLElement>('button');
        closeBtn?.focus();
      }
    }, 100);

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = '';
      document.body.style.paddingRight = '';
      previousFocusRef.current?.focus();
    };
  }, [open, handleKeyDown]);

  // Swipe handlers
  const handleTouchStart = (e: React.TouchEvent) => {
    touchStartX.current = e.touches[0].clientX;
    touchStartY.current = e.touches[0].clientY;
  };

  const handleTouchEnd = (e: React.TouchEvent) => {
    if (videos.length <= 1) return;
    const dx = e.changedTouches[0].clientX - touchStartX.current;
    const dy = e.changedTouches[0].clientY - touchStartY.current;
    if (Math.abs(dx) > Math.abs(dy) && Math.abs(dx) > 60) {
      if (dx < 0) {
        rtl ? handlePrev() : handleNext();
      } else {
        rtl ? handleNext() : handlePrev();
      }
    }
  };

  const handlePrev = () => {
    if (isTransitioning) return;
    setSlideDirection('left');
    setIsTransitioning(true);
    onPrev();
    setTimeout(() => setIsTransitioning(false), 300);
  };

  const handleNext = () => {
    if (isTransitioning) return;
    setSlideDirection('right');
    setIsTransitioning(true);
    onNext();
    setTimeout(() => setIsTransitioning(false), 300);
  };

  if (!open || !videos.length) return null;

  const video = videos[activeIndex];

  return (
    <div
      ref={dialogRef}
      className="fixed inset-0 z-50 flex items-center justify-center animate-lightbox-enter"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
      aria-label={title || t('gallery.openLightbox')}
      onTouchStart={handleTouchStart}
      onTouchEnd={handleTouchEnd}
      style={{ overscrollBehavior: 'contain' }}
    >
      {/* Backdrop */}
      <div className="absolute inset-0 bg-gradient-to-b from-navy/98 via-navy/96 to-navy/98 backdrop-blur-xl" />

      {/* Close button */}
      <button
        onClick={onClose}
        className={`absolute top-4 z-30 w-12 h-12 rounded-full bg-black/30 backdrop-blur-md flex items-center justify-center text-white/90 hover:bg-gold hover:text-navy transition-all duration-300 border border-white/20 hover:border-gold shadow-lg hover:scale-105 ${language === 'ar' ? 'left-4' : 'right-4'}`}
        aria-label={t('modal.close')}
      >
        <X className="w-5 h-5" />
      </button>

      {/* Navigation arrows */}
      {videos.length > 1 && (
        <>
          <button
            onClick={(e) => { e.stopPropagation(); handlePrev(); }}
            aria-label={t('gallery.previous')}
            className={`absolute z-30 w-12 h-12 sm:w-14 sm:h-14 rounded-full bg-black/30 backdrop-blur-md flex items-center justify-center text-white/80 hover:bg-gold hover:text-navy transition-all duration-300 border border-white/20 hover:border-gold shadow-lg hover:scale-105 ${language === 'ar' ? 'right-3 sm:right-6' : 'left-3 sm:left-6'} top-1/2 -translate-y-1/2`}
          >
            {rtl ? <ChevronRight className="w-5 h-5 sm:w-6 sm:h-6" /> : <ChevronLeft className="w-5 h-5 sm:w-6 sm:h-6" />}
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); handleNext(); }}
            aria-label={t('gallery.next')}
            className={`absolute z-30 w-12 h-12 sm:w-14 sm:h-14 rounded-full bg-black/30 backdrop-blur-md flex items-center justify-center text-white/80 hover:bg-gold hover:text-navy transition-all duration-300 border border-white/20 hover:border-gold shadow-lg hover:scale-105 ${language === 'ar' ? 'left-3 sm:left-6' : 'right-3 sm:right-6'} top-1/2 -translate-y-1/2`}
          >
            {rtl ? <ChevronLeft className="w-5 h-5 sm:w-6 sm:h-6" /> : <ChevronRight className="w-5 h-5 sm:w-6 sm:h-6" />}
          </button>
        </>
      )}

      {/* Video container */}
      <div
        className="relative z-10 flex items-center justify-center w-full h-full p-2 sm:p-8 md:p-12"
        onClick={(e) => e.stopPropagation()}
        style={{ paddingTop: 'env(safe-area-inset-top)', paddingBottom: 'env(safe-area-inset-bottom)' }}
      >
        <div
          className={`w-full max-w-5xl max-h-[85vh] rounded-2xl overflow-hidden shadow-2xl shadow-black/50 transition-all duration-300 ${isTransitioning ? (slideDirection === 'right' ? 'animate-slide-in-right' : 'animate-slide-in-left') : ''}`}
        >
          <PremiumVideo
            src={video.url}
            poster={video.thumbnailUrl}
            title={title}
            aspectRatio="16/9"
            autoPlay
            muted={false}
          />
        </div>
      </div>

      {/* Counter badge */}
      {videos.length > 1 && (
        <div className="absolute bottom-6 left-1/2 -translate-x-1/2 z-20 px-4 py-2 rounded-full bg-black/40 backdrop-blur-md border border-white/15 text-white/80 text-sm font-medium pointer-events-none shadow-lg">
          {activeIndex + 1} / {videos.length}
        </div>
      )}

      <style>{`
        @keyframes lightbox-enter {
          from { opacity: 0; }
          to { opacity: 1; }
        }
        @keyframes slide-in-right {
          from { opacity: 0; transform: translateX(40px) scale(0.96); }
          to { opacity: 1; transform: translateX(0) scale(1); }
        }
        @keyframes slide-in-left {
          from { opacity: 0; transform: translateX(-40px) scale(0.96); }
          to { opacity: 1; transform: translateX(0) scale(1); }
        }
        .animate-lightbox-enter {
          animation: lightbox-enter 0.3s ease-out;
        }
        .animate-slide-in-right {
          animation: slide-in-right 0.3s ease-out;
        }
        .animate-slide-in-left {
          animation: slide-in-left 0.3s ease-out;
        }
      `}</style>
    </div>
  );
}
