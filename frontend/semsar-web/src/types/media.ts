import type { VideoItem } from './property';
import { getCloudinaryVideoPosterUrl } from '../lib/utils';

export type MediaType = 'image' | 'video';

export type MediaPriority = 1 | 2 | 3 | 4 | 5 | 6;

export const MEDIA_PRIORITY = {
  hero: 1 as MediaPriority,
  featuredVideo: 2 as MediaPriority,
  featuredImage: 3 as MediaPriority,
  galleryVideo: 4 as MediaPriority,
  galleryImage: 5 as MediaPriority,
  floorplan: 6 as MediaPriority,
};

export interface MediaItem {
  id: string;
  type: MediaType;
  url: string;
  thumbnailUrl?: string;
  poster?: string;
  duration?: number;
  videoId?: number;
  publicId?: string;
  sortOrder?: number;
  priority?: MediaPriority;
}

export interface MediaCollection {
  items: MediaItem[];
  heroIndex: number;
}

export function sortMediaItems(items: MediaItem[]): MediaItem[] {
  return [...items].sort((a, b) => {
    const pa = a.priority ?? MEDIA_PRIORITY.galleryImage;
    const pb = b.priority ?? MEDIA_PRIORITY.galleryImage;
    if (pa !== pb) return pa - pb;
    return (a.sortOrder ?? 0) - (b.sortOrder ?? 0);
  });
}

export function buildMediaItems(heroImage: string, galleryImages: string[], videos?: VideoItem[]): MediaCollection {
  const items: MediaItem[] = [];

  if (videos && videos.length > 0) {
    for (const v of videos) {
      const poster = v.thumbnailUrl || getCloudinaryVideoPosterUrl(v.url);
      items.push({
        id: `video-${v.id}`,
        type: 'video',
        url: v.url,
        thumbnailUrl: poster,
        poster,
        videoId: v.id,
        publicId: v.publicId,
        sortOrder: v.sortOrder ?? 0,
        priority: MEDIA_PRIORITY.featuredVideo,
      });
    }
  }

  items.push({ id: 'hero', type: 'image', url: heroImage, priority: MEDIA_PRIORITY.hero, sortOrder: -1 });

  for (const img of galleryImages) {
    if (img === heroImage) continue;
    items.push({ id: `img-${items.length}`, type: 'image', url: img, priority: MEDIA_PRIORITY.galleryImage, sortOrder: items.length });
  }

  return {
    items,
    heroIndex: (videos && videos.length > 0) ? 0 : items.findIndex(i => i.id === 'hero'),
  };
}

export function buildProjectMediaItems(images: string[], videos?: VideoItem[]): MediaCollection {
  const items: MediaItem[] = [];
  const heroImage = images[0] || '';

  if (videos && videos.length > 0) {
    for (const v of videos) {
      const poster = v.thumbnailUrl || getCloudinaryVideoPosterUrl(v.url);
      items.push({
        id: `video-${v.id}`,
        type: 'video',
        url: v.url,
        thumbnailUrl: poster,
        poster,
        videoId: v.id,
        publicId: v.publicId,
        sortOrder: v.sortOrder ?? 0,
        priority: MEDIA_PRIORITY.featuredVideo,
      });
    }
  }

  if (heroImage) {
    items.push({ id: 'hero', type: 'image', url: heroImage, priority: MEDIA_PRIORITY.hero, sortOrder: -1 });
  }

  for (const img of images) {
    if (img === heroImage) continue;
    items.push({ id: `img-${items.length}`, type: 'image', url: img, priority: MEDIA_PRIORITY.galleryImage, sortOrder: items.length });
  }

  return {
    items,
    heroIndex: (videos && videos.length > 0) ? 0 : items.findIndex(i => i.id === 'hero'),
  };
}
