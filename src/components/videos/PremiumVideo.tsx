import { useRef, useState, useEffect, useCallback } from 'react';
import { Play, Pause, Volume2, VolumeX, Maximize, Minimize, Loader2, PictureInPicture2 } from 'lucide-react';
import { useLanguage } from '../../i18n/LanguageContext';
import { optimizeCloudinaryVideoUrl } from '../../lib/utils';

interface PremiumVideoProps {
  src: string;
  poster?: string;
  title?: string;
  className?: string;
  aspectRatio?: string;
  autoPlay?: boolean;
  muted?: boolean;
  loop?: boolean;
  onPlay?: () => void;
  onPause?: () => void;
  onEnded?: () => void;
}

export function PremiumVideo({
  src,
  poster,
  title,
  className = '',
  aspectRatio = '16/9',
  autoPlay = false,
  muted: initialMuted = false,
  loop = false,
  onPlay,
  onPause,
  onEnded,
}: PremiumVideoProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const hideTimer = useRef<ReturnType<typeof setTimeout>>();
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
  const [isPosterLoaded, setIsPosterLoaded] = useState(false);
  const [showPoster, setShowPoster] = useState(true);
  const [showVolumeSlider, setShowVolumeSlider] = useState(false);
  const { language } = useLanguage();
  const rtl = language === 'ar';

  const videoSrc = optimizeCloudinaryVideoUrl(src);

  useEffect(() => {
    setError(false);
    setIsLoading(true);
    setIsBuffering(false);
    setIsPlaying(autoPlay);
    setCurrentTime(0);
    setDuration(0);
    setShowPoster(true);
    setIsPosterLoaded(false);
  }, [src, autoPlay]);

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;

    const onTimeUpdate = () => setCurrentTime(video.currentTime);
    const onDurationChange = () => { setDuration(video.duration || 0); setIsLoading(false); };
    const onPlay = () => { setIsPlaying(true); setShowPoster(false); onPlay?.(); };
    const onPause = () => setIsPlaying(false);
    const onEnded = () => { setIsPlaying(false); onEnded?.(); };
    const onError = () => { setError(true); setIsLoading(false); setIsBuffering(false); };
    const onCanPlay = () => { setIsLoading(false); setIsBuffering(false); };
    const onWaiting = () => setIsBuffering(true);
    const onPlaying = () => { setIsLoading(false); setIsBuffering(false); };
    const onVolumeChange = () => { setVolume(video.volume); setIsMuted(video.muted); };
    const onFullscreenChange = () => setIsFullscreen(!!document.fullscreenElement);

    video.addEventListener('timeupdate', onTimeUpdate);
    video.addEventListener('durationchange', onDurationChange);
    video.addEventListener('play', onPlay);
    video.addEventListener('pause', onPause);
    video.addEventListener('ended', onEnded);
    video.addEventListener('error', onError);
    video.addEventListener('canplay', onCanPlay);
    video.addEventListener('waiting', onWaiting);
    video.addEventListener('playing', onPlaying);
    video.addEventListener('volumechange', onVolumeChange);
    document.addEventListener('fullscreenchange', onFullscreenChange);

    return () => {
      video.removeEventListener('timeupdate', onTimeUpdate);
      video.removeEventListener('durationchange', onDurationChange);
      video.removeEventListener('play', onPlay);
      video.removeEventListener('pause', onPause);
      video.removeEventListener('ended', onEnded);
      video.removeEventListener('error', onError);
      video.removeEventListener('canplay', onCanPlay);
      video.removeEventListener('waiting', onWaiting);
      video.removeEventListener('playing', onPlaying);
      video.removeEventListener('volumechange', onVolumeChange);
      document.removeEventListener('fullscreenchange', onFullscreenChange);
    };
  }, [onPlay, onPause, onEnded, src]);

  useEffect(() => {
    if (!videoRef.current) return;
    videoRef.current.muted = isMuted;
  }, [isMuted]);

  const togglePlay = useCallback(() => {
    if (!videoRef.current) return;
    if (isPlaying) {
      videoRef.current.pause();
    } else {
      videoRef.current.play().catch(() => setError(true));
    }
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

  const seek = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!videoRef.current || !duration) return;
    const rect = e.currentTarget.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const ratio = Math.max(0, Math.min(1, x / rect.width));
    videoRef.current.currentTime = ratio * duration;
  };

  const skip = (seconds: number) => {
    if (!videoRef.current) return;
    videoRef.current.currentTime = Math.max(0, Math.min(videoRef.current.duration || 0, videoRef.current.currentTime + seconds));
  };

  const toggleFullscreen = useCallback(() => {
    if (!containerRef.current) return;
    if (!document.fullscreenElement) {
      containerRef.current.requestFullscreen?.();
    } else {
      document.exitFullscreen?.();
    }
  }, []);

  const togglePiP = async () => {
    const video = videoRef.current;
    if (!video) return;
    try {
      if (document.pictureInPictureElement) {
        await document.exitPictureInPicture();
      } else {
        await video.requestPictureInPicture();
      }
    } catch { /* PiP not supported */ }
  };

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!containerRef.current?.contains(document.activeElement) && !document.fullscreenElement) return;
      switch (e.key) {
        case ' ':
        case 'k':
        case 'K':
          e.preventDefault();
          togglePlay();
          break;
        case 'j':
        case 'J':
          e.preventDefault();
          skip(-10);
          break;
        case 'l':
        case 'L':
          e.preventDefault();
          skip(10);
          break;
        case 'f':
        case 'F':
          e.preventDefault();
          toggleFullscreen();
          break;
        case 'm':
        case 'M':
          e.preventDefault();
          toggleMute();
          break;
        case 'ArrowLeft':
          if (!rtl) { e.preventDefault(); skip(-5); }
          break;
        case 'ArrowRight':
          if (!rtl) { e.preventDefault(); skip(5); }
          break;
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [togglePlay, toggleFullscreen, toggleMute, rtl]);

  const handleDoubleClick = () => toggleFullscreen();

  const handleMouseMove = () => {
    setShowControls(true);
    clearTimeout(hideTimer.current);
    hideTimer.current = setTimeout(() => {
      if (isPlaying && !showVolumeSlider) setShowControls(false);
    }, 3000);
  };

  const formatTime = (seconds: number) => {
    if (!seconds || isNaN(seconds)) return '0:00';
    const m = Math.floor(seconds / 60);
    const s = Math.floor(seconds % 60);
    return `${m}:${s.toString().padStart(2, '0')}`;
  };

  const progress = duration > 0 ? (currentTime / duration) * 100 : 0;
  const isLive = duration === 0 && currentTime === 0 && !isLoading;

  return (
    <div
      ref={containerRef}
      className={`relative overflow-hidden bg-black rounded-2xl shadow-2xl shadow-black/30 group animate-fadeIn ${className}`}
      style={{ aspectRatio }}
      onMouseMove={handleMouseMove}
      onMouseLeave={() => isPlaying && setShowControls(false)}
      onDoubleClick={handleDoubleClick}
      dir="ltr"
      role="region"
      aria-label={title || 'Video player'}
    >
      {/* Poster Image with blur-up */}
      {poster && showPoster && (
        <div
          className={`absolute inset-0 z-10 transition-opacity duration-700 ${isPosterLoaded ? 'opacity-100' : 'opacity-0'}`}
          onTransitionEnd={() => !isPosterLoaded && setIsPosterLoaded(true)}
        >
          <img
            src={poster}
            alt=""
            className="absolute inset-0 w-full h-full object-cover"
            onLoad={() => setIsPosterLoaded(true)}
          />
          <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />
        </div>
      )}

      {error ? (
        <div className="absolute inset-0 z-20 flex items-center justify-center bg-gradient-to-br from-gray-900 to-black">
          <div className="text-center px-6">
            <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-white/10 flex items-center justify-center">
              <VolumeX className="w-7 h-7 text-white/60" />
            </div>
            <p className="text-white/80 text-sm font-medium">{language === 'ar' ? 'تعذر تحميل الفيديو' : 'Video failed to load'}</p>
            <p className="text-white/40 text-xs mt-1">{title}</p>
            <button
              onClick={() => { setError(false); setIsLoading(true); videoRef.current?.load(); }}
              className="mt-3 px-4 py-1.5 text-xs rounded-full bg-white/10 text-white/70 hover:bg-white/20 transition-colors"
            >
              {language === 'ar' ? 'إعادة المحاولة' : 'Retry'}
            </button>
          </div>
        </div>
      ) : (
        <>
          <video
            ref={videoRef}
            src={videoSrc}
            poster={undefined}
            autoPlay={autoPlay}
            muted={initialMuted}
            loop={loop}
            playsInline
            preload="metadata"
            className={`absolute inset-0 w-full h-full object-contain cursor-pointer transition-opacity duration-500 ${showPoster ? 'opacity-0' : 'opacity-100'}`}
            onClick={togglePlay}
            aria-label={title}
          />

          {/* Loading shimmer */}
          {isLoading && !error && (
            <div className="absolute inset-0 z-20 flex items-center justify-center bg-black/40">
              <div className="flex flex-col items-center gap-2">
                <div className="relative w-10 h-10">
                  <div className="absolute inset-0 rounded-full border-2 border-white/10 border-t-white/40 animate-spin" />
                  <div className="absolute inset-1 rounded-full bg-black/20" />
                </div>
                <span className="text-white/50 text-xs tracking-wider uppercase">{language === 'ar' ? 'جاري التحميل' : 'Loading'}</span>
              </div>
            </div>
          )}

          {/* Buffering indicator */}
          {isBuffering && isPlaying && (
            <div className="absolute inset-0 z-20 pointer-events-none flex items-center justify-center">
              <div className="w-12 h-12 flex items-center justify-center">
                <div className="w-2 h-2 bg-white/60 rounded-full animate-ping" />
              </div>
            </div>
          )}

          {/* Big play button overlay */}
          {!isPlaying && !isLoading && !error && (
            <div className="absolute inset-0 z-20 flex items-center justify-center cursor-pointer group/play" onClick={togglePlay}>
              <div className="w-16 h-16 sm:w-20 sm:h-20 rounded-full bg-white/95 backdrop-blur-sm flex items-center justify-center shadow-2xl shadow-black/40 group-hover/play:scale-110 group-hover/play:bg-white transition-all duration-300">
                <Play className="w-7 h-7 sm:w-9 sm:h-9 text-navy ml-1" fill="currentColor" />
              </div>
            </div>
          )}

          {/* Controls overlay */}
          <div className={`absolute inset-x-0 bottom-0 z-30 bg-gradient-to-t from-black/90 via-black/50 to-transparent pt-16 pb-3 px-3 sm:px-4 transition-all duration-500 ${showControls || !isPlaying ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4 pointer-events-none'}`}>
            {/* Progress bar */}
            <div className="mb-2.5 cursor-pointer group/progress" onClick={seek}>
              <div className="h-1.5 bg-white/20 rounded-full overflow-hidden group-hover/progress:h-2 transition-all duration-200">
                <div className="h-full bg-gradient-to-r from-secondary/90 to-secondary rounded-full relative transition-all duration-100" style={{ width: `${progress}%` }}>
                  <div className="absolute right-0 top-1/2 -translate-y-1/2 w-3.5 h-3.5 rounded-full bg-white shadow-md opacity-0 group-hover/progress:opacity-100 transition-opacity scale-0 group-hover/progress:scale-100" style={{ transform: 'translate(50%, -50%)' }} />
                </div>
              </div>
            </div>

            <div className="flex items-center justify-between gap-2">
              <div className="flex items-center gap-1 sm:gap-2">
                <button onClick={togglePlay} className="w-9 h-9 flex items-center justify-center text-white/90 hover:text-white hover:bg-white/10 rounded-lg transition-colors" aria-label={isPlaying ? 'Pause' : 'Play'}>
                  {isPlaying ? <Pause className="w-5 h-5" fill="currentColor" /> : <Play className="w-5 h-5" fill="currentColor" />}
                </button>

                <div className="relative flex items-center" onMouseEnter={() => setShowVolumeSlider(true)} onMouseLeave={() => setShowVolumeSlider(false)}>
                  <button onClick={toggleMute} className="w-9 h-9 flex items-center justify-center text-white/90 hover:text-white hover:bg-white/10 rounded-lg transition-colors" aria-label={isMuted ? 'Unmute' : 'Mute'}>
                    {isMuted || volume === 0 ? <VolumeX className="w-5 h-5" /> : <Volume2 className="w-5 h-5" />}
                  </button>
                  <div className={`overflow-hidden transition-all duration-300 ${showVolumeSlider ? 'w-20 opacity-100' : 'w-0 opacity-0'}`}>
                    <input
                      type="range"
                      min="0"
                      max="1"
                      step="0.05"
                      value={isMuted ? 0 : volume}
                      onChange={handleVolumeChange}
                      className="w-20 h-1 appearance-none bg-white/20 rounded-full cursor-pointer accent-white [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:w-3 [&::-webkit-slider-thumb]:h-3 [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-white [&::-webkit-slider-thumb]:shadow-md"
                      aria-label="Volume"
                    />
                  </div>
                </div>

                <span className="text-xs text-white/70 font-mono min-w-[80px] tabular-nums hidden sm:block">
                  {formatTime(currentTime)} / {formatTime(duration)}
                </span>
                <span className="text-xs text-white/70 font-mono min-w-[80px] tabular-nums sm:hidden">
                  {formatTime(currentTime)} / {formatTime(duration)}
                </span>
              </div>

              <div className="flex items-center gap-1 sm:gap-2">
                <button onClick={togglePiP} className="w-9 h-9 flex items-center justify-center text-white/70 hover:text-white hover:bg-white/10 rounded-lg transition-colors hidden sm:flex" aria-label="Picture in Picture">
                  <PictureInPicture2 className="w-4 h-4" />
                </button>
                <button onClick={toggleFullscreen} className="w-9 h-9 flex items-center justify-center text-white/70 hover:text-white hover:bg-white/10 rounded-lg transition-colors" aria-label={isFullscreen ? 'Exit fullscreen' : 'Fullscreen'}>
                  {isFullscreen ? <Minimize className="w-[18px] h-[18px]" /> : <Maximize className="w-[18px] h-[18px]" />}
                </button>
              </div>
            </div>
          </div>

          {/* Top gradient for text readability */}
          <div className="absolute inset-x-0 top-0 z-20 h-20 bg-gradient-to-b from-black/40 to-transparent pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity duration-500" />

          {/* Title in top-left */}
          {title && (
            <div className="absolute top-3 left-3 z-20 pointer-events-none">
              <p className="text-white/80 text-xs font-medium drop-shadow-lg">{title}</p>
            </div>
          )}
        </>
      )}

      {/* Entrance animation */}
      <style>{`
        @keyframes fadeIn {
          from { opacity: 0; transform: scale(0.97); }
          to { opacity: 1; transform: scale(1); }
        }
        .animate-fadeIn {
          animation: fadeIn 0.4s ease-out;
        }
      `}</style>
    </div>
  );
}
