import { useRef, useState, useEffect } from 'react';
import { Play, Pause, X, Maximize2, Volume2, VolumeX } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import { optimizeCloudinaryVideoUrl } from '../lib/utils';
import { validateImageUrls } from '../lib/image-validator';
import type { MediaItem } from '../types/media';

interface MiniPlayerProps {
  video: MediaItem;
  currentTime: number;
  isMuted: boolean;
  onRestore: () => void;
  onClose: () => void;
}

export function MiniPlayer({ video, currentTime, isMuted: initialMuted, onRestore, onClose }: MiniPlayerProps) {
  const { language } = useLanguage();
  const videoRef = useRef<HTMLVideoElement>(null);
  const dragRef = useRef<HTMLDivElement>(null);
  const [isPlaying, setIsPlaying] = useState(true);
  const [isMuted, setIsMuted] = useState(initialMuted);
  const [progress, setProgress] = useState(0);
  const [duration, setDuration] = useState(0);
  const [position, setPosition] = useState<{x: number; y: number} | null>(null);
  const [validPoster, setValidPoster] = useState<string | undefined>(undefined);
  const dragStart = useRef({ x: 0, y: 0, left: 0, top: 0 });
  const isDragging = useRef(false);
  const userPaused = useRef(false);

  useEffect(() => {
    const el = videoRef.current;
    if (!el) return;
    el.muted = initialMuted;
    el.currentTime = currentTime;
    el.play().catch(() => {});
  }, [currentTime, initialMuted]);

  useEffect(() => {
    const el = videoRef.current;
    if (!el) return;
    const handleTimeUpdate = () => { setProgress(el.duration > 0 ? (el.currentTime / el.duration) * 100 : 0); setDuration(el.duration || 0); };
    const handlePlay = () => setIsPlaying(true);
    const handlePause = () => setIsPlaying(false);
    const handleCanPlay = () => {};
    const handleVolumeChange = () => setIsMuted(el.muted);
    const handleVisibility = () => { if (document.hidden) { el.pause(); } else if (!userPaused.current) { el.play().catch(() => {}); } };

    el.addEventListener('timeupdate', handleTimeUpdate);
    el.addEventListener('play', handlePlay);
    el.addEventListener('pause', handlePause);
    el.addEventListener('canplay', handleCanPlay);
    el.addEventListener('volumechange', handleVolumeChange);
    document.addEventListener('visibilitychange', handleVisibility);

    return () => {
      el.removeEventListener('timeupdate', handleTimeUpdate);
      el.removeEventListener('play', handlePlay);
      el.removeEventListener('pause', handlePause);
      el.removeEventListener('canplay', handleCanPlay);
      el.removeEventListener('volumechange', handleVolumeChange);
      document.removeEventListener('visibilitychange', handleVisibility);
    };
  }, []);

  const togglePlay = (e: React.MouseEvent) => { e.stopPropagation(); const el = videoRef.current; if (!el) return; if (isPlaying) { userPaused.current = true; el.pause(); } else { userPaused.current = false; el.play().catch(() => {}); } };
  const toggleMute = (e: React.MouseEvent) => { e.stopPropagation(); const el = videoRef.current; if (!el) return; el.muted = !el.muted; };

  const handleMouseDown = (e: React.MouseEvent) => {
    isDragging.current = true;
    const rect = dragRef.current?.getBoundingClientRect();
    dragStart.current = { x: e.clientX, y: e.clientY, left: rect?.left ?? 0, top: rect?.top ?? 0 };
    document.addEventListener('mousemove', handleMouseMoveRef.current);
    document.addEventListener('mouseup', handleMouseUpRef.current);
  };

  const handleMouseMove = (e: MouseEvent) => {
    if (!isDragging.current) return;
    setPosition({ x: dragStart.current.left + (e.clientX - dragStart.current.x), y: dragStart.current.top + (e.clientY - dragStart.current.y) });
  };

  const handleMouseUp = () => { isDragging.current = false; document.removeEventListener('mousemove', handleMouseMoveRef.current); document.removeEventListener('mouseup', handleMouseUpRef.current); };

const handleMouseMoveRef = useRef(handleMouseMove);
const handleMouseUpRef = useRef(handleMouseUp);
const handleTouchMoveRef = useRef(handleTouchMove);
const handleTouchEndRef = useRef(handleTouchEnd);
useEffect(() => { handleMouseMoveRef.current = handleMouseMove; });
useEffect(() => { handleMouseUpRef.current = handleMouseUp; });
useEffect(() => { handleTouchMoveRef.current = handleTouchMove; });
useEffect(() => { handleTouchEndRef.current = handleTouchEnd; });

  useEffect(() => {
    return () => {
      document.removeEventListener('mousemove', handleMouseMoveRef.current);
      document.removeEventListener('mouseup', handleMouseUpRef.current);
      document.removeEventListener('touchmove', handleTouchMoveRef.current);
      document.removeEventListener('touchend', handleTouchEndRef.current);
    };
  }, []);

  useEffect(() => {
    if (!video.thumbnailUrl) return;
    validateImageUrls([video.thumbnailUrl]).then(valid => {
      if (valid.length > 0) setValidPoster(valid[0]);
    });
  }, [video.thumbnailUrl]);

  const handleTouchStart = (e: React.TouchEvent) => {
    const t = e.touches[0];
    const rect = dragRef.current?.getBoundingClientRect();
    dragStart.current = { x: t.clientX, y: t.clientY, left: rect?.left ?? 0, top: rect?.top ?? 0 };
    document.addEventListener('touchmove', handleTouchMoveRef.current, { passive: true });
    document.addEventListener('touchend', handleTouchEndRef.current);
  };

  const handleTouchMove = (e: TouchEvent) => {
    const t = e.touches[0];
    setPosition({ x: dragStart.current.left + (t.clientX - dragStart.current.x), y: dragStart.current.top + (t.clientY - dragStart.current.y) });
  };

  const handleTouchEnd = () => { document.removeEventListener('touchmove', handleTouchMoveRef.current); document.removeEventListener('touchend', handleTouchEndRef.current); };

  const formatTime = (s: number) => { if (!s || isNaN(s)) return '0:00'; return `${Math.floor(s / 60)}:${Math.floor(s % 60).toString().padStart(2, '0')}`; };

  return (
    <div
      ref={dragRef}
      className={`fixed z-50 w-72 sm:w-96 aspect-video rounded-xl overflow-hidden shadow-2xl shadow-black/40 border border-white/20 cursor-grab active:cursor-grabbing select-none will-change-transform ${position ? '' : `bottom-4 ${language === 'ar' ? 'left-4' : 'right-4'}`} animate-miniplayer-enter`}
      style={position ? { left: `${position.x}px`, top: `${position.y}px` } : undefined}
      onMouseDown={handleMouseDown}
      onTouchStart={handleTouchStart}
    >
      <video ref={videoRef} src={optimizeCloudinaryVideoUrl(video.url)} poster={validPoster} autoPlay playsInline muted className="absolute inset-0 w-full h-full object-cover pointer-events-none" />
      <div className="absolute inset-x-0 bottom-0 z-10 h-20 bg-gradient-to-t from-black/80 via-black/40 to-transparent pointer-events-none" />

      <div className={`absolute top-2 z-20 flex gap-1.5 ${language === 'ar' ? 'left-2' : 'right-2'}`}>
        <button onClick={(e) => { e.stopPropagation(); onRestore(); }} className="w-9 h-9 rounded-full bg-black/50 backdrop-blur-sm flex items-center justify-center text-white/80 hover:text-white hover:bg-black/70 transition-colors shadow-lg will-change-transform" aria-label="Restore"><Maximize2 className="w-3.5 h-3.5" /></button>
        <button onClick={(e) => { e.stopPropagation(); onClose(); }} className="w-9 h-9 rounded-full bg-black/50 backdrop-blur-sm flex items-center justify-center text-white/80 hover:text-white hover:bg-red-500/80 transition-colors shadow-lg will-change-transform" aria-label="Close"><X className="w-3.5 h-3.5" /></button>
      </div>

      <button onClick={togglePlay} className="absolute inset-0 z-10 flex items-center justify-center bg-black/0 hover:bg-black/10 transition-colors duration-200" aria-label={isPlaying ? 'Pause' : 'Play'}>
        <div className={`w-12 h-12 rounded-full flex items-center justify-center transition-[opacity,background-color] duration-200 shadow-xl will-change-transform ${isPlaying ? 'opacity-0 hover:opacity-100 bg-white/80' : 'opacity-100 bg-white/90'}`}>
          {isPlaying ? <Pause className="w-5 h-5 text-navy" fill="currentColor" /> : <Play className="w-6 h-6 text-navy ml-0.5" fill="currentColor" />}
        </div>
      </button>

      <div className="absolute inset-x-0 bottom-0 z-20 px-2 pb-1.5 flex flex-col gap-0.5">
        <div className="w-full h-1 bg-white/20 rounded-full overflow-hidden cursor-pointer" onClick={(e) => { e.stopPropagation(); const el = videoRef.current; if (!el || !duration) return; const r = e.currentTarget.getBoundingClientRect(); el.currentTime = ((e.clientX - r.left) / r.width) * duration; }}>
          <div className="h-full bg-white rounded-full transition-[width] duration-100" style={{ width: `${progress}%` }} />
        </div>
        <div className="flex items-center justify-between">
          <span className="text-[10px] text-white/60 font-mono tabular-nums">{formatTime(videoRef.current?.currentTime ?? 0)} / {formatTime(duration)}</span>
          <button onClick={toggleMute} className="w-8 h-8 flex items-center justify-center text-white/60 hover:text-white transition-colors" aria-label={isMuted ? 'Unmute' : 'Mute'}>{isMuted ? <VolumeX className="w-3.5 h-3.5" /> : <Volume2 className="w-3.5 h-3.5" />}</button>
        </div>
      </div>


    </div>
  );
}
