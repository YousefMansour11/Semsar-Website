import { useState, useRef, useEffect, useMemo, memo } from 'react';
import { optimizeCloudinaryUrl, buildSrcSet, buildProfileSrcSet, PROFILE_SRCSET_WIDTHS, PROFILE_SIZES, type ComponentProfile, type ImageTransformOptions } from '../lib/utils';

interface PremiumImageProps {
  src: string;
  alt: string;
  width?: number;
  height?: number;
  aspectRatio?: string;
  className?: string;
  imgClassName?: string;
  loading?: 'lazy' | 'eager';
  fallback?: string;
  options?: Partial<ImageTransformOptions>;
  srcsetWidths?: number[];
  sizes?: string;
  priority?: boolean;
  profile?: ComponentProfile;
}

const FALLBACK = '/placeholder.svg';

const DEFAULT_SRCSET_WIDTHS = [480, 640, 828, 1080, 1200, 1600, 1920, 2560];
const DEFAULT_SIZES = '(max-width: 640px) 90vw, (max-width: 1024px) 50vw, 33vw';

const isPositionClass = (cn: string) => /\b(absolute|relative|fixed|sticky)\b/.test(cn);

export const PremiumImage = memo(function PremiumImage({
  src,
  alt,
  width,
  height,
  aspectRatio,
  className = '',
  imgClassName = '',
  loading = 'lazy',
  fallback = FALLBACK,
  options,
  srcsetWidths,
  sizes,
  priority = false,
  profile,
}: PremiumImageProps) {
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState(false);
  const imgRef = useRef<HTMLImageElement>(null);

  const effectiveWidths = srcsetWidths || (profile ? PROFILE_SRCSET_WIDTHS[profile] : DEFAULT_SRCSET_WIDTHS);
  const effectiveSizes = sizes || (profile ? PROFILE_SIZES[profile] : DEFAULT_SIZES);
  const imgQuality = options?.quality || 'best';

  const isCloudinary = src?.includes('res.cloudinary.com');

  const imgSrc = useMemo(
    () => error ? fallback : optimizeCloudinaryUrl(src, { width: width || 1200, height, quality: imgQuality, crop: options?.crop || 'fill', gravity: options?.gravity || 'auto', sharpen: options?.sharpen }),
    [error, fallback, src, width, height, imgQuality, options?.crop, options?.gravity, options?.sharpen]
  );

  const srcSet = useMemo(
    () => {
      if (error || effectiveWidths.length === 0) return '';
      if (profile && !optsDefined(options)) {
        return buildProfileSrcSet(src, profile);
      }
      return buildSrcSet(src, effectiveWidths, undefined, { quality: imgQuality, gravity: options?.gravity, sharpen: options?.sharpen });
    },
    [error, src, effectiveWidths, imgQuality, profile, options]
  );

  const loadingAttr = priority ? 'eager' as const : loading;

  useEffect(() => {
    setLoaded(false);
    setError(false);
  }, [src]);

  useEffect(() => {
    if (imgRef.current?.complete) {
      setLoaded(true);
    }
  }, []);

  const hasPositionClass = useMemo(() => isPositionClass(className), [className]);
  const wrapperClassName = hasPositionClass ? `overflow-hidden ${className}` : `relative overflow-hidden ${className}`;

  const containerStyle: React.CSSProperties = {};
  if (aspectRatio) {
    containerStyle.aspectRatio = aspectRatio;
  } else if (width && height && !className?.includes('absolute')) {
    containerStyle.aspectRatio = `${width}/${height}`;
  }

  return (
    <div
      className={wrapperClassName}
      style={containerStyle}
    >
      {!loaded && !error && (
        <div className="absolute inset-0 bg-muted/30" />
      )}
      <img
        ref={imgRef}
        src={imgSrc}
        alt={alt}
        width={width}
        height={height}
        loading={loadingAttr}
        fetchpriority={priority ? 'high' : undefined}
        decoding="async"
        srcSet={srcSet || undefined}
        sizes={srcSet ? effectiveSizes : undefined}
        onLoad={() => setLoaded(true)}
        onError={() => { setError(true); setLoaded(true); }}
        className={`w-full h-full object-cover ${isCloudinary && !loaded ? 'blur-xl scale-105' : ''} transition-[filter,transform,opacity] duration-700 ease-out ${loaded ? 'opacity-100' : 'opacity-0'} ${imgClassName}`}
        style={{ willChange: loaded ? 'auto' : 'opacity, filter, transform', backfaceVisibility: 'hidden' }}
      />
    </div>
  );
});

function optsDefined(opts: Partial<ImageTransformOptions> | undefined): boolean {
  return opts?.quality !== undefined || opts?.crop !== undefined || opts?.gravity !== undefined || opts?.sharpen !== undefined;
}
