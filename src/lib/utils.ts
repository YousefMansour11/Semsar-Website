import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

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
  crop?: 'fill' | 'scale' | 'fit' | 'pad';
  gravity?: string;
  sharpen?: 'none' | 'soft' | 'medium';
}

export function optimizeCloudinaryUrl(url: string, opts?: number | ImageTransformOptions): string {
  if (!url || !url.includes('res.cloudinary.com')) return url;

  const uploadStr = '/upload/';
  const idx = url.indexOf(uploadStr);
  if (idx === -1) return url;

  const afterUpload = url.slice(idx + uploadStr.length);
  const slashIdx = afterUpload.indexOf('/');
  if (slashIdx === -1) return url;

  const publicPart = afterUpload.slice(slashIdx);

  let w: number | undefined;
  let h: number | undefined;
  let quality = 'good';
  let crop = 'fill';
  let gravity = 'auto';
  let sharpen: string | undefined;

  if (typeof opts === 'number') {
    w = opts;
  } else if (opts) {
    w = opts.width;
    h = opts.height;
    if (opts.quality) quality = opts.quality;
    if (opts.crop) crop = opts.crop;
    if (opts.gravity) gravity = opts.gravity;
    if (opts.sharpen === 'soft') sharpen = 'e_sharpen:100';
    else if (opts.sharpen === 'medium') sharpen = 'e_unsharp_mask:100';
  }

  const parts: string[] = [];

  if (crop) parts.push(`c_${crop}`);
  if (gravity) parts.push(`g_${gravity}`);
  if (w) parts.push(`w_${w}`);
  if (h) parts.push(`h_${h}`);
  if (sharpen) parts.push(sharpen);
  parts.push(`q_auto:${quality}`, 'f_auto', 'fl_progressive', 'dpr_auto');

  return `${url.slice(0, idx + uploadStr.length)}${parts.join(',')}${publicPart}`;
}

export function buildSrcSet(url: string, widths: number[], _aspectRatio?: string, opts?: Partial<ImageTransformOptions>): string {
  if (!url || !url.includes('res.cloudinary.com')) return '';
  return widths
    .map(w => `${optimizeCloudinaryUrl(url, { width: w, crop: 'fill', gravity: 'auto', quality: 'best', ...opts })} ${w}w`)
    .join(', ');
}

export function getBlurDataUrl(url: string): string {
  if (!url || !url.includes('res.cloudinary.com')) return '';
  const idx = url.indexOf('/upload/');
  if (idx === -1) return '';
  const afterUpload = url.slice(idx + '/upload/'.length);
  const slashIdx = afterUpload.indexOf('/');
  if (slashIdx === -1) return '';
  const publicPart = afterUpload.slice(slashIdx);
  return `${url.slice(0, idx)}/upload/e_blur:200,q_auto:low,w_20${publicPart}`;
}
