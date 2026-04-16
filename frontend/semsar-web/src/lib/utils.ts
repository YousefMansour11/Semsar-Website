import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function safeSessionGet<T>(key: string): T | null {
  try {
    const v = sessionStorage.getItem(key);
    return v ? (JSON.parse(v) as T) : null;
  } catch { return null; }
}

export function safeSessionSet(key: string, value: unknown): void {
  try { sessionStorage.setItem(key, JSON.stringify(value)); } catch { /* quota or private browsing */ }
}

export function safeSessionRemove(key: string): void {
  try { sessionStorage.removeItem(key); } catch { /* ignore */ }
}

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatPrice(price: number, locale: string, currency: string = 'EGP') {
  return new Intl.NumberFormat(locale, {
    style: 'currency', currency, maximumFractionDigits: 0,
  }).format(price);
}

export interface ImageTransformOptions {
  width: number;
  height?: number;
  quality?: 'eco' | 'good' | 'best';
  crop?: 'fill' | 'scale' | 'fit' | 'pad' | 'thumb';
  gravity?: string;
  sharpen?: 'none' | 'soft' | 'medium';
  flags?: string;
}

export type ComponentProfile = 'hero' | 'card' | 'lightbox' | 'thumbnail' | 'gallery';

const COMPONENT_PROFILES: Record<ComponentProfile, {
  quality: 'eco' | 'good' | 'best';
  crop: 'fill' | 'scale' | 'fit' | 'pad' | 'thumb';
  gravity: string;
  sharpen: 'none' | 'soft' | 'medium';
  flags?: string;
}> = {
  hero: {
    quality: 'best',
    crop: 'fill',
    gravity: 'auto',
    sharpen: 'soft',
  },
  card: {
    quality: 'best',
    crop: 'fill',
    gravity: 'auto',
    sharpen: 'medium',
  },
  lightbox: {
    quality: 'best',
    crop: 'fit',
    sharpen: 'soft',
    flags: 'fl_progressive:steep',
  },
  thumbnail: {
    quality: 'good',
    crop: 'fill',
    gravity: 'auto',
    sharpen: 'medium',
  },
  gallery: {
    quality: 'best',
    crop: 'fill',
    gravity: 'auto',
    sharpen: 'soft',
  },
};

export const PROFILE_SRCSET_WIDTHS: Record<ComponentProfile, number[]> = {
  hero: [480, 768, 1080, 1600, 1920, 2560, 3200],
  card: [480, 640, 828, 1080, 1200, 1600, 1920],
  lightbox: [480, 768, 1080, 1600, 2048, 2560, 3200],
  thumbnail: [160, 320, 480, 640],
  gallery: [480, 640, 1080, 1600, 1920, 2560],
};

export const PROFILE_SIZES: Record<ComponentProfile, string> = {
  hero: '100vw',
  card: '(max-width: 640px) 90vw, (max-width: 1024px) 50vw, 33vw',
  lightbox: '(max-width: 768px) 100vw, 90vw',
  thumbnail: '160px',
  gallery: '(max-width: 480px) 100vw, (max-width: 1024px) 66vw, 66vw',
};

const CLOUDINARY_TRANSFORM_PREFIXES = [
  'c_', 'g_', 'w_', 'h_', 'q_', 'f_', 'e_', 'fl_', 'dpr_',
  'l_', 't_', 'r_', 'x_', 'y_', 'z_', 'o_', 'b_', 'bo_',
  'co_', 'cs_', 'dn_', 'pg_', 'so_', 'eo_', 'du_', 'vc_',
  'ac_', 'af_', 'ar_',
];

function isCloudinaryTransform(seg: string): boolean {
  if (seg.includes('.')) return false;
  if (seg.includes(',')) return true;
  return CLOUDINARY_TRANSFORM_PREFIXES.some(prefix => seg.startsWith(prefix));
}

function getCloudinaryPath(url: string): { base: string; publicPath: string } | null {
  if (!url || !url.includes('res.cloudinary.com')) return null;
  const uploadStr = '/upload/';
  const idx = url.indexOf(uploadStr);
  if (idx === -1) return null;

  const afterUpload = url.slice(idx + uploadStr.length);
  const segments = afterUpload.split('/');

  let pathStart = 0;
  let versionPrefix = '';

  for (let i = 0; i < segments.length; i++) {
    const seg = segments[i];

    if (/^v\d+$/.test(seg)) {
      versionPrefix = seg + '/';
      pathStart = i + 1;
      break;
    }

    if (isCloudinaryTransform(seg)) {
      pathStart = i + 1;
      continue;
    }

    break;
  }

  const publicPath = segments.slice(pathStart).join('/');
  const base = url.slice(0, idx + uploadStr.length);

  return { base, publicPath: versionPrefix + publicPath };
}

