import { useState, useRef, useEffect, useCallback } from 'react';
import { Video, Play } from 'lucide-react';
import { useLanguage } from '../../i18n/LanguageContext';
import { PremiumVideo } from './PremiumVideo';
import { VideoLightbox } from './VideoLightbox';
import type { VideoItem } from '../../types/property';

interface VideoGalleryProps {
  videos: VideoItem[];
  title?: string;
  maxVisible?: number;
}

function Thumbnail({ video, onClick, index, total }: { video: VideoItem; onClick: () => void; index: number; total: number }) {
  const { t } = useLanguage();
  const imgRef = useRef<HTMLImageElement>(null);
  const [loaded, setLoaded] = useState(false);
  const [inView, setInView] = useState(false);
  const observerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = observerRef.current;
    if (!el) return;
    const obs = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setInView(true);
          obs.disconnect();
        }
      },
      { rootMargin: '200px' }
    );
    obs.observe(el);
    return () => obs.disconnect();
  }, []);

  return (
    <div ref={observerRef}>
      <button
        onClick={onClick}
        className="relative shrink-0 w-64 sm:w-72 aspect-video rounded-xl overflow-hidden border border-border/60 snap-start group cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-secondary"
        aria-label={t('gallery.counter', undefined, { current: String(index + 1), total: String(total) })}
      >
        {/* Base gradient background */}
        <div className="absolute inset-0 bg-gradient-to-br from-gray-800 to-gray-900" />

        {/* Thumbnail with blur-up */}
        {inView && video.thumbnailUrl ? (
          <>
            {/* Blur placeholder */}
            <div className="absolute inset-0 bg-gradient-to-br from-gray-700 to-gray-800 animate-pulse" />
            <img
              ref={imgRef}
              src={video.thumbnailUrl}
              alt=""
              loading="lazy"
              width={320}
              height={180}
              onLoad={() => setLoaded(true)}
              className={`absolute inset-0 w-full h-full object-cover transition-all duration-700 group-hover:scale-105 ${loaded ? 'opacity-100 blur-0' : 'opacity-0 blur-xl'}`}
            />
          </>
        ) : (
          <div className="absolute inset-0 bg-gradient-to-br from-gray-800 to-gray-900 flex items-center justify-center">
            <div className="w-10 h-10 rounded-full bg-white/5 flex items-center justify-center">
              <Video className="w-5 h-5 text-white/20" />
            </div>
          </div>
        )}

        {/* Gradient overlay */}
        <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-black/10 group-hover:from-black/40 transition-colors duration-500" />

        {/* Duration badge */}
        {video.duration && (
          <div className="absolute bottom-2 left-2 z-10 px-2 py-0.5 rounded-md bg-black/60 backdrop-blur-sm text-white/80 text-[10px] font-mono tabular-nums">
            {Math.floor(video.duration / 60)}:{Math.floor(video.duration % 60).toString().padStart(2, '0')}
          </div>
        )}

        {/* Play button */}
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="w-12 h-12 rounded-full bg-white/95 backdrop-blur-sm flex items-center justify-center shadow-xl shadow-black/30 group-hover:scale-110 group-hover:bg-white transition-all duration-300">
            <Play className="w-5 h-5 text-navy ml-0.5" fill="currentColor" />
          </div>
        </div>

        {/* Hover shine effect */}
        <div className="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-500 pointer-events-none">
          <div className="absolute inset-0 bg-gradient-to-tr from-white/0 via-white/5 to-white/0" />
        </div>
      </button>
    </div>
  );
}

export function VideoGallery({ videos, title, maxVisible = 3 }: VideoGalleryProps) {
  const { t } = useLanguage();
  const [activeVideo, setActiveVideo] = useState(0);
  const [lightboxOpen, setLightboxOpen] = useState(false);

  if (!videos || videos.length === 0) return null;

  const visibleVideos = videos.slice(0, maxVisible);
  const remaining = videos.length - maxVisible;

  const openLightbox = useCallback((index: number) => {
    setActiveVideo(index);
    setLightboxOpen(true);
  }, []);

  return (
    <section className="animate-section-enter">
      {/* Header */}
      <div className="flex items-center gap-3 mb-4">
        <div className="flex-1">
          <h2 className="font-display text-xl sm:text-2xl font-bold">{title || t('property.videos')}</h2>
        </div>
        {videos.length > 1 && (
          <span className="text-xs text-muted-foreground/60 font-mono">{videos.length} videos</span>
        )}
      </div>
      <div className="w-12 h-1 bg-gradient-to-r from-gold to-gold/40 rounded-full mb-6" />

      {/* Thumbnail strip */}
      <div className="flex gap-3 overflow-x-auto pb-2 snap-x snap-mandatory scrollbar-thin scrollbar-thumb-rounded-full scrollbar-track-transparent -mx-1 px-1">
        {visibleVideos.map((video, idx) => (
          <Thumbnail
            key={video.id}
            video={video}
            index={idx}
            total={videos.length}
            onClick={() => openLightbox(idx)}
          />
        ))}

        {/* "+N more" button */}
        {remaining > 0 && (
          <button
            onClick={() => openLightbox(0)}
            className="shrink-0 w-24 sm:w-28 aspect-video rounded-xl border border-dashed border-border/60 flex flex-col items-center justify-center text-muted-foreground hover:text-primary hover:border-primary/60 transition-all duration-300 snap-start group"
          >
            <span className="text-lg font-bold tabular-nums group-hover:scale-110 transition-transform duration-300">+{remaining}</span>
            <span className="text-[10px] opacity-60">{t('general.more')}</span>
          </button>
        )}
      </div>

      {/* Lightbox */}
      <VideoLightbox
        open={lightboxOpen}
        videos={videos}
        activeIndex={activeVideo}
        onClose={() => setLightboxOpen(false)}
        onPrev={() => setActiveVideo(prev => (prev > 0 ? prev - 1 : videos.length - 1))}
        onNext={() => setActiveVideo(prev => (prev < videos.length - 1 ? prev + 1 : 0))}
        title={title}
      />

      <style>{`
        @keyframes section-enter {
          from { opacity: 0; transform: translateY(12px); }
          to { opacity: 1; transform: translateY(0); }
        }
        .animate-section-enter {
          animation: section-enter 0.5s ease-out;
        }
        .scrollbar-thin::-webkit-scrollbar {
          height: 4px;
        }
        .scrollbar-thin::-webkit-scrollbar-track {
          background: transparent;
        }
        .scrollbar-thin::-webkit-scrollbar-thumb {
          background: rgba(255,255,255,0.1);
          border-radius: 99px;
        }
        .scrollbar-thin::-webkit-scrollbar-thumb:hover {
          background: rgba(255,255,255,0.2);
        }
      `}</style>
    </section>
  );
}
