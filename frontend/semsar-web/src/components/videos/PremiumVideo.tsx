import { useRef, useState, useEffect, useCallback } from 'react';
import { Play, Pause, Volume2, VolumeX, Maximize, Minimize, Minimize2, Settings } from 'lucide-react';
import { useLanguage } from '../../i18n/LanguageContext';
import { optimizeCloudinaryVideoUrl, getCloudinaryVideoPosterUrl } from '../../lib/utils';
import { PremiumImage } from '../PremiumImage';

interface PremiumVideoProps {
  src: string;
  poster?: string;
  posterTimestamp?: number;
  title?: string;
  className?: string;
  aspectRatio?: string;
  autoPlay?: boolean;
  muted?: boolean;
  loop?: boolean;
  onPlay?: () => void;
  onPause?: () => void;
  onEnded?: () => void;
  onMinimize?: (currentTime: number, isMuted: boolean) => void;
}

type Quality = 'auto' | '1080p' | '720p' | '480p';
const QUALITY_LABELS: Record<Quality, string> = { auto: 'Auto', '1080p': '1080p', '720p': '720p', '480p': '480p' };

export function PremiumVideo({
  src,
  poster,
  posterTimestamp,
  title,
  className = '',
  aspectRatio = '16/9',
  autoPlay = false,
  muted: initialMuted = false,
  loop = false,
  onPlay,
  onPause,
  onEnded,
  onMinimize,
}: PremiumVideoProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const hideTimer = useRef<ReturnType<typeof setTimeout>>();
  const isDraggingRef = useRef(false);
  const seekRectRef = useRef<DOMRect | null>(null);
  const [isPlaying, setIsPlaying] = useState(autoPlay);
  const [isMuted, setIsMuted] = useState(initialMuted);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [volume, setVolume] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [isBuffering, setIsBuffering] = useState(false);
  const [error, setError] = useState(false);
  const [showControls, setShowControls] = useState(true);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [showPoster, setShowPoster] = useState(true);
  const [showVolumeSlider, setShowVolumeSlider] = useState(false);
  const [quality, setQuality] = useState<Quality>('auto');
  const [showQualityMenu, setShowQualityMenu] = useState(false);
  const { language } = useLanguage();
  const rtl = language === 'ar';

  const onPlayRef = useRef(onPlay);
  const onPauseRef = useRef(onPause);
  const onEndedRef = useRef(onEnded);
  useEffect(() => { onPlayRef.current = onPlay; });
  useEffect(() => { onPauseRef.current = onPause; });
  useEffect(() => { onEndedRef.current = onEnded; });

  const qualityChangeRef = useRef<{ seek: number; play: boolean } | null>(null);

  const posterUrl = poster || getCloudinaryVideoPosterUrl(src, posterTimestamp);

  useEffect(() => {
    setError(false);
    setIsLoading(true);
    setIsBuffering(false);
    setIsPlaying(autoPlay);
    setCurrentTime(0);
    setDuration(0);
    setShowPoster(true);
  }, [src, autoPlay]);

  const syncVideoSource = useCallback(() => {
    const el = videoRef.current;
    if (!el) return;
    const pending = qualityChangeRef.current;
    qualityChangeRef.current = null;
    el.src = optimizeCloudinaryVideoUrl(src, quality);
    if (pending) {
      el.currentTime = pending.seek;
      if (pending.play) el.play().catch(() => {});
    }
  }, [src, quality]);

  useEffect(() => { syncVideoSource(); }, [syncVideoSource]);

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;

    const handleTimeUpdate = () => setCurrentTime(video.currentTime);
    const handleDurationChange = () => { setDuration(video.duration || 0); setIsLoading(false); };
    const handlePlay = () => { setIsPlaying(true); setShowPoster(false); onPlayRef.current?.(); };
    const handlePause = () => { onPauseRef.current?.(); setIsPlaying(false); };
    const handleEnded = () => { setIsPlaying(false); onEndedRef.current?.(); };
    const handleErrorEvent = () => { setError(true); setIsLoading(false); setIsBuffering(false); };
    const handleCanPlay = () => { setIsLoading(false); setIsBuffering(false); };
    const handleWaiting = () => setIsBuffering(true);
    const handlePlaying = () => { setIsLoading(false); setIsBuffering(false); };
    const handleVolumeChange = () => { setVolume(video.volume); setIsMuted(video.muted); };
    const handleFullscreenChange = () => setIsFullscreen(!!document.fullscreenElement);

    video.addEventListener('timeupdate', handleTimeUpdate);
    video.addEventListener('durationchange', handleDurationChange);
    video.addEventListener('play', handlePlay);
    video.addEventListener('pause', handlePause);
    video.addEventListener('ended', handleEnded);
    video.addEventListener('error', handleErrorEvent);
    video.addEventListener('canplay', handleCanPlay);
    video.addEventListener('waiting', handleWaiting);
    video.addEventListener('playing', handlePlaying);
    video.addEventListener('volumechange', handleVolumeChange);
    document.addEventListener('fullscreenchange', handleFullscreenChange);

    return () => {
      video.removeEventListener('timeupdate', handleTimeUpdate);
      video.removeEventListener('durationchange', handleDurationChange);
      video.removeEventListener('play', handlePlay);
      video.removeEventListener('pause', handlePause);
      video.removeEventListener('ended', handleEnded);
      video.removeEventListener('error', handleErrorEvent);
      video.removeEventListener('canplay', handleCanPlay);
      video.removeEventListener('waiting', handleWaiting);
      video.removeEventListener('playing', handlePlaying);
      video.removeEventListener('volumechange', handleVolumeChange);
      document.removeEventListener('fullscreenchange', handleFullscreenChange);
    };
  }, [src]);

  useEffect(() => {
    if (!videoRef.current) return;
    videoRef.current.muted = isMuted;
  }, [isMuted]);

  const togglePlay = useCallback(() => {
    if (!videoRef.current) return;
    if (isPlaying) videoRef.current.pause();
    else videoRef.current.play().catch(() => setError(true));
  }, [isPlaying]);

  const toggleMute = useCallback(() => setIsMuted(prev => !prev), []);

  const handleVolumeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const v = parseFloat(e.target.value);
    if (!videoRef.current) return;
    videoRef.current.volume = v;
    setVolume(v);
    if (v === 0) setIsMuted(true);
    else if (isMuted) setIsMuted(false);
  };

  const handleSeekMouseDown = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!videoRef.current || !duration) return;
    isDraggingRef.current = true;
    seekRectRef.current = e.currentTarget.getBoundingClientRect();
    const newTime = ((e.clientX - seekRectRef.current.left) / seekRectRef.current.width) * duration;
    videoRef.current.currentTime = newTime;
    setCurrentTime(newTime);
  };

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      if (!isDraggingRef.current || !videoRef.current || !duration || !seekRectRef.current) return;
      const newTime = Math.max(0, Math.min(duration, ((e.clientX - seekRectRef.current.left) / seekRectRef.current.width) * duration));
      videoRef.current.currentTime = newTime;
      setCurrentTime(newTime);
    };
    const handleMouseUp = () => {
      isDraggingRef.current = false;
      seekRectRef.current = null;
    };
    window.addEventListener('mousemove', handleMouseMove);
    window.addEventListener('mouseup', handleMouseUp);
    return () => {
      window.removeEventListener('mousemove', handleMouseMove);
      window.removeEventListener('mouseup', handleMouseUp);
    };
  }, [duration]);

  const skip = (seconds: number) => {
    if (!videoRef.current) return;
    videoRef.current.currentTime = Math.max(0, Math.min(videoRef.current.duration || 0, videoRef.current.currentTime + seconds));
  };

  const toggleFullscreen = useCallback(() => {
    if (!containerRef.current) return;
    if (!document.fullscreenElement) containerRef.current.requestFullscreen?.();
    else document.exitFullscreen?.();
  }, []);

  const handleMinimize = () => {
    videoRef.current?.pause();
    onMinimize?.(currentTime, isMuted);
  };

  const changeQuality = (q: Quality) => {
    const el = videoRef.current;
    if (!el) return;
    qualityChangeRef.current = { seek: el.currentTime, play: isPlaying };
    setQuality(q);
    setShowQualityMenu(false);
  };

  const qualityMenuRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (qualityMenuRef.current && !qualityMenuRef.current.contains(e.target as Node)) {
        setShowQualityMenu(false);
      }
    };
    window.addEventListener('click', handleClickOutside);
    return () => window.removeEventListener('click', handleClickOutside);
  }, []);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!containerRef.current?.contains(document.activeElement) && !document.fullscreenElement) return;
      switch (e.key) {
        case ' ':
        case 'k':
        case 'K': e.preventDefault(); togglePlay(); break;
        case 'j':
        case 'J': e.preventDefault(); skip(-10); break;
        case 'l':
        case 'L': e.preventDefault(); skip(10); break;
        case 'f':
        case 'F': e.preventDefault(); toggleFullscreen(); break;
        case 'm':
        case 'M': e.preventDefault(); toggleMute(); break;
        case 'ArrowLeft': e.preventDefault(); skip(rtl ? 5 : -5); break;
        case 'ArrowRight': e.preventDefault(); skip(rtl ? -5 : 5); break;
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [togglePlay, toggleFullscreen, toggleMute, rtl]);

  const handleDoubleClick = () => toggleFullscreen();

  useEffect(() => {
    return () => clearTimeout(hideTimer.current);
  }, []);

  const handleMouseMove = () => {
    setShowControls(true);
    clearTimeout(hideTimer.current);
    hideTimer.current = setTimeout(() => {
      if (isPlaying && !showVolumeSlider && !showQualityMenu) setShowControls(false);
    }, 3000);
  };

  const formatTime = (seconds: number) => {
    if (!seconds || isNaN(seconds)) return '0:00';
    return `${Math.floor(seconds / 60)}:${Math.floor(seconds % 60).toString().padStart(2, '0')}`;
  };

  const progress = duration > 0 ? (currentTime / duration) * 100 : 0;

  return (
    <div
      ref={containerRef}
      className={`relative overflow-hidden bg-black rounded-2xl shadow-2xl shadow-black/30 group animate-fadeIn ${className}`}
      style={{ aspectRatio }}
      onMouseMove={handleMouseMove}
      onMouseLeave={() => isPlaying && setShowControls(false)}
      onDoubleClick={handleDoubleClick}
      dir="ltr"
      role="application"
      aria-label={title || (language === 'ar' ? 'مشغل فيديو' : 'Video player')}
    >
      {/* Poster */}
      {posterUrl && showPoster && (
        <div className="absolute inset-0 z-10 transition-opacity duration-700 ease-out opacity-100">
          <PremiumImage src={posterUrl} alt="" width={1280} height={720} className="absolute inset-0" imgClassName="object-cover" />
          <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />
        </div>
      )}

      {error ? (
        <div className="absolute inset-0 z-20 flex items-center justify-center bg-gradient-to-br from-gray-900 to-black">
          <div className="text-center px-6">
            <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-white/10 flex items-center justify-center"><VolumeX className="w-7 h-7 text-white/60" /></div>
            <p className="text-white/80 text-sm font-medium">{language === 'ar' ? 'تعذر تحميل الفيديو' : 'Video failed to load'}</p>
            <p className="text-white/40 text-xs mt-1">{title}</p>
            <button type="button" onClick={() => { setError(false); setIsLoading(true); videoRef.current?.load(); }} className="mt-3 px-4 py-1.5 text-xs rounded-full bg-white/10 text-white/70 hover:bg-white/20 transition-colors">{language === 'ar' ? 'إعادة المحاولة' : 'Retry'}</button>
          </div>
        </div>
      ) : (
        <>
          <video ref={videoRef} poster={undefined} autoPlay={autoPlay} muted={initialMuted} loop={loop} playsInline preload="metadata" tabIndex={0}
            className={`absolute inset-0 w-full h-full object-contain cursor-pointer transition-opacity duration-500 ease-out ${showPoster ? 'opacity-0' : 'opacity-100'}`}
            onClick={togglePlay} aria-label={title}
            onKeyDown={(e) => { if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); togglePlay(); } }} />

          {isLoading && !error && (
            <div className="absolute inset-0 z-20 flex items-center justify-center bg-black/40">
              <div className="flex flex-col items-center gap-2">
                <div className="relative w-10 h-10"><div className="absolute inset-0 rounded-full border-2 border-white/10 border-t-white/40 animate-spin" /><div className="absolute inset-1 rounded-full bg-black/20" /></div>
                <span className="text-white/50 text-xs tracking-wider uppercase">{language === 'ar' ? 'جاري التحميل' : 'Loading'}</span>
              </div>
            </div>
          )}

          {isBuffering && isPlaying && (
            <div className="absolute inset-0 z-20 pointer-events-none flex items-center justify-center"><div className="w-12 h-12 flex items-center justify-center"><div className="w-2 h-2 bg-white/60 rounded-full animate-ping" /></div></div>
          )}

          {!isPlaying && !isLoading && !error && (
            <div className="absolute inset-0 z-20 flex items-center justify-center cursor-pointer group/play" onClick={togglePlay}>
              <div className="w-16 h-16 sm:w-20 sm:h-20 rounded-full bg-white/95 backdrop-blur-sm flex items-center justify-center shadow-2xl shadow-black/40 group-hover/play:scale-110 group-hover/play:bg-white transition-[transform,background-color] duration-300 ease-out will-change-transform">
                <Play className="w-7 h-7 sm:w-9 sm:h-9 text-navy ml-1" fill="currentColor" />
              </div>
            </div>
          )}

          {/* Controls overlay */}
          <div className={`absolute inset-x-0 bottom-0 z-30 bg-gradient-to-t from-black/90 via-black/50 to-transparent pt-16 pb-3 px-3 sm:px-4 transition-[opacity,transform] duration-500 ease-out ${showControls || !isPlaying ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4 pointer-events-none'}`}>
            <div className="mb-2.5 cursor-pointer group/progress" onMouseDown={handleSeekMouseDown} role="slider" aria-label={language === 'ar' ? 'شريط التقدم' : 'Seek'} aria-valuemin={0} aria-valuemax={duration} aria-valuenow={currentTime} tabIndex={0}
              onKeyDown={(e) => {
                const step = 5;
                if (!videoRef.current || !duration) return;
                switch (e.key) {
                  case 'ArrowLeft': e.preventDefault(); videoRef.current.currentTime = Math.max(0, currentTime - step); break;
                  case 'ArrowRight': e.preventDefault(); videoRef.current.currentTime = Math.min(duration, currentTime + step); break;
                  case 'Home': e.preventDefault(); videoRef.current.currentTime = 0; break;
                  case 'End': e.preventDefault(); videoRef.current.currentTime = duration; break;
                }
              }}
            >
              <div className="h-1.5 bg-white/20 rounded-full overflow-hidden group-hover/progress:h-2 transition-[height] duration-200">
                <div className="h-full bg-gradient-to-r from-secondary/90 to-secondary rounded-full relative transition-[width] duration-75 will-change-transform" style={{ width: `${progress}%` }}>
                  <div className="absolute right-0 top-1/2 -translate-y-1/2 w-3.5 h-3.5 rounded-full bg-white shadow-md opacity-60 group-hover/progress:opacity-100 transition-opacity" style={{ transform: 'translate(50%, -50%)' }} />
                </div>
              </div>
            </div>

            <div className="flex items-center justify-between gap-2">
              <div className="flex items-center gap-1 sm:gap-2">
                <button type="button" onClick={togglePlay} className="min-w-[44px] min-h-[44px] w-9 h-9 flex items-center justify-center text-white/90 hover:text-white hover:bg-white/10 rounded-lg transition-colors" aria-label={isPlaying ? (language === 'ar' ? 'إيقاف' : 'Pause') : (language === 'ar' ? 'تشغيل' : 'Play')}>
                  {isPlaying ? <Pause className="w-5 h-5" fill="currentColor" /> : <Play className="w-5 h-5" fill="currentColor" />}
                </button>

                <div className="relative flex items-center" onMouseEnter={() => setShowVolumeSlider(true)} onMouseLeave={() => setShowVolumeSlider(false)}>
                  <button type="button" onClick={toggleMute} className="min-w-[44px] min-h-[44px] w-9 h-9 flex items-center justify-center text-white/90 hover:text-white hover:bg-white/10 rounded-lg transition-colors" aria-label={isMuted ? (language === 'ar' ? 'إلغاء كتم الصوت' : 'Unmute') : (language === 'ar' ? 'كتم الصوت' : 'Mute')}>
                    {isMuted || volume === 0 ? <VolumeX className="w-5 h-5" /> : <Volume2 className="w-5 h-5" />}
                  </button>
                  <div className={`overflow-hidden transition-[width,opacity] duration-300 ease-out ${showVolumeSlider ? 'w-24 opacity-100' : 'w-0 opacity-0'}`}>
                    <div className="relative w-24 h-4 flex items-center">
                      <div className="absolute inset-x-0.5 h-0.5 rounded-full bg-white/20 pointer-events-none overflow-hidden">
                        <div className="h-full rounded-full bg-white" style={{ width: `${(isMuted ? 0 : volume) * 100}%` }} />
                      </div>
                      <input type="range" min="0" max="1" step="0.05" value={isMuted ? 0 : volume} onChange={handleVolumeChange} className="relative w-full h-4 appearance-none bg-transparent cursor-pointer z-10" aria-label={language === 'ar' ? 'مستوى الصوت' : 'Volume'} />
                    </div>
                  </div>
                </div>

                <span className="text-xs text-white/70 font-mono min-w-[80px] tabular-nums hidden sm:block">{formatTime(currentTime)} / {formatTime(duration)}</span>
                <span className="text-xs text-white/70 font-mono min-w-[80px] tabular-nums sm:hidden">{formatTime(currentTime)} / {formatTime(duration)}</span>
              </div>

              <div className="flex items-center gap-1 sm:gap-2">
                {/* Quality selector */}
                <div ref={qualityMenuRef} className="relative">
                  <button type="button" onClick={() => setShowQualityMenu(prev => !prev)} className="min-w-[44px] min-h-[44px] w-9 h-9 flex items-center justify-center text-white/70 hover:text-white hover:bg-white/10 rounded-lg transition-colors" aria-label={language === 'ar' ? 'الجودة' : 'Quality'}>
                    <Settings className="w-4 h-4" />
                  </button>
                  {showQualityMenu && (
                    <div className={`absolute bottom-full mb-2 ${rtl ? 'left-0' : 'right-0'} bg-black/90 backdrop-blur-md border border-white/10 rounded-xl overflow-hidden shadow-2xl shadow-black/50 min-w-[120px]`}>
                      {(Object.keys(QUALITY_LABELS) as Quality[]).map(q => (
                        <button key={q} type="button" onClick={() => changeQuality(q)} className={`w-full px-4 py-2 text-xs text-left hover:bg-white/10 transition-colors flex items-center gap-2 ${q === quality ? 'text-secondary font-bold' : 'text-white/70'}`}>
                          <span className={`w-1.5 h-1.5 rounded-full ${q === quality ? 'bg-secondary' : 'bg-transparent'}`} />
                          {QUALITY_LABELS[q]}
                        </button>
                      ))}
                    </div>
                  )}
                </div>

                <button type="button" onClick={handleMinimize} className="min-w-[44px] min-h-[44px] w-9 h-9 flex items-center justify-center text-white/70 hover:text-white hover:bg-white/10 rounded-lg transition-colors hidden sm:flex" aria-label={language === 'ar' ? 'تصغير' : 'Minimize'}>
                  <Minimize2 className="w-4 h-4" />
                </button>
                <button type="button" onClick={toggleFullscreen} className="min-w-[44px] min-h-[44px] w-9 h-9 flex items-center justify-center text-white/70 hover:text-white hover:bg-white/10 rounded-lg transition-colors" aria-label={isFullscreen ? (language === 'ar' ? 'إنهاء وضع ملء الشاشة' : 'Exit fullscreen') : (language === 'ar' ? 'ملء الشاشة' : 'Fullscreen')}>
                  {isFullscreen ? <Minimize className="w-[18px] h-[18px]" /> : <Maximize className="w-[18px] h-[18px]" />}
                </button>
              </div>
            </div>
          </div>

          {/* Top gradient */}
          <div className="absolute inset-x-0 top-0 z-20 h-20 bg-gradient-to-b from-black/40 to-transparent pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity duration-500 ease-out" />

          {title && (
            <div className="absolute top-3 left-3 z-20 pointer-events-none">
              <p className="text-white/80 text-xs font-medium drop-shadow-lg">{title}</p>
            </div>
          )}
        </>
      )}

      {/* Current quality badge */}
      {quality !== 'auto' && !error && (
        <div className="absolute top-3 right-3 z-20 px-2 py-0.5 rounded-md bg-black/60 backdrop-blur-sm text-white/50 text-[10px] font-mono pointer-events-none">
          {QUALITY_LABELS[quality]}
        </div>
      )}

    </div>
  );
}