export function optimizeCloudinaryUrl(url: string, opts?: number | ImageTransformOptions): string {
  const parsed = getCloudinaryPath(url);
  if (!parsed) return url;

  let w: number | undefined;
  let h: number | undefined;
  let quality = 'best';
  let crop = 'fill';
  let gravity = 'auto';
  let sharpen: string | undefined;
  let flags: string | undefined;

  if (typeof opts === 'number') {
    w = opts;
  } else if (opts) {
    w = opts.width;
    h = opts.height;
    if (opts.quality) quality = opts.quality;
    if (opts.crop) crop = opts.crop;
    if (opts.gravity) gravity = opts.gravity;
    if (opts.sharpen === 'soft') sharpen = 'e_sharpen:80';
    else if (opts.sharpen === 'medium') sharpen = 'e_unsharp_mask:70';
    if (opts.flags) flags = opts.flags;
  }

  const GRAVITY_COMPATIBLE_CROPS = ['fill', 'thumb', 'lfill', 'fill_pad', 'auto', 'auto_pad', 'mpad', 'crop'];

  const parts: string[] = [];

  if (crop) parts.push(`c_${crop}`);
  if (gravity && GRAVITY_COMPATIBLE_CROPS.includes(crop)) parts.push(`g_${gravity}`);
  if (w) parts.push(`w_${w}`);
  if (h) parts.push(`h_${h}`);
  if (sharpen) parts.push(sharpen);
  parts.push(`q_auto:${quality}`, 'f_auto', 'dpr_auto');
  if (flags) parts.push(flags);
  else parts.push('fl_progressive');

  return `${parsed.base}${parts.join(',')}/${encodeURI(parsed.publicPath)}`;
}

export function buildSrcSet(url: string, widths: number[], _aspectRatio?: string, opts?: Partial<ImageTransformOptions>): string {
  if (!url || !url.includes('res.cloudinary.com')) return '';
  const base = { crop: 'fill' as const, gravity: 'auto' as const, quality: 'best' as const, ...opts };
  return widths
    .map(w => `${optimizeCloudinaryUrl(url, { width: w, ...base })} ${w}w`)
    .join(', ');
}

export function buildProfileSrcSet(url: string, profile: ComponentProfile, opts?: Partial<ImageTransformOptions>): string {
  const widths = PROFILE_SRCSET_WIDTHS[profile];
  const profileDefaults = COMPONENT_PROFILES[profile];
  return buildSrcSet(url, widths, undefined, { ...profileDefaults, ...opts });
}

export function optimizeCloudinaryVideoUrl(url: string, quality?: string): string {
  const parsed = getCloudinaryPath(url);
  if (!parsed) return url;

  const q = quality || 'auto';
  const qualityMap: Record<string, string> = {
    'auto': 'q_auto:good,vc_auto',
    '1080p': 'q_auto:best,w_1920,vc_auto',
    '720p': 'q_auto:good,w_1280,vc_auto',
    '480p': 'q_auto:eco,w_854,vc_auto',
  };
  const transforms = qualityMap[q] || 'q_auto:good,vc_auto';
  return `${parsed.base}${transforms}/${encodeURI(parsed.publicPath)}`;
}

export function getCloudinaryVideoPosterUrl(videoUrl: string, timestamp?: number): string {
  const parsed = getCloudinaryPath(videoUrl);
  if (!parsed) return videoUrl;
  const so = timestamp !== undefined ? `so_${timestamp}` : 'so_2.0';
  return `${parsed.base}${so},q_auto:good,w_640,f_jpg/${encodeURI(parsed.publicPath)}`;
}

export function getBlurDataUrl(url: string): string {
  const parsed = getCloudinaryPath(url);
  if (!parsed) return '';
  return `${parsed.base}w_50,c_scale,e_blur:300,q_auto:best,f_auto/${encodeURI(parsed.publicPath)}`;
}
