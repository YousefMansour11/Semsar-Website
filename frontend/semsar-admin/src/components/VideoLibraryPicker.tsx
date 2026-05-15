import { useState, useEffect } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Video, Loader2, Library, Check } from "lucide-react";
import { adminApi } from "@/lib/admin-api";
import { toast } from "sonner";
import { cn } from "@/lib/utils";

interface LibraryVideo {
  url: string;
  publicId: string;
  thumbnailUrl?: string;
  referenceCount: number;
  fileName?: string;
}

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSelect: (video: { publicId: string; url: string }) => void;
  projectId?: number;
}

function getPosterUrl(url: string): string {
  if (url?.includes('res.cloudinary.com')) {
    return url.replace('/upload/', '/upload/so_2.0,q_auto:good,w_320,f_jpg/').replace(/\.\w+$/, '.jpg');
  }
  return '';
}

function getFileName(v: LibraryVideo): string {
  return v.fileName || v.publicId.split('/').pop() || v.url.split('/').pop() || 'Video';
}

export function VideoLibraryPicker({ open, onOpenChange, onSelect, projectId }: Props) {
  const [videos, setVideos] = useState<LibraryVideo[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    setSelectedId(null);
    const fetchPromise = projectId
      ? adminApi.getVideoLibraryByProject(projectId)
      : adminApi.getVideoLibrary();
    fetchPromise
      .then((data) => setVideos(Array.isArray(data) ? data : []))
      .catch(() => { toast.error("Failed to load video library"); setVideos([]); })
      .finally(() => setLoading(false));
  }, [open, projectId]);

  const handleConfirm = () => {
    if (!selectedId) return;
    const video = videos.find(v => v.publicId === selectedId);
    if (!video) return;
    onSelect({ publicId: video.publicId, url: video.url });
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-card sm:max-w-3xl max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Library className="w-5 h-5" /> Video Library
          </DialogTitle>
        </DialogHeader>

        {loading ? (
          <div className="flex items-center justify-center py-16">
            <Loader2 className="w-8 h-8 animate-spin text-primary" />
          </div>
        ) : videos.length === 0 ? (
          <div className="text-center py-16 border-2 border-dashed border-border rounded-xl">
            <Video className="mx-auto h-12 w-12 text-muted-foreground/30 mb-3" />
            <p className="font-medium text-muted-foreground">No videos in library yet</p>
            <p className="text-sm text-muted-foreground/60 mt-1">Upload videos first to see them here</p>
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3">
            {videos.map((v) => {
              const poster = v.thumbnailUrl || getPosterUrl(v.url);
              return (
                <button
                  type="button"
                  key={v.publicId}
                  onClick={() => setSelectedId(v.publicId)}
                  className={cn(
                    "relative group rounded-xl border-2 overflow-hidden transition-all",
                    selectedId === v.publicId
                      ? "border-primary ring-2 ring-primary/30"
                      : "border-border hover:border-primary/50"
                  )}
                >
                  <div className="aspect-video relative">
                    {poster ? (
                      <img
                        src={poster}
                        alt=""
                        className="w-full h-full object-cover"
                        loading="lazy"
                        onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
                      />
                    ) : (
                      <div className="w-full h-full bg-accent flex items-center justify-center">
                        <Video className="w-8 h-8 text-muted-foreground/40" />
                      </div>
                    )}
                    <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent opacity-0 group-hover:opacity-100 transition-opacity" />
                    <div className="absolute bottom-1 left-1 right-1 flex items-center justify-between">
                      <span className="text-[10px] text-white/80 bg-black/50 px-1.5 py-0.5 rounded">
                        {v.referenceCount} ref{v.referenceCount !== 1 ? 's' : ''}
                      </span>
                    </div>
                    {selectedId === v.publicId && (
                      <div className="absolute top-1 right-1 w-5 h-5 bg-primary rounded-full flex items-center justify-center">
                        <Check className="w-3 h-3 text-primary-foreground" />
                      </div>
                    )}
                  </div>
                  <div className="px-2 py-1.5 text-xs text-left truncate text-muted-foreground border-t border-border">
                    {getFileName(v)}
                  </div>
                </button>
              );
            })}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button onClick={handleConfirm} disabled={!selectedId} className="bg-primary hover:bg-primary/90">
            Attach Video
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}