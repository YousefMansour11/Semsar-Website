import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function autoSlug(s: string): string {
  return s.toLowerCase().replace(/[^a-z0-9\s-]/g, "").replace(/\s+/g, "-").replace(/-+/g, "-").replace(/^-|-$/g, "");
}

export function safeParseJson<T>(json: string | null | undefined, fallback: T): T {
  if (!json) return fallback;
  try { return JSON.parse(json) as T; } catch { return fallback; }
}

export function safeHostname(url: string | null | undefined): string {
  if (!url) return '';
  try { return new URL(url).hostname; } catch { return url || ''; }
}

export function optimizeCloudinaryUrl(url: string, width?: number): string {
  if (!url || !url.includes('res.cloudinary.com')) return url;
  const uploadStr = '/upload/';
  const idx = url.indexOf(uploadStr);
  if (idx === -1) return url;
  const afterUpload = url.slice(idx + uploadStr.length);
  const slashIdx = afterUpload.indexOf('/');
  if (slashIdx === -1) return url;
  const publicPart = afterUpload.slice(slashIdx);
  const existingTransforms = afterUpload.slice(0, slashIdx);
  let transforms = 'f_auto,q_auto';
  if (width) transforms += `,w_${width}`;
  if (existingTransforms.includes('f_auto')) {
    if (!width || existingTransforms.includes(`w_${width}`)) return url;
  }
  return url.slice(0, idx + uploadStr.length) + transforms + publicPart;
}
