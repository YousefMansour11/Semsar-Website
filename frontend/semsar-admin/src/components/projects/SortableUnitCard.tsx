import { type Property, type Contact } from "@/store";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { GripVertical, ImagePlus, Hash, User, Phone, Pencil, Eye, Trash2 } from "lucide-react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";

interface Props {
  unit: Property;
  contact?: Contact;
  onEdit: () => void;
  onView: () => void;
  onDelete: () => void;
}

export function SortableUnitCard({ unit, contact, onEdit, onView, onDelete }: Props) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: unit.id });
  const style = { transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? 0.5 : 1, zIndex: isDragging ? 50 : undefined };

  return (
    <div ref={setNodeRef} style={style} className="bg-card border border-border rounded-2xl overflow-hidden relative group">
      <div className="absolute top-2 right-2 z-10 p-1 rounded-md opacity-0 group-hover:opacity-100 transition-opacity cursor-grab active:cursor-grabbing text-muted-foreground hover:text-foreground hover:bg-accent/80 bg-background/60 backdrop-blur-sm"
        {...attributes} {...listeners} aria-label="Drag to reorder">
        <GripVertical className="w-4 h-4" />
      </div>
      <div className="relative h-40 bg-accent overflow-hidden">
        {unit.images[0] ? (
          <img src={unit.images[0]} alt={unit.title} loading="lazy" width={400} height={300} className="w-full h-full object-cover" />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-muted-foreground"><ImagePlus className="w-8 h-8" /></div>
        )}
        <div className="absolute top-2 left-2">
          <Badge variant="outline" className="bg-background/80 backdrop-blur-sm text-[10px] font-mono">
            <Hash className="w-2.5 h-2.5 mr-1" />{unit.code}
          </Badge>
        </div>
      </div>
      <div className="p-4 space-y-2">
        <h3 className="font-semibold text-sm truncate">{unit.title}</h3>
        <p className="text-lg font-bold text-primary">{unit.listingType === 'Rental' ? `${(unit.rentPerMonth || unit.price).toLocaleString()} ${unit.currency}/mo` : `${unit.price.toLocaleString()} ${unit.currency}`}</p>
        {contact && (
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <User className="w-3 h-3" /> {contact.name} <Phone className="w-3 h-3" /> {contact.phone}
          </div>
        )}
      </div>
      <div className="px-4 pb-4 flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
        <Button variant="outline" size="sm" className="flex-1" onClick={onEdit}><Pencil className="w-3 h-3 mr-1" /> Edit</Button>
        <Button variant="outline" size="sm" onClick={onView}><Eye className="w-3 h-3" /></Button>
        <Button variant="outline" size="sm" className="text-destructive" onClick={onDelete}><Trash2 className="w-3 h-3" /></Button>
      </div>
    </div>
  );
}
