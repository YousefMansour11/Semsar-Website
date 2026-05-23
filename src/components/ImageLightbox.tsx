import { useEffect, useCallback, useRef } from 'react';
import { X, ChevronLeft, ChevronRight } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import { optimizeCloudinaryUrl } from '../lib/utils';

interface ImageLightboxProps {
  open: boolean;
  images: string[];
  activeIndex: number;
  onClose: () => void;
  onPrev: () => void;
  onNext: () => void;
  title?: string;
}

export function ImageLightbox({ open, images, activeIndex, onClose, onPrev, onNext, title }: ImageLightboxProps) {
  const { t, language } = useLanguage();
  const touchStartX = useRef(0);
  const touchEndX = useRef(0);

  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if (e.key === 'Escape') onClose();
    if (e.key === 'ArrowLeft') {
      if (language === 'ar') onNext(); else onPrev();
    }
    if (e.key === 'ArrowRight') {
      if (language === 'ar') onPrev(); else onNext();
    }
  }, [onClose, onPrev, onNext, language]);

  useEffect(() => {
    if (!open) return;
    window.addEventListener('keydown', handleKeyDown);
    document.body.style.overflow = 'hidden';
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = '';
    };
  }, [open, handleKeyDown]);

  const handleTouchStart = (e: React.TouchEvent) => {
    touchStartX.current = e.touches[0].clientX;
  };

  const handleTouchMove = (e: React.TouchEvent) => {
    touchEndX.current = e.touches[0].clientX;
  };

  const handleTouchEnd = () => {
    const diff = touchStartX.current - touchEndX.current;
    if (Math.abs(diff) > 50) {
      if (language === 'ar') {
        if (diff > 0) onPrev(); else onNext();
      } else {
        if (diff > 0) onNext(); else onPrev();
      }
    }
  };

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center touch-pan-y"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
      aria-label={title || t('gallery.openLightbox')}
    >
      <div className="absolute inset-0 bg-gradient-to-b from-navy/98 via-navy/95 to-navy/98 backdrop-blur-sm" />

      <button
        onClick={onClose}
        className={`absolute top-4 ${language === 'ar' ? 'left-4' : 'right-4'} z-30 w-11 h-11 rounded-full bg-black/40 backdrop-blur-md flex items-center justify-center text-white hover:bg-gold hover:text-navy transition-all border border-white/30 hover:border-gold shadow-lg`}
        aria-label={t('modal.close')}
      >
        <X className="w-6 h-6" />
      </button>

      {images.length > 1 && (
        <>
          <button
            onClick={(e) => { e.stopPropagation(); onPrev(); }}
            aria-label={t('gallery.previous')}
            className={`absolute ${language === 'ar' ? 'right-2 sm:right-6' : 'left-2 sm:left-6'} top-1/2 -translate-y-1/2 z-30 w-11 h-11 sm:w-14 sm:h-14 rounded-full bg-black/40 backdrop-blur-md flex items-center justify-center text-white hover:bg-gold hover:text-navy transition-all border border-white/30 hover:border-gold shadow-lg`}
          >
            {language === 'ar' ? <ChevronRight className="w-5 h-5 sm:w-6 sm:h-6" /> : <ChevronLeft className="w-5 h-5 sm:w-6 sm:h-6" />}
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); onNext(); }}
            aria-label={t('gallery.next')}
            className={`absolute ${language === 'ar' ? 'left-2 sm:left-6' : 'right-2 sm:right-6'} top-1/2 -translate-y-1/2 z-30 w-11 h-11 sm:w-14 sm:h-14 rounded-full bg-black/40 backdrop-blur-md flex items-center justify-center text-white hover:bg-gold hover:text-navy transition-all border border-white/30 hover:border-gold shadow-lg`}
          >
            {language === 'ar' ? <ChevronLeft className="w-5 h-5 sm:w-6 sm:h-6" /> : <ChevronRight className="w-5 h-5 sm:w-6 sm:h-6" />}
          </button>
        </>
      )}

      <div
        className="relative z-10 flex items-center justify-center w-full h-full p-2 sm:p-8"
        onClick={(e) => e.stopPropagation()}
        onTouchStart={handleTouchStart}
        onTouchMove={handleTouchMove}
        onTouchEnd={handleTouchEnd}
      >
        <img
          src={optimizeCloudinaryUrl(images[activeIndex], 1200)}
          alt={title ? `${title} - ${t('gallery.counter', { current: String(activeIndex + 1), total: String(images.length) })}` : ''}
          className="max-h-[85vh] max-w-full sm:max-w-[90vw] object-contain rounded-2xl shadow-2xl shadow-black/50 pointer-events-none select-none"
          draggable={false}
        />
      </div>

      <div className="absolute bottom-6 left-1/2 -translate-x-1/2 z-20 px-4 py-2 rounded-full bg-black/40 backdrop-blur-md border border-white/20 text-white text-sm font-medium pointer-events-none">
        {activeIndex + 1} / {images.length}
      </div>
    </div>
  );
}
