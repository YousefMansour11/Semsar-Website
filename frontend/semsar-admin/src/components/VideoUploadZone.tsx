import { useState, useRef, useCallback } from "react";
import { Button } from "@/components/ui/button";
import { Video, X, Upload, Loader2, CheckCircle2, Library } from "lucide-react";
import { adminApi } from "@/lib/admin-api";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import { VideoLibraryPicker } from "./VideoLibraryPicker";


export interface VideoItem {
  id: number;
  url: string;
  publicId: string;
}

interface Props {
  entityType: "properties" | "projects" | "units";
  entityId: number | null;
  existingVideos?: VideoItem[];
  onVideoAdded?: (video: VideoItem) => void;
  onVideoRemoved?: (videoId: number) => void;
  projectId?: number;
}

const ACCEPTED_TYPES = ".mp4,.mov,.webm";
const MAX_SIZE = 150 * 1024 * 1024;
const MAX_DURATION = 180;

function getVideoDuration(file: File): Promise<number> {
  return new Promise((resolve) => {
    const video = document.createElement("video");
    video.preload = "metadata";
    video.onloadedmetadata = () => {
      URL.revokeObjectURL(video.src);
      resolve(video.duration);
    };
    video.onerror = () => resolve(0);
    video.src = URL.createObjectURL(file);
  });
}

