import { type Project } from "@/store";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { MapPin, Building, GripVertical, Pencil, Trash2 } from "lucide-react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";

interface Props {
  project: Project;
  onClick: () => void;
  onEdit: () => void;
  onDelete: () => void;
}

export function SortableProjectCard({ project, onClick, onEdit, onDelete }: Props) {
  const unitCount = project.unitCount;
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: project.id });
  const style = { transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? 0.5 : 1, zIndex: isDragging ? 50 : undefined };

  return (
    <div ref={setNodeRef} style={style}>
      <Card className="bg-card border-border hover:border-primary/30 transition-all cursor-pointer hover:shadow-lg hover:shadow-primary/5 relative group overflow-hidden">
        <div className="absolute top-3 right-3 z-10 p-1 rounded-md opacity-0 group-hover:opacity-100 transition-opacity cursor-grab active:cursor-grabbing text-muted-foreground hover:text-foreground hover:bg-accent"
          {...attributes} {...listeners} aria-label="Drag to reorder">
          <GripVertical className="w-4 h-4" />
        </div>
        {project.image && (
          <div className="h-40 overflow-hidden">
            <img src={project.image} alt={project.name} loading="lazy" width={400} height={300} className="w-full h-full object-cover" />
          </div>
        )}
        <div onClick={onClick}>
          <CardHeader className="pb-2">
            <div className="flex justify-between items-start pr-6">
              <CardTitle className="text-lg">{project.name}</CardTitle>
              <Badge variant="secondary">{unitCount} units</Badge>
            </div>
            <CardDescription className="flex flex-wrap items-center gap-x-3 gap-y-1 mt-1">
              <span className="flex items-center gap-1"><MapPin className="h-3.5 w-3.5" /> {project.location}</span>
              {project.developer && <span className="flex items-center gap-1"><Building className="h-3.5 w-3.5" /> {project.developer}</span>}
            </CardDescription>
          </CardHeader>
          <CardContent className="pt-0">
            <p className="text-sm text-muted-foreground line-clamp-2">{project.description}</p>
            {project.highlights.length > 0 && (
              <div className="flex flex-wrap gap-1 mt-2">
                {project.highlights.slice(0, 3).map((h, i) => (
                  <Badge key={h + '-' + i} variant="outline" className="text-[10px]">{h}</Badge>
                ))}
                {project.highlights.length > 3 && <Badge variant="outline" className="text-[10px]">+{project.highlights.length - 3}</Badge>}
              </div>
            )}
          </CardContent>
        </div>
        <div className="px-6 pb-4 flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
          <Button variant="outline" size="sm" className="flex-1" onClick={onEdit}><Pencil className="w-3 h-3 mr-1" /> Edit</Button>
          <Button variant="outline" size="sm" className="text-destructive" onClick={onDelete}><Trash2 className="w-3 h-3" /></Button>
        </div>
      </Card>
    </div>
  );
}
