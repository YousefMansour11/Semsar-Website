import { useState, useRef, useEffect } from 'react';
import { optimizeCloudinaryUrl, buildSrcSet, getBlurDataUrl, type ImageTransformOptions } from '../lib/utils';

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
}

const FALLBACK = '/placeholder.svg';

const DEFAULT_SRCSET_WIDTHS = [480, 640, 828, 1080, 1200, 1600];
const DEFAULT_SIZES = '(max-width: 640px) 90vw, (max-width: 1024px) 50vw, 33vw';

export function PremiumImage({
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
  srcsetWidths = DEFAULT_SRCSET_WIDTHS,
  sizes,
  priority = false,
}: PremiumImageProps) {
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState(false);
  const imgRef = useRef<HTMLImageElement>(null);

  const imgSrc = error ? fallback : optimizeCloudinaryUrl(src, { width: width || 800, height, quality: options?.quality || 'best', crop: options?.crop || 'fill', gravity: options?.gravity || 'auto', sharpen: options?.sharpen });
  const blurSrc = !error ? getBlurDataUrl(src) : '';
  const srcSet = !error && srcsetWidths.length > 0 ? buildSrcSet(src, srcsetWidths, undefined, { gravity: options?.gravity, sharpen: options?.sharpen }) : '';
  const loadingAttr = priority ? 'eager' as const : loading;

  useEffect(() => {
    if (!priority && imgRef.current && 'loading' in HTMLImageElement.prototype) {
      return;
    }
  }, [priority]);

  const handleLoad = () => setLoaded(true);
  const handleError = () => { setError(true); setLoaded(true); };

  return (
    <div
      className={`relative overflow-hidden ${className}`}
      style={{ aspectRatio: aspectRatio || (width && height ? `${width}/${height}` : undefined) }}
    >
      {!error && (
        <img
          src={blurSrc}
          alt=""
          aria-hidden="true"
          className={`absolute inset-0 w-full h-full object-cover transition-opacity duration-500 ${loaded ? 'opacity-0' : 'opacity-100'}`}
        />
      )}
      <img
        ref={imgRef}
        src={imgSrc}
        alt={alt}
        width={width}
        height={height}
        loading={loadingAttr}
        srcSet={srcSet || undefined}
        sizes={srcSet ? (sizes || DEFAULT_SIZES) : undefined}
        onLoad={handleLoad}
        onError={handleError}
        className={`w-full h-full object-cover transition-opacity duration-500 ${loaded ? 'opacity-100' : 'opacity-0'} ${imgClassName}`}
        style={{ willChange: loaded ? 'auto' : 'opacity' }}
      />
      {!loaded && !error && (
        <div className="absolute inset-0 bg-muted/30 animate-pulse" />
      )}
    </div>
  );
}