export function VideoUploadZone({ entityType, entityId, existingVideos = [], onVideoAdded, onVideoRemoved, projectId }: Props) {
  const [videos, setVideos] = useState<VideoItem[]>(existingVideos);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<Record<string, number>>({});
  const [dragOver, setDragOver] = useState(false);
  const [showLibrary, setShowLibrary] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);
  const uploadedFilenames = useRef<Set<string>>(new Set());
  const uploadCount = useRef(0);

  const validateFile = (file: File): string | null => {
    const ext = "." + file.name.split(".").pop()?.toLowerCase();
    if (!ACCEPTED_TYPES.includes(ext)) return "Only MP4, MOV, and WebM files are allowed";
    if (file.size > MAX_SIZE) return "File exceeds maximum allowed size of 150MB";
    if (file.size === 0) return "File is empty";
    return null;
  };

  const uploadFile = useCallback(async (file: File) => {
    const error = validateFile(file);
    if (error) {
      toast.error(error);
      return;
    }

    const duration = await getVideoDuration(file);
    if (duration > MAX_DURATION) {
      toast.error(`Video duration (${Math.round(duration)}s) exceeds maximum of ${MAX_DURATION / 60} minutes`);
      return;
    }
    if (duration > 120) {
      toast.warning(`Video is ${Math.round(duration)}s long. Consider keeping videos under 2 minutes for better engagement.`);
    }

    if (!entityId) {
      toast.error("Save the entity first before uploading videos");
      return;
    }

    if (uploadedFilenames.current.has(file.name)) {
      toast.error(`Video "${file.name}" was already uploaded in this session`);
      return;
    }

    uploadCount.current += 1;
    setIsUploading(true);
    const progressKey = file.name + Date.now();
    setUploadProgress((prev) => ({ ...prev, [progressKey]: 0 }));

    try {
      // Track real upload progress via XMLHttpRequest
      const progress = (pct: number) => {
        setUploadProgress((prev) => ({ ...prev, [progressKey]: Math.min(pct, 99) }));
      };
      progress(0);

      // Step 1: Compute content hash for dedup
      progress(2);
      const fileHash = await computeSHA256(file);
      progress(4);

      // Step 2: Get a signed upload token from backend with content-based publicId
      const publicId = `semsar/library/${fileHash}`;
      const sig = await adminApi.getVideoUploadSignature(`${entityType}/${entityId}`, publicId);
      progress(6);

      // Step 3: Upload directly to Cloudinary from the browser (overwrite=false dedup)
      const cloudinaryResult = await uploadToCloudinary(file, sig, progress);
      progress(95);

      // Step 3: Confirm with backend to attach the video to the entity
      const thumbnailUrl = cloudinaryResult.secure_url.replace(
        '/upload/',
        '/upload/so_2.0,q_auto:good,w_640,f_jpg/'
      );

      type ConfirmResponse = { files?: Array<{ id: number; url: string; publicId?: string }> };
      let confirmResult: ConfirmResponse | undefined;
      const confirmData = { url: cloudinaryResult.secure_url, publicId: cloudinaryResult.public_id, thumbnailUrl, fileName: file.name };

      if (entityType === "properties") {
        confirmResult = await adminApi.confirmPropertyVideo(entityId, confirmData);
      } else if (entityType === "projects") {
        confirmResult = await adminApi.confirmProjectVideo(entityId, confirmData);
      } else {
        confirmResult = await adminApi.confirmUnitVideo(entityId, confirmData);
      }
      progress(100);

      const video = confirmResult?.files?.[0];
      if (video?.id && video?.url) {
        const newVideo: VideoItem = { id: video.id, url: video.url, publicId: video.publicId ?? "" };
        uploadedFilenames.current.add(file.name);
        setVideos((prev) => [...prev, newVideo]);
        onVideoAdded?.(newVideo);
        toast.success("Video uploaded");
      } else {
        toast.error("Upload succeeded but no video data returned");
      }
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to upload video");
    } finally {
      uploadCount.current -= 1;
      setIsUploading(uploadCount.current > 0);
      setUploadProgress((prev) => {
        const { [progressKey]: _, ...rest } = prev;
        return rest;
      });
    }
  }, [entityId, entityType, onVideoAdded]);

  async function computeSHA256(file: File): Promise<string> {
    if (!crypto.subtle) {
      return `${file.name}_${file.size}_${Date.now()}`;
    }
    const buffer = await file.arrayBuffer();
    const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
  }

  async function uploadToCloudinary(
    file: File,
    sig: { signature: string; timestamp: number; apiKey: string; cloudName: string; folder: string; publicId?: string | null; overwrite?: boolean | null },
    onProgress: (pct: number) => void
  ): Promise<{ secure_url: string; public_id: string; [key: string]: unknown }> {
    const url = `https://api.cloudinary.com/v1_1/${sig.cloudName}/video/upload`;

    return new Promise((resolve, reject) => {
      const fd = new FormData();
      fd.append('file', file);
      fd.append('api_key', sig.apiKey);
      fd.append('timestamp', String(sig.timestamp));
      fd.append('folder', sig.folder);
      fd.append('signature', sig.signature);
      if (sig.publicId) fd.append('public_id', sig.publicId);
      if (sig.overwrite === false) fd.append('overwrite', 'false');

      const xhr = new XMLHttpRequest();
      xhr.open('POST', url, true);

      xhr.upload.onprogress = (e) => {
        if (e.lengthComputable) {
          onProgress(10 + Math.round((e.loaded / e.total) * 80));
        }
      };

      xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          try {
            resolve(JSON.parse(xhr.responseText));
          } catch {
            reject(new Error('Invalid response from Cloudinary'));
          }
        } else {
          try {
            const err = JSON.parse(xhr.responseText);
            reject(new Error(err?.error?.message || `Cloudinary upload failed (${xhr.status})`));
          } catch {
            reject(new Error(`Cloudinary upload failed (${xhr.status})`));
          }
        }
      };

      xhr.onerror = () => reject(new Error('Network error during Cloudinary upload'));
      xhr.ontimeout = () => reject(new Error('Cloudinary upload timed out'));
      xhr.timeout = 300000;

      xhr.send(fd);
    });
  }

  const handleLibrarySelect = async (libraryVideo: { publicId: string; url: string }) => {
    if (!entityId) {
      toast.error("Save the entity first before attaching videos");
      return;
    }
    try {
      type AttachResponse = { files?: Array<{ id: number; url: string; publicId?: string }> };
      let result: AttachResponse | undefined;
      if (entityType === "properties") {
        result = await adminApi.attachLibraryVideoToProperty(entityId, libraryVideo.publicId) as AttachResponse | undefined;
      } else if (entityType === "projects") {
        result = await adminApi.attachLibraryVideoToProject(entityId, libraryVideo.publicId) as AttachResponse | undefined;
      } else {
        result = await adminApi.attachLibraryVideoToUnit(entityId, libraryVideo.publicId) as AttachResponse | undefined;
      }
      const video = result?.files?.[0];
      if (video?.id && video?.url) {
        const newVideo: VideoItem = { id: video.id, url: video.url, publicId: video.publicId ?? "" };
        setVideos((prev) => [...prev, newVideo]);
        onVideoAdded?.(newVideo);
        toast.success("Video attached from library");
      }
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to attach video");
    }
  };

  const handleFileSelect = (files: FileList | null) => {
    if (!files) return;
    for (const file of Array.from(files)) {
      uploadFile(file);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    if (e.dataTransfer.files) {
      for (const file of Array.from(e.dataTransfer.files)) {
        if (file.type.startsWith("video/")) {
          uploadFile(file);
        }
      }
    }
  };

  const handleDelete = async (videoId: number) => {
    if (!entityId) return;
    try {
      let deleted = false;
      if (entityType === "properties") {
        await adminApi.deletePropertyVideo(entityId, videoId);
        deleted = true;
      } else if (entityType === "projects") {
        await adminApi.deleteProjectVideo(entityId, videoId);
        deleted = true;
      } else {
        await adminApi.deleteUnitVideo(entityId, videoId);
        deleted = true;
      }
      if (deleted) {
        setVideos((prev) => prev.filter((v) => v.id !== videoId));
        onVideoRemoved?.(videoId);
        toast.success("Video removed");
      }
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Failed to delete video");
    }
  };

  return (
    <div className="space-y-3">
      <p className="text-sm font-medium">Videos</p>

      {videos.length > 0 && (
        <div className="space-y-2">
          {videos.map((video) => {
            const posterUrl = video.url?.includes('res.cloudinary.com')
              ? video.url.replace('/upload/', '/upload/so_2.0,q_auto:good,w_160,f_jpg/').replace(/\.\w+$/, '.jpg')
              : '';
            return (
              <div key={video.id} className="flex items-center gap-3 p-2 rounded-lg border border-border bg-accent/30">
                <div className="relative w-16 h-10 shrink-0 rounded overflow-hidden bg-muted">
                  {posterUrl ? (
                    <img src={posterUrl} alt="" className="w-full h-full object-cover" loading="lazy"
                      onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }} />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center"><Video className="w-4 h-4 text-muted-foreground/40" /></div>
                  )}
                </div>
                <a href={video.url} target="_blank" rel="noopener noreferrer" className="flex-1 text-sm truncate text-primary hover:underline min-w-0">
                  {video.url.split("/").pop() || "Video"}
                </a>
                <Button variant="ghost" size="icon" className="h-7 w-7 text-destructive shrink-0" onClick={() => handleDelete(video.id)} aria-label="Delete video">
                  <X className="w-4 h-4" />
                </Button>
              </div>
            );
          })}
        </div>
      )}

      <div
        onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        onClick={() => fileRef.current?.click()}
        className={cn(
          "relative w-full h-28 rounded-xl border-2 border-dashed flex flex-col items-center justify-center text-muted-foreground hover:text-primary transition-colors cursor-pointer",
          dragOver ? "border-primary bg-primary/5" : "border-border",
          isUploading ? "pointer-events-none opacity-60" : ""
        )}
      >
        {isUploading ? (
          <>
            <Loader2 className="w-6 h-6 animate-spin" />
            <span className="text-xs mt-1">Uploading...</span>
          </>
        ) : (
          <>
            <Upload className="w-6 h-6" />
            <span className="text-xs mt-1">Drop video or click to upload</span>
            <span className="text-[10px] text-muted-foreground/60 mt-0.5">MP4, MOV, WebM &middot; Max 150MB &middot; Max 2 min</span>
          </>
        )}
      </div>

      <div className="flex items-center gap-2">
        <div className="flex-1">
          <input
            ref={fileRef}
            id="video-upload-input"
            type="file"
            accept={ACCEPTED_TYPES}
            className="hidden"
            onChange={(e) => { handleFileSelect(e.target.files); if (e.target) e.target.value = ""; }}
          />
        </div>
        <Button type="button" variant="outline" size="sm" onClick={() => setShowLibrary(true)} className="gap-1.5">
          <Library className="w-4 h-4" /> From Library
        </Button>
      </div>

      {Object.keys(uploadProgress).length > 0 && (
        <div className="space-y-1">
          {Object.entries(uploadProgress).map(([key, progress]) => (
            <div key={key} className="flex items-center gap-2 text-xs text-muted-foreground">
              {progress === 100 ? (
                <CheckCircle2 className="w-3.5 h-3.5 text-green-500" />
              ) : (
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
              )}
              <span className="truncate flex-1">{key.replace(/\d{13,}$/, "")}</span>
              <span>{progress}%</span>
            </div>
          ))}
        </div>
      )}

      <VideoLibraryPicker
        open={showLibrary}
        onOpenChange={setShowLibrary}
        onSelect={handleLibrarySelect}
        projectId={entityType === "units" ? projectId : undefined}
      />
    </div>
  );
}
