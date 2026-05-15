/* eslint-disable @typescript-eslint/no-explicit-any */
import { useState, useMemo, useRef, useEffect } from "react";
import { useStore, apiPropertyToStore, type Property, type PropertyType, type ListingType, PROPERTY_TYPES, LISTING_TYPES, PROPERTY_VIEWS } from "@/store";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { CONTACT_TYPE_MAP, LISTING_BADGE } from "@/lib/constants";
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Checkbox } from "@/components/ui/checkbox";
import { Switch } from "@/components/ui/switch";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from "@/components/ui/dialog";
import { AdminLocationPicker } from "@/components/AdminLocationPicker";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import {
  Accordion, AccordionContent, AccordionItem, AccordionTrigger,
} from "@/components/ui/accordion";
import { Plus, Search, Building2, Star, Pencil, Trash2, ImagePlus, X, GripVertical, Loader2, Eye, Languages } from "lucide-react";
import {
  DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext, verticalListSortingStrategy, useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { cn, autoSlug } from "@/lib/utils";
import { UserPropertyCard } from "@/components/properties/UserPropertyCard";
import { PropertyDetailDialog } from "@/components/properties/PropertyDetailDialog";
import { AdminPropertyDetailDialog } from "@/components/properties/AdminPropertyDetailDialog";
import { VideoUploadZone } from "@/components/VideoUploadZone";

function fmtNum(n: string): string {
  const raw = n.replace(/[^0-9]/g, '');
  return raw ? raw.replace(/\B(?=(\d{3})+(?!\d))/g, ',') : '';
}
function stripNum(s: string): string {
  return s.replace(/[^0-9]/g, '');
}

function SortableRow({ property, selected, onToggle, onEdit, onDelete, onToggleFeatured, onView }: {
  property: Property; selected: boolean;
  onToggle: () => void; onEdit: () => void; onDelete: () => void; onToggleFeatured: () => void; onView: () => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: property.id });
  const style = { transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? 0.5 : 1 };
  const p = property;

  return (
    <TableRow ref={setNodeRef} style={style} className="group">
      <TableCell className="w-10"><Checkbox checked={selected} onCheckedChange={onToggle} /></TableCell>
      <TableCell className="w-10 cursor-grab active:cursor-grabbing" {...attributes} {...listeners}>
        <GripVertical className="w-4 h-4 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
      </TableCell>
      <TableCell className="w-16">
        {p.images[0] ? (
          <img src={p.images[0]} alt="" loading="lazy" width={56} height={40} className="w-14 h-10 object-cover rounded-md" />
        ) : (
          <div className="w-14 h-10 bg-accent rounded-md flex items-center justify-center"><ImagePlus className="w-4 h-4 text-muted-foreground" /></div>
        )}
      </TableCell>
      <TableCell className="font-mono text-xs">{p.code}</TableCell>
      <TableCell className="font-medium max-w-[200px] truncate">{p.title}</TableCell>
      <TableCell>{p.propertyType}</TableCell>
      <TableCell>
        <Badge variant="outline" className={LISTING_BADGE[p.listingType]}>{p.listingType}</Badge>
      </TableCell>
      <TableCell className="font-semibold">
        {p.listingType === 'Rental' ? `${(p.rentPerMonth || p.price).toLocaleString()}/mo` : p.price.toLocaleString()} {p.currency}
      </TableCell>
      <TableCell className="max-w-[120px] truncate">{p.location}</TableCell>
      <TableCell>
        <button onClick={onToggleFeatured} aria-label={p.isFeatured ? "Unfeature" : "Feature"}>
          <Star className={cn("w-4 h-4", p.isFeatured ? "fill-primary text-primary" : "text-muted-foreground hover:text-primary")} />
        </button>
      </TableCell>
      <TableCell>
        <div className="flex gap-1">
          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={onEdit} aria-label={"Edit"}><Pencil className="w-3.5 h-3.5" /></Button>
          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={onView} aria-label={"View"}><Eye className="w-3.5 h-3.5" /></Button>
          <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive" onClick={onDelete} aria-label={"Delete"}><Trash2 className="w-3.5 h-3.5" /></Button>
        </div>
      </TableCell>
    </TableRow>
  );
}

interface InstallmentRow {
  paymentType: 'Installment' | 'Cash';
  downPaymentPercent: string;
  discountPercent: string;
  years: string;
  monthlyAmount: string;
  isEnabled: boolean;
}

interface PropertyFormData {
  titleEn: string; titleAr: string;
  descriptionEn: string; descriptionAr: string;
  price: string; rentPerMonth: string; currency: string;
  location: string; size: string;
  governorate: string; city: string; area: string;
  governorateAr: string; cityAr: string; areaAr: string;
  bedrooms: string; bathrooms: string; floor: string; totalFloors: string;
  isFurnished: boolean; view: string;
  propertyType: PropertyType; listingType: ListingType;
  contactId: string; projectId: string | null; images: string[];
  features: string[]; featureInput: string;
  featuresAr: string[]; featuresArInput: string;
  locationAr: string;
  isRecommended: boolean;
  deliveryText: string;
  deliveryTextAr: string;
  constructionStatus: string;
  availabilityStatus: string;
  ownershipType: string;
  virtualTourUrl: string;
  highlightsAr: string[]; highlightsArInput: string;
  nearbyPlaces: string[]; nearbyPlaceInput: string;
  nearbyPlacesAr: string[]; nearbyPlaceArInput: string;
  installments: InstallmentRow[];
  // SEO
  slug: string; slugIsAuto: boolean;
  seoTitle: string; seoDescription: string; seoKeywords: string;
  seoTitleAr: string; seoDescriptionAr: string; seoKeywordsAr: string;
}

const defaultForm: PropertyFormData = {
  titleEn: '', titleAr: '',
  descriptionEn: '', descriptionAr: '',
  price: '', rentPerMonth: '', currency: 'EGP',
  location: '', size: '',
  governorate: '', city: '', area: '',
  governorateAr: '', cityAr: '', areaAr: '',
  bedrooms: '', bathrooms: '', floor: '', totalFloors: '',
  isFurnished: false, view: 'Unknown',
  propertyType: 'Apartment', listingType: 'Resale',
  contactId: '', projectId: null, images: [],
  features: [], featureInput: '',
  featuresAr: [], featuresArInput: '',
  locationAr: '',
  isRecommended: false,
  deliveryText: '',
  deliveryTextAr: '',
  constructionStatus: '',
  availabilityStatus: 'Available',
  ownershipType: '',
  virtualTourUrl: '',
  highlightsAr: [], highlightsArInput: '',
  nearbyPlaces: [], nearbyPlaceInput: '',
  nearbyPlacesAr: [], nearbyPlaceArInput: '',
  installments: [],
  slug: '', slugIsAuto: true,
  seoTitle: '', seoDescription: '', seoKeywords: '',
  seoTitleAr: '', seoDescriptionAr: '', seoKeywordsAr: '',
};

export default function PropertiesPage() {
  const properties = useStore(s => s.properties);
  const units = useStore(s => s.units);
  const contacts = useStore(s => s.contacts);
  const projects = useStore(s => s.projects);
  const previewMode = useStore(s => s.previewMode);
  const deleteProperty = useStore(s => s.deleteProperty);
  const reorderProperties = useStore(s => s.reorderProperties);
  const toggleFeatured = useStore(s => s.toggleFeatured);
  const addProperty = useStore(s => s.addProperty);
  const updateProperty = useStore(s => s.updateProperty);
  const loadProperties = useStore(s => s.loadProperties);
  const loadUnits = useStore(s => s.loadUnits);
  const loadContacts = useStore(s => s.loadContacts);
  const loadProjects = useStore(s => s.loadProjects);

  useEffect(() => {
    setInitialLoading(true);
    Promise.all([loadProperties(), loadUnits(), loadContacts(), loadProjects()]).finally(() => setInitialLoading(false));
  }, [loadProperties, loadUnits, loadContacts, loadProjects]);

  const [search, setSearch] = useState("");
  const [listingTab, setListingTab] = useState<string>("all");
  const filterListing = listingTab;
  const [filterType, setFilterType] = useState<string>("all");
  const [filterLocation, setFilterLocation] = useState<string>("all");
  const [filterFeatured, setFilterFeatured] = useState<string>("all");
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [detailProperty, setDetailProperty] = useState<Property | null>(null);
  const [form, setForm] = useState<PropertyFormData>(defaultForm);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [initialLoading, setInitialLoading] = useState(true);
  const [pendingFiles, setPendingFiles] = useState<File[]>([]);
  const [pendingPreviews, setPendingPreviews] = useState<string[]>([]);
  const [newContactMode, setNewContactMode] = useState(false);
  const [imageIdByUrl, setImageIdByUrl] = useState<Record<string, number>>({});
  const [removedImageIds, setRemovedImageIds] = useState<number[]>([]);
  const [existingVideos, setExistingVideos] = useState<{ id: number; url: string; publicId: string }[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [previewLang, setPreviewLang] = useState<'en' | 'ar'>('en');
  const [newContactName, setNewContactName] = useState('');
  const [newContactPhone, setNewContactPhone] = useState('');
  const [newContactType, setNewContactType] = useState<'Owner' | 'Broker'>('Owner');
  const [contactInfo, setContactInfo] = useState<{ whatsappNumber?: string; phoneNumber?: string }>({});
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    adminApi.getSettings().then(s => setContactInfo({ whatsappNumber: s.whatsappNumber, phoneNumber: s.phoneNumber })).catch(() => {});
  }, []);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor)
  );

  const locations = useMemo(() => {
    const all = filterListing === 'Project' ? units : properties;
    return [...new Set(all.map(p => p.location))];
  }, [properties, units, filterListing]);

  const filtered = useMemo(() => {
    const items = filterListing === 'Project' ? units : properties;
    return items
      .filter(p => {
        const matchesSearch = p.title.toLowerCase().includes(search.toLowerCase()) ||
          p.location.toLowerCase().includes(search.toLowerCase()) ||
          p.code.toLowerCase().includes(search.toLowerCase());
        const matchesListing = filterListing === 'all' || filterListing === 'Project' || p.listingType === filterListing;
        const matchesType = filterType === 'all' || p.propertyType === filterType;
        const matchesLocation = filterLocation === 'all' || p.location === filterLocation;
        const matchesFeatured = filterFeatured === 'all' || (filterFeatured === 'yes' ? p.isFeatured : !p.isFeatured);
        return matchesSearch && matchesListing && matchesType && matchesLocation && matchesFeatured;
      })
      .sort((a, b) => a.order - b.order);
  }, [properties, units, search, filterListing, filterType, filterLocation, filterFeatured]);

  const openAdd = () => {
    setForm(defaultForm);
    setNewContactMode(false);
    setNewContactName('');
    setNewContactPhone('');
    setNewContactType('Owner');
    setIsFormOpen(true);
  };

  const openEdit = async (p: Property) => {
    clearPending();
    setRemovedImageIds([]);
    setExistingVideos([]);
    setNewContactMode(false);
    setNewContactName('');
    setNewContactPhone('');
    setNewContactType('Owner');
    const apiId = parseInt(p.id, 10);
    let detail = p;
    const urlToId: Record<string, number> = {};
    if (!isNaN(apiId)) {
      try {
        const fetched: any = await adminApi.getProperty(apiId);
        if (fetched) {
          const { adminImages, ...clean } = fetched;
          if (Array.isArray(adminImages)) {
            adminImages.forEach((img: any) => {
              const key = img.Url ?? img.url;
              const val = img.Id ?? img.id;
              if (key && val) urlToId[key] = val;
            });
          }
          const mergedLocationAr = p.locationAr || clean.locationAr;
          detail = { ...p, ...clean, locationAr: mergedLocationAr };
          if (mergedLocationAr) {
            updateProperty(p.id, { locationAr: mergedLocationAr });
          }
          if (Array.isArray(fetched.videos)) {
            setExistingVideos(fetched.videos.map((v: any) => ({ id: v.id, url: v.url, publicId: v.publicId ?? '' })));
          }
        }
      } catch { toast.error("Failed to fetch full property details from server; falling back to stored data"); }
    }
    setImageIdByUrl(urlToId);
    setForm({
      titleEn: detail.titleEn || detail.title, titleAr: detail.titleAr || '',
      descriptionEn: detail.descriptionEn || detail.description || '', descriptionAr: detail.descriptionAr || '',
      price: String(detail.price), rentPerMonth: String(detail.rentPerMonth || ''),
      currency: detail.currency,       location: detail.location, locationAr: (detail as any).locationAr || '',
      size: String(detail.size || ''),
      governorate: (detail.location || '').split(/[،,]\s*/)[0]?.trim() || '',
      city: (detail.location || '').split(/[،,]\s*/)[1]?.trim() || '',
      area: (detail.location || '').split(/[،,]\s*/)[2]?.trim() || '',
      governorateAr: ((detail as any).locationAr || '').split(/[،,]\s*/)[0]?.trim() || '',
      cityAr: ((detail as any).locationAr || '').split(/[،,]\s*/)[1]?.trim() || '',
      areaAr: ((detail as any).locationAr || '').split(/[،,]\s*/)[2]?.trim() || '',
      bedrooms: detail.bedrooms != null && detail.bedrooms > 0 ? String(detail.bedrooms) : '',
      bathrooms: detail.bathrooms != null && detail.bathrooms > 0 ? String(detail.bathrooms) : '',
      floor: detail.floor != null ? String(detail.floor) : '',
      totalFloors: detail.totalFloors != null ? String(detail.totalFloors) : '',
      isFurnished: !!detail.isFurnished, view: detail.view || 'Unknown',
      propertyType: detail.propertyType, listingType: detail.listingType,
      contactId: String(detail.contactId ?? ''), projectId: detail.projectId, images: detail.images,
      features: detail.features, featuresAr: (detail as any).featuresAr || [], featureInput: '', featuresArInput: '',
      installments: (detail.installments || []).map((i: any) => ({
        paymentType: (i.paymentType ?? i.PaymentType) === 'Cash' ? 'Cash' : 'Installment',
        downPaymentPercent: String(i.downPaymentPercent ?? 0),
        discountPercent: String(i.discountPercent ?? ''),
        years: String(i.years ?? 0),
        monthlyAmount: String(i.monthlyAmount ?? ''),
        isEnabled: i.isEnabled,
      })),
      isRecommended: !!detail.isRecommended,
      deliveryText: (detail as any).deliveryText || '',
      deliveryTextAr: (detail as any).deliveryTextAr || '',
      constructionStatus: (detail as any).constructionStatus || '',
      availabilityStatus: (detail as any).availabilityStatus || 'Available',
      ownershipType: (detail as any).ownershipType || '',
      virtualTourUrl: (detail as any).virtualTourUrl || '',
      highlightsAr: (detail as any).highlightsAr || [], highlightsArInput: '',
      nearbyPlaces: (detail as any).nearbyPlaces || [], nearbyPlaceInput: '',
      nearbyPlacesAr: (detail as any).nearbyPlacesAr || [], nearbyPlaceArInput: '',
      slug: detail.slug, slugIsAuto: detail.slugIsAuto,
      seoTitle: detail.seoTitle || '', seoDescription: detail.seoDescription || '', seoKeywords: detail.seoKeywords || '',
      seoTitleAr: detail.seoTitleAr || '', seoDescriptionAr: detail.seoDescriptionAr || '', seoKeywordsAr: detail.seoKeywordsAr || '',
    });
    setEditingId(p.id);
    setIsFormOpen(true);
  };

  const handleSubmit = async () => {
    if (!form.titleEn || !form.titleAr || !form.descriptionEn || !form.location) {
      toast.error("Please fill in all required fields (Title EN/AR, Description EN, Location)");
      return;
    }
    if (form.listingType === 'Rental' && !form.rentPerMonth) {
      toast.error("Please enter a rent amount");
      return;
    }
    if (form.listingType !== 'Rental' && !form.price) {
      toast.error("Please enter a price");
      return;
    }

    if (!form.contactId && !newContactMode) {
      toast.error("Please select or create a contact");
      return;
    }
    if (newContactMode && (!newContactName.trim() || !newContactPhone.trim())) {
      toast.error("Please provide contact info");
      return;
    }

    const price = form.listingType === 'Rental' ? Number(form.rentPerMonth) : Number(form.price);
    const installments = form.installments
      .filter(i => i.paymentType === 'Cash' || (i.downPaymentPercent && i.years))
      .map(i => i.paymentType === 'Cash' ? ({
        paymentType: 'Cash' as const,
        downPaymentPercent: 100,
        discountPercent: Number(i.discountPercent) || 0,
        years: 0,
        monthlyAmount: 0,
        isEnabled: i.isEnabled,
      }) : ({
        paymentType: 'Installment' as const,
        downPaymentPercent: Number(i.downPaymentPercent),
        years: Number(i.years),
        monthlyAmount: Number(i.monthlyAmount) || Math.round((price * (1 - Number(i.downPaymentPercent) / 100)) / (Number(i.years) * 12)),
        isEnabled: i.isEnabled,
      }));

    const slug = form.slugIsAuto ? autoSlug(form.titleEn) : form.slug;
    const selectedContact = contacts.find(c => c.id === form.contactId);
    const contactPayload = newContactMode
      ? { name: newContactName.trim(), phone: newContactPhone.trim(), type: CONTACT_TYPE_MAP[newContactType] ?? 0 }
      : selectedContact
        ? { name: selectedContact.name, phone: selectedContact.phone, type: CONTACT_TYPE_MAP[selectedContact.type] ?? 0 }
        : undefined;

    const contactId = newContactMode ? '' : form.contactId;
    const contactName = newContactMode ? newContactName.trim() : selectedContact?.name || '';
    const contactPhone = newContactMode ? newContactPhone.trim() : selectedContact?.phone || '';

    const data = {
      titleEn: form.titleEn,
      titleAr: form.titleAr || form.titleEn,
      descriptionEn: form.descriptionEn,
      descriptionAr: form.descriptionAr || form.descriptionEn,
      price,       rentPerMonth: form.listingType === 'Rental' ? Number(form.rentPerMonth) : null,
      location: form.location, locationAr: [form.governorateAr, form.cityAr, form.areaAr].filter(Boolean).join(', ') || undefined, size: Number(form.size) || 0,
      bedrooms: form.bedrooms ? Number(form.bedrooms) : null,
      bathrooms: form.bathrooms ? Number(form.bathrooms) : null,
      floor: form.floor ? Number(form.floor) : null,
      totalFloors: form.totalFloors ? Number(form.totalFloors) : null,
      isFurnished: form.isFurnished,
      view: form.view !== 'Unknown' ? form.view : null,
      propertyType: form.propertyType, listingType: form.listingType,
      contact: contactPayload,
      contactId, contactName, contactPhone,
      images: form.images,
      features: form.features, featuresAr: form.featuresAr.length ? form.featuresAr : undefined, installments,
      isRecommended: form.isRecommended, deliveryText: form.deliveryText, deliveryTextAr: form.deliveryTextAr, constructionStatus: form.constructionStatus, availabilityStatus: form.availabilityStatus, ownershipType: form.ownershipType, virtualTourUrl: form.virtualTourUrl,
      highlightsAr: form.highlightsAr.length ? form.highlightsAr : undefined,
      nearbyPlaces: form.nearbyPlaces.length ? form.nearbyPlaces : undefined,
      nearbyPlacesAr: form.nearbyPlacesAr.length ? form.nearbyPlacesAr : undefined,
      seoTitle: form.seoTitle || null, seoDescription: form.seoDescription || null, seoKeywords: form.seoKeywords || null,
      seoTitleAr: form.seoTitleAr || null, seoDescriptionAr: form.seoDescriptionAr || null, seoKeywordsAr: form.seoKeywordsAr || null,
    };
    const apiPayload = { ...data };

    setIsSubmitting(true);
    if (editingId) {
      const apiId = parseInt(editingId, 10);
      if (!isNaN(apiId)) {
        for (const imgId of removedImageIds) {
          try { await adminApi.deletePropertyImage(apiId, imgId); }
          catch { /* skip - image might already be gone */ }
        }
      }
      if (!isNaN(apiId)) {
        try {
          const patchPayload = {
            ...apiPayload,
            slug, slugIsAuto: form.slugIsAuto,
            contact: contactPayload,
          };
            await adminApi.updateProperty(apiId, patchPayload);
            if (pendingFiles.length > 0) {
              await adminApi.uploadPropertyImages(apiId, pendingFiles);
            }
            const refreshed: any = await adminApi.getProperty(apiId);
            if (refreshed) {
              const updatedImages = Array.isArray(refreshed.images) ? refreshed.images.filter(Boolean) : [];
              const updatedVideos = Array.isArray(refreshed.videos) ? refreshed.videos.map((v: any) => ({ id: v.id, url: v.url, publicId: v.publicId ?? '' })) : undefined;
              updateProperty(editingId, {
                images: updatedImages,
                videos: updatedVideos,
                locationAr: data.locationAr || refreshed.locationAr || undefined,
              });
              setForm(prev => ({ ...prev, images: updatedImages }));
              if (updatedVideos) setExistingVideos(updatedVideos);
            }
            await loadProperties();
            toast.success("Property updated");
        } catch (e: any) {
          loadProperties();
          toast.error(e?.message || "Failed to update property");
          setIsSubmitting(false);
          return;
        }
      } else {
        updateProperty(editingId, data);
        toast.success("Property updated");
      }
    } else {
      try {
        const created: any = await adminApi.createProperty(apiPayload);
        const newId = created?.id ?? created?.Id;
        if (newContactMode) {
          const respContactId = created?.contactId ?? created?.ContactId;
          if (respContactId) {
            data.contactId = String(respContactId);
            data.contactName = newContactName.trim();
            data.contactPhone = newContactPhone.trim();
          }
        }
        if (newId && pendingFiles.length > 0) {
          await adminApi.uploadPropertyImages(newId, pendingFiles);
        }
        addProperty(data);
        await loadProperties();
        if (newId && data.locationAr) {
          updateProperty(String(newId), { locationAr: data.locationAr });
        }
        toast.success("Property added");
      } catch (e) {
        loadProperties();
        toast.error(e instanceof Error ? e.message : "Failed to create property");
        setIsSubmitting(false);
        return;
      }
    }
    clearPending();
    setRemovedImageIds([]);
    setImageIdByUrl({});
    setNewContactMode(false);
    setNewContactName('');
    setNewContactPhone('');
    setNewContactType('Owner');
    setIsFormOpen(false);
    setIsSubmitting(false);
  };

  const handleDelete = async () => {
    if (deleteId) {
      const apiId = parseInt(deleteId, 10);
      if (!isNaN(apiId)) {
        try {
          const detail: any = await adminApi.getProperty(apiId);
          if (detail?.videos) {
            for (const v of detail.videos) {
              try { await adminApi.deletePropertyVideo(apiId, v.id); } catch { /* skip individual failures */ }
            }
          }
        } catch { /* fall through to delete entity */ }
        try { await adminApi.deleteProperty(apiId); } catch { toast.error("Failed to sync property delete with server"); }
      }
      deleteProperty(deleteId);
      setSelectedIds(prev => prev.filter(id => id !== deleteId));
      toast.success("Property deleted");
      setDeleteId(null);
    }
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      reorderProperties(active.id as string, over.id as string);
      const latest = [...useStore.getState().properties]
        .sort((a, b) => a.order - b.order)
        .map((p, i) => ({ ...p, order: i }));
      for (const p of latest) {
        const apiId = parseInt(p.id, 10);
        if (!isNaN(apiId)) {
          try {
            await adminApi.updateProperty(apiId, { sortOrder: p.order });
          } catch { /* skip individual failures */ }
        }
      }
      toast.success("Properties reordered");
    }
  };

  const handleImageUpload = (files: FileList | null) => {
    if (!files) return;
    const newFiles = Array.from(files);
    setPendingFiles(prev => [...prev, ...newFiles]);
    const urls = newFiles.map(f => URL.createObjectURL(f));
    setPendingPreviews(prev => [...prev, ...urls]);
  };

  const removePending = (idx: number) => {
    URL.revokeObjectURL(pendingPreviews[idx]);
    setPendingFiles(prev => prev.filter((_, i) => i !== idx));
    setPendingPreviews(prev => prev.filter((_, i) => i !== idx));
  };

  const clearPending = () => {
    pendingPreviews.forEach(u => URL.revokeObjectURL(u));
    setPendingFiles([]);
    setPendingPreviews([]);
  };

  const addFeature = () => {
    if (form.featureInput.trim()) {
      setForm(prev => ({ ...prev, features: [...prev.features, prev.featureInput.trim()], featureInput: '' }));
    }
  };
  const removeFeature = (idx: number) => setForm(prev => ({ ...prev, features: prev.features.filter((_, i) => i !== idx) }));
  const addFeatureAr = () => {
    if (form.featuresArInput.trim()) {
      setForm(prev => ({ ...prev, featuresAr: [...prev.featuresAr, prev.featuresArInput.trim()], featuresArInput: '' }));
    }
  };
  const removeFeatureAr = (idx: number) => setForm(prev => ({ ...prev, featuresAr: prev.featuresAr.filter((_, i) => i !== idx) }));
  const addHighlightAr = () => {
    if (form.highlightsArInput.trim()) {
      setForm(prev => ({ ...prev, highlightsAr: [...prev.highlightsAr, prev.highlightsArInput.trim()], highlightsArInput: '' }));
    }
  };
  const removeHighlightAr = (idx: number) => setForm(prev => ({ ...prev, highlightsAr: prev.highlightsAr.filter((_, i) => i !== idx) }));
  const addNearbyPlace = () => {
    if (form.nearbyPlaceInput.trim()) {
      setForm(prev => ({ ...prev, nearbyPlaces: [...prev.nearbyPlaces, prev.nearbyPlaceInput.trim()], nearbyPlaceInput: '' }));
    }
  };
  const removeNearbyPlace = (idx: number) => setForm(prev => ({ ...prev, nearbyPlaces: prev.nearbyPlaces.filter((_, i) => i !== idx) }));
  const addNearbyPlaceAr = () => {
    if (form.nearbyPlaceArInput.trim()) {
      setForm(prev => ({ ...prev, nearbyPlacesAr: [...prev.nearbyPlacesAr, prev.nearbyPlaceArInput.trim()], nearbyPlaceArInput: '' }));
    }
  };
  const removeNearbyPlaceAr = (idx: number) => setForm(prev => ({ ...prev, nearbyPlacesAr: prev.nearbyPlacesAr.filter((_, i) => i !== idx) }));

  const addInstallment = () => setForm(prev => ({
    ...prev,
    installments: [...prev.installments, { paymentType: 'Installment', downPaymentPercent: '10', discountPercent: '', years: '5', monthlyAmount: '', isEnabled: true }],
  }));
  const addCashOption = () => setForm(prev => ({
    ...prev,
    installments: [...prev.installments, { paymentType: 'Cash', downPaymentPercent: '100', discountPercent: '20', years: '0', monthlyAmount: '', isEnabled: true }],
  }));
  const updateInstallment = (idx: number, patch: Partial<InstallmentRow>) => setForm(prev => ({
    ...prev,
    installments: prev.installments.map((i, n) => n === idx ? { ...i, ...patch } : i),
  }));
  const removeInstallment = (idx: number) => setForm(prev => ({
    ...prev,
    installments: prev.installments.filter((_, i) => i !== idx),
  }));

  const toggleSelect = (id: string) => setSelectedIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);
  const toggleSelectAll = () => setSelectedIds(prev => prev.length === filtered.length ? [] : filtered.map(p => p.id));

  const handleViewDetails = async (id: string) => {
    try {
      const numericId = Number(id);
      if (isNaN(numericId)) {
        const found = properties.find(p => p.id === id);
        if (found) setDetailProperty(apiPropertyToStore(found));
        return;
      }
      const raw = await adminApi.getProperty(numericId);
      if (raw) setDetailProperty(apiPropertyToStore(raw));
    } catch {
      const found = properties.find(p => p.id === id);
      if (found) setDetailProperty(apiPropertyToStore(found));
      toast.error("Failed to load full property details; showing stored data");
    }
  };

  if (initialLoading) {
    return <div className="flex items-center justify-center py-20"><Loader2 className="w-8 h-8 animate-spin text-primary" /></div>;
  }

  // Preview
  if (previewMode) {
    const featured = filtered.filter(p => p.isFeatured);
    const regular = filtered.filter(p => !p.isFeatured);

    return (
      <div className="space-y-6 animate-slide-in">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-3xl font-bold tracking-tight">{"Properties"}</h2>
            <p className="text-muted-foreground mt-1">{filtered.length} {"Manage property listings."}</p>
          </div>
          <Button variant="ghost" size="sm" onClick={() => setPreviewLang(l => l === 'en' ? 'ar' : 'en')} className="gap-1 text-xs">
            <Languages className="w-3 h-3" /> {previewLang === 'en' ? 'AR' : 'EN'}
          </Button>
        </div>
        <div className="relative max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Label htmlFor="prop-search" className="sr-only">{"Search properties"}</Label>
          <Input id="prop-search" autoComplete="off" placeholder={"Search by code, title, location..."} value={search} onChange={(e) => setSearch(e.target.value)} className="pl-10" />
        </div>
        {featured.length > 0 && (
          <div className="space-y-4">
            <h3 className="text-lg font-semibold flex items-center gap-2"><Star className="w-5 h-5 text-primary fill-primary" /> {"Featured"}</h3>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {featured.map(p => <UserPropertyCard key={p.id} property={p} onClick={() => handleViewDetails(p.id)} lang={previewLang} />)}
            </div>
          </div>
        )}
        {regular.length > 0 && (
          <div className="space-y-4">
            <h3 className="text-lg font-semibold flex items-center gap-2"><Building2 className="w-5 h-5 text-muted-foreground" /> {"All Properties"}</h3>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {regular.map(p => <UserPropertyCard key={p.id} property={p} onClick={() => handleViewDetails(p.id)} lang={previewLang} />)}
            </div>
          </div>
        )}
        <PropertyDetailDialog key={detailProperty?.id} property={detailProperty} open={!!detailProperty} onOpenChange={(o) => !o && setDetailProperty(null)} whatsappNumber={contactInfo.whatsappNumber} phoneNumber={contactInfo.phoneNumber} />
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-slide-in">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">{"Properties"}</h2>
          <p className="text-muted-foreground mt-1">{filtered.length} {"Manage property listings."}</p>
        </div>
        <Button onClick={openAdd} className="bg-primary hover:bg-primary/90">
          <Plus className="mr-2 h-4 w-4" /> {"Add Property"}
        </Button>
      </div>

      <Tabs value={listingTab} onValueChange={setListingTab} className="w-full">
        <TabsList>
          <TabsTrigger value="all">{"All"}</TabsTrigger>
          {LISTING_TYPES.map(t => <TabsTrigger key={t.value} value={t.value}>{t.label}</TabsTrigger>)}
        </TabsList>
      </Tabs>

      <div className="flex flex-wrap gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Label htmlFor="prop-search-table" className="sr-only">{"Search properties"}</Label>
          <Input id="prop-search-table" autoComplete="off" placeholder={"Search by code, title, location..."} value={search} onChange={(e) => setSearch(e.target.value)} className="pl-10" />
        </div>
        <Label htmlFor="prop-filter-type" className="sr-only">{"Filter by property type"}</Label>
        <Select value={filterType} onValueChange={setFilterType}>
          <SelectTrigger id="prop-filter-type" className="w-[150px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{"All Types"}</SelectItem>
            {PROPERTY_TYPES.map(t => <SelectItem key={t.value} value={t.value}>{t.label}</SelectItem>)}
          </SelectContent>
        </Select>
        <Label htmlFor="prop-filter-location" className="sr-only">{"Filter by location"}</Label>
        <Select value={filterLocation} onValueChange={setFilterLocation}>
          <SelectTrigger id="prop-filter-location" className="w-[180px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{"All Locations"}</SelectItem>
            {locations.map(l => <SelectItem key={l} value={l}>{l}</SelectItem>)}
          </SelectContent>
        </Select>
        <Label htmlFor="prop-filter-featured" className="sr-only">{"Filter by featured status"}</Label>
        <Select value={filterFeatured} onValueChange={setFilterFeatured}>
          <SelectTrigger id="prop-filter-featured" className="w-[130px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{"All"}</SelectItem>
            <SelectItem value="yes">{"Featured"}</SelectItem>
            <SelectItem value="no">{"Not Featured"}</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {selectedIds.length > 0 && (
        <div className="flex items-center gap-3 p-3 bg-accent/50 border border-border rounded-xl">
          <span className="text-sm font-medium">{selectedIds.length} {"selected"}</span>
          <Button variant="outline" size="sm" onClick={async () => {
            for (const id of selectedIds) {
              const p = properties.find(x => x.id === id);
              if (!p) continue;
              const apiId = parseInt(id, 10);
              if (!isNaN(apiId)) {
                try { await adminApi.updateProperty(apiId, { isFeatured: !p.isFeatured }); } catch { /* skip individual failures */ }
              }
              toggleFeatured(id);
            }
            setSelectedIds([]);
            toast.success("Featured status toggled");
          }}>{"Toggle Featured"}</Button>
          <Button variant="destructive" size="sm" onClick={async () => {
            for (const id of selectedIds) {
              const apiId = parseInt(id, 10);
              if (!isNaN(apiId)) {
                try { await adminApi.deleteProperty(apiId); } catch { /* skip individual failures */ }
              }
              deleteProperty(id);
            }
            setSelectedIds([]);
            toast.success("Properties deleted");
          }}>{"Delete"}</Button>
        </div>
      )}

      {filtered.length === 0 ? (
        <div className="text-center py-20 border-2 border-dashed border-border rounded-2xl">
          <Building2 className="mx-auto h-12 w-12 text-muted-foreground/30 mb-4" />
          <h3 className="text-lg font-medium">{"No properties found"}</h3>
        </div>
      ) : (
        <div className="border border-border rounded-xl overflow-x-auto">
          <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
            <SortableContext items={filtered.map(p => p.id)} strategy={verticalListSortingStrategy}>
              <Table>
                <caption className="sr-only">{"Properties"}</caption>
                <TableHeader>
                  <TableRow className="bg-accent/30">
                    <TableHead className="w-10"><Checkbox checked={selectedIds.length === filtered.length && filtered.length > 0} onCheckedChange={toggleSelectAll} /></TableHead>
                    <TableHead className="w-10"></TableHead>
                    <TableHead className="w-16">{"Image"}</TableHead>
                    <TableHead>{"Code"}</TableHead>
                    <TableHead>{"Title"}</TableHead>
                    <TableHead>{"Type"}</TableHead>
                    <TableHead>{"Listing"}</TableHead>
                    <TableHead>{"Price"}</TableHead>
                    <TableHead>{"Location"}</TableHead>
                    <TableHead className="w-10"><span aria-label={"Featured"}>⭐</span></TableHead>
                    <TableHead className="w-20">{"Actions"}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filtered.map(p => (
                    <SortableRow
                      key={p.id} property={p}
                      selected={selectedIds.includes(p.id)}
                      onToggle={() => toggleSelect(p.id)}
                      onEdit={() => openEdit(p)}
                      onDelete={() => setDeleteId(p.id)}
                      onToggleFeatured={async () => {
                        const apiId = parseInt(p.id, 10);
                        if (!isNaN(apiId)) {
                          try { await adminApi.updateProperty(apiId, { isFeatured: !p.isFeatured }); } catch { /* skip */ }
                        }
                        toggleFeatured(p.id);
                        toast.success("Featured status toggled");
                      }}
                      onView={() => handleViewDetails(p.id)}
                    />
                  ))}
                </TableBody>
              </Table>
            </SortableContext>
          </DndContext>
        </div>
      )}

      {/* Create/Edit Form Dialog */}
      <Dialog open={isFormOpen} onOpenChange={(open) => { if (!open) { clearPending(); setRemovedImageIds([]); setImageIdByUrl({}); setNewContactMode(false); setNewContactName(''); setNewContactPhone(''); setNewContactType('Owner'); } setIsFormOpen(open); }}>
        <DialogContent className="sm:max-w-[900px] lg:max-w-[1000px] max-h-[90vh] overflow-y-auto overflow-x-hidden bg-card">
          <DialogHeader>
            <DialogTitle>{editingId ? "Update Property" : "Create Property"}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            {/* Bilingual titles */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="prop-title-en">{"Title (EN)"}</Label>
                <Input id="prop-title-en" autoComplete="off" value={form.titleEn} onChange={(e) => setForm({ ...form, titleEn: e.target.value })} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="prop-title-ar">{"Title (AR)"}</Label>
                <Input id="prop-title-ar" autoComplete="off" dir="rtl" value={form.titleAr} onChange={(e) => setForm({ ...form, titleAr: e.target.value })} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="prop-desc-en">{"Description (EN)"}</Label>
                <Textarea id="prop-desc-en" value={form.descriptionEn} onChange={(e) => setForm({ ...form, descriptionEn: e.target.value })} rows={5} className="min-h-[120px]" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="prop-desc-ar">{"Description (AR)"}</Label>
                <Textarea id="prop-desc-ar" dir="rtl" value={form.descriptionAr} onChange={(e) => setForm({ ...form, descriptionAr: e.target.value })} rows={5} className="min-h-[120px]" />
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="prop-type">{"Property Type"}</Label>
                <Select value={form.propertyType} onValueChange={(v) => setForm({ ...form, propertyType: v as PropertyType })}>
                  <SelectTrigger id="prop-type"><SelectValue /></SelectTrigger>
                  <SelectContent>{PROPERTY_TYPES.map(t => <SelectItem key={t.value} value={t.value}>{t.label}</SelectItem>)}</SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="prop-listing-type">{"Listing Type"}</Label>
                <Select value={form.listingType} onValueChange={(v) => setForm({ ...form, listingType: v as ListingType })}>
                  <SelectTrigger id="prop-listing-type"><SelectValue /></SelectTrigger>
                  <SelectContent>{LISTING_TYPES.map(t => <SelectItem key={t.value} value={t.value}>{t.label}</SelectItem>)}</SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="prop-currency">{"Currency"}</Label>
                <Input id="prop-currency" autoComplete="off" value={form.currency} onChange={(e) => setForm({ ...form, currency: e.target.value })} />
              </div>

              {form.listingType === 'Rental' ? (
                <div className="space-y-2">
                  <Label htmlFor="prop-rent">{"Rent / month"}</Label>
                  <Input id="prop-rent" autoComplete="off" type="text" inputMode="numeric" value={fmtNum(form.rentPerMonth)} onChange={(e) => setForm({ ...form, rentPerMonth: stripNum(e.target.value) })} />
                </div>
              ) : (
                <div className="space-y-2">
                  <Label htmlFor="prop-price">{"Price"}</Label>
                  <Input id="prop-price" autoComplete="off" type="text" inputMode="numeric" value={fmtNum(form.price)} onChange={(e) => setForm({ ...form, price: stripNum(e.target.value) })} />
                </div>
              )}
              <div className="space-y-2">
                <Label htmlFor="prop-size">{"Size (sqm)"}</Label>
                <Input id="prop-size" autoComplete="off" type="number" value={form.size} onChange={(e) => setForm({ ...form, size: e.target.value })} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="prop-project">{"Project (Optional)"}</Label>
                <Select value={form.projectId || "none"} onValueChange={(v) => setForm({ ...form, projectId: v === "none" ? null : v })}>
                  <SelectTrigger id="prop-project"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">{"Standalone"}</SelectItem>
                    {projects.map(pr => <SelectItem key={pr.id} value={pr.id}>{pr.name}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div className="col-span-full space-y-2">
                <Label htmlFor="alp-governorate">{"Location"} *</Label>
                <AdminLocationPicker
                  governorate={form.governorate}
                  city={form.city}
                  area={form.area}
                  onChange={(gov, cty, are, combined) => setForm({ ...form, governorate: gov, city: cty, area: are, location: combined })}
                />
              </div>
              <div className="col-span-full space-y-2">
                <Label htmlFor="pp-governorateAr">{"الموقع (Arabic)"}</Label>
                <div className="grid grid-cols-3 gap-2" dir="rtl">
                  <Input id="pp-governorateAr" name="governorateAr" value={form.governorateAr} onChange={e => setForm({...form, governorateAr: e.target.value})} placeholder="المحافظة" />
                  <Input id="pp-cityAr" name="cityAr" value={form.cityAr} onChange={e => setForm({...form, cityAr: e.target.value})} placeholder="المدينة" />
                  <Input id="pp-areaAr" name="areaAr" value={form.areaAr} onChange={e => setForm({...form, areaAr: e.target.value})} placeholder="المنطقة" />
                </div>
              </div>
            </div>

            {/* Real Estate Details */}
            <div className="border border-border rounded-xl p-4 space-y-3">
              <p className="text-base font-semibold">{"Property Details"}</p>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="prop-bedrooms">{"Bedrooms"}</Label>
                  <Input id="prop-bedrooms" autoComplete="off" type="number" min={0} max={20} value={form.bedrooms} onChange={(e) => setForm({ ...form, bedrooms: e.target.value })} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="prop-bathrooms">{"Bathrooms"}</Label>
                  <Input id="prop-bathrooms" autoComplete="off" type="number" min={0} max={20} value={form.bathrooms} onChange={(e) => setForm({ ...form, bathrooms: e.target.value })} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="prop-floor">{"Floor"}</Label>
                  <Input id="prop-floor" autoComplete="off" type="number" min={0} value={form.floor} onChange={(e) => setForm({ ...form, floor: e.target.value })} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="prop-total-floors">{"Total Floors"}</Label>
                  <Input id="prop-total-floors" autoComplete="off" type="number" min={0} value={form.totalFloors} onChange={(e) => setForm({ ...form, totalFloors: e.target.value })} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="prop-view">{"View"}</Label>
                  <Select value={form.view} onValueChange={(v) => setForm({ ...form, view: v })}>
                    <SelectTrigger id="prop-view"><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {PROPERTY_VIEWS.map(v => <SelectItem key={v} value={v}>{v}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex items-end pb-2">
                  <div className="flex items-center gap-2">
                    <Switch id="prop-furnished" checked={form.isFurnished} onCheckedChange={(v) => setForm({ ...form, isFurnished: v })} />
                    <Label htmlFor="prop-furnished">{"Furnished"}</Label>
                  </div>
                </div>
              </div>
            </div>

            {/* Features */}
            <div className="space-y-2">
              <Label htmlFor="prop-features">{"Features"}</Label>
              <div className="flex gap-2">
                <Input id="prop-features" autoComplete="off" value={form.featureInput} onChange={(e) => setForm({ ...form, featureInput: e.target.value })}
                  placeholder={"Type and press Enter"} onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addFeature())} />
                <Button type="button" variant="outline" size="sm" onClick={addFeature}>{"Add feature"}</Button>
              </div>
              <div className="flex flex-wrap gap-1.5">
                {form.features.map((f, i) => (
                  <Badge key={f + '-' + i} variant="secondary" className="gap-1">
                    {f}
                    <button onClick={() => removeFeature(i)} aria-label={"Delete"}><X className="w-3 h-3" /></button>
                  </Badge>
                ))}
              </div>
            </div>

            {/* Features Arabic */}
            <div className="space-y-2">
              <Label htmlFor="prop-features-ar">{"المميزات (Arabic)"}</Label>
              <div className="flex gap-2" dir="rtl">
                <Input id="prop-features-ar" autoComplete="off" value={form.featuresArInput} onChange={(e) => setForm({ ...form, featuresArInput: e.target.value })}
                  placeholder={"Type and press Enter"} onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addFeatureAr())} />
                <Button type="button" variant="outline" size="sm" onClick={addFeatureAr}>{"Add feature"}</Button>
              </div>
              <div className="flex flex-wrap gap-1.5">
                {form.featuresAr.map((f, i) => (
                  <Badge key={f + '-' + i} variant="secondary" className="gap-1">
                    {f}
                    <button onClick={() => removeFeatureAr(i)} aria-label={"Delete"}><X className="w-3 h-3" /></button>
                  </Badge>
                ))}
              </div>
            </div>

            {/* Additional Details */}
            <div className="border border-border rounded-xl p-4 space-y-3">
              <p className="text-base font-semibold">{"Additional Details"}</p>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                <div className="flex items-end pb-2">
                  <div className="flex items-center gap-2">
                    <Switch id="prop-recommended" checked={form.isRecommended} onCheckedChange={(v) => setForm({ ...form, isRecommended: v })} />
                    <Label htmlFor="prop-recommended">{"Recommended"}</Label>
                  </div>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="prop-delivery-text">{"Delivery Text"}</Label>
                  <Input id="prop-delivery-text" autoComplete="off" value={form.deliveryText} onChange={(e) => setForm({ ...form, deliveryText: e.target.value })} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="prop-delivery-text-ar">{"Delivery Text (AR)"}</Label>
                  <Input id="prop-delivery-text-ar" autoComplete="off" dir="rtl" value={form.deliveryTextAr} onChange={(e) => setForm({ ...form, deliveryTextAr: e.target.value })} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="prop-construction-status">{"Construction Status"}</Label>
                  <Select value={form.constructionStatus} onValueChange={(v) => setForm({ ...form, constructionStatus: v })}>
                    <SelectTrigger id="prop-construction-status"><SelectValue placeholder={"Select..."} /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Under Construction">{"Under Construction"}</SelectItem>
                      <SelectItem value="Ready to Move">{"Ready to Move"}</SelectItem>
                      <SelectItem value="Completed">{"Completed"}</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="prop-availability-status">{"Availability Status"}</Label>
                  <Select value={form.availabilityStatus} onValueChange={(v) => setForm({ ...form, availabilityStatus: v })}>
                    <SelectTrigger id="prop-availability-status"><SelectValue placeholder={"Select..."} /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Available">{"Available"}</SelectItem>
                      <SelectItem value="Sold">{"Sold"}</SelectItem>
                      <SelectItem value="Reserved">{"Reserved"}</SelectItem>
                      <SelectItem value="Under Offer">{"Under Offer"}</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="prop-ownership-type">{"Ownership Type"}</Label>
                  <Input id="prop-ownership-type" autoComplete="off" value={form.ownershipType} onChange={(e) => setForm({ ...form, ownershipType: e.target.value })} />
                </div>
              </div>
            </div>

            {/* Highlights Arabic */}
            <div className="space-y-2">
              <Label htmlFor="prop-highlights-ar">{"Highlights (AR)"}</Label>
              <div className="flex gap-2" dir="rtl">
                <Input id="prop-highlights-ar" autoComplete="off" value={form.highlightsArInput} onChange={(e) => setForm({ ...form, highlightsArInput: e.target.value })}
                  placeholder={"Type and press Enter"} onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addHighlightAr())} />
                <Button type="button" variant="outline" size="sm" onClick={addHighlightAr}>{"Add"}</Button>
              </div>
              <div className="flex flex-wrap gap-1.5">
                {form.highlightsAr.map((f, i) => (
                  <Badge key={f + '-' + i} variant="secondary" className="gap-1">
                    {f}
                    <button onClick={() => removeHighlightAr(i)} aria-label={"Delete"}><X className="w-3 h-3" /></button>
                  </Badge>
                ))}
              </div>
            </div>

            {/* Nearby Places */}
            <div className="space-y-2">
              <Label htmlFor="prop-nearby">{"Nearby Places"}</Label>
              <div className="flex gap-2">
                <Input id="prop-nearby" autoComplete="off" value={form.nearbyPlaceInput} onChange={(e) => setForm({ ...form, nearbyPlaceInput: e.target.value })}
                  placeholder={"Type and press Enter"} onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addNearbyPlace())} />
                <Button type="button" variant="outline" size="sm" onClick={addNearbyPlace}>{"Add"}</Button>
              </div>
              <div className="flex flex-wrap gap-1.5">
                {form.nearbyPlaces.map((f, i) => (
                  <Badge key={f + '-' + i} variant="secondary" className="gap-1">
                    {f}
                    <button onClick={() => removeNearbyPlace(i)} aria-label={"Delete"}><X className="w-3 h-3" /></button>
                  </Badge>
                ))}
              </div>
            </div>

            {/* Nearby Places Arabic */}
            <div className="space-y-2">
              <Label htmlFor="prop-nearby-ar">{"Nearby Places (AR)"}</Label>
              <div className="flex gap-2" dir="rtl">
                <Input id="prop-nearby-ar" autoComplete="off" value={form.nearbyPlaceArInput} onChange={(e) => setForm({ ...form, nearbyPlaceArInput: e.target.value })}
                  placeholder={"Type and press Enter"} onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addNearbyPlaceAr())} />
                <Button type="button" variant="outline" size="sm" onClick={addNearbyPlaceAr}>{"Add"}</Button>
              </div>
              <div className="flex flex-wrap gap-1.5">
                {form.nearbyPlacesAr.map((f, i) => (
                  <Badge key={f + '-' + i} variant="secondary" className="gap-1">
                    {f}
                    <button onClick={() => removeNearbyPlaceAr(i)} aria-label={"Delete"}><X className="w-3 h-3" /></button>
                  </Badge>
                ))}
              </div>
            </div>

            {/* Installments (array) */}
            {form.listingType !== 'Rental' && (
              <div className="border border-border rounded-xl p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <p className="text-base font-semibold">{"Installment Plans"}</p>
                  <div className="flex gap-2">
                    <Button type="button" variant="outline" size="sm" onClick={addInstallment}>
                      <Plus className="w-3 h-3 mr-1" /> {"Installment"}
                    </Button>
                    <Button type="button" variant="outline" size="sm" onClick={addCashOption}>
                      <Plus className="w-3 h-3 mr-1" /> {"Cash"}
                    </Button>
                  </div>
                </div>
                {form.installments.length === 0 ? (
                  <p className="text-sm text-muted-foreground">{"No installment plans. Add one to enable financing."}</p>
                ) : (
                  <div className="space-y-2">
                    {form.installments.map((inst, i) => {
                      const monthly = inst.paymentType === 'Cash'
                        ? 0
                        : form.price && inst.downPaymentPercent && inst.years
                          ? Math.round((Number(form.price) * (1 - Number(inst.downPaymentPercent) / 100)) / (Number(inst.years) * 12))
                          : 0;
                      const cashPrice = inst.paymentType === 'Cash' && form.price && inst.discountPercent
                        ? Math.round(Number(form.price) * (1 - Number(inst.discountPercent) / 100))
                        : 0;
                      return (
                        <div key={`prop-inst-${i}`} className="border border-border rounded-lg p-3 space-y-2">
                          <div className="flex items-center justify-between">
                            <Badge variant={inst.paymentType === 'Cash' ? 'secondary' : 'default'}>
                              {inst.paymentType === 'Cash' ? 'Cash' : 'Installment'}
                            </Badge>
                            <div className="flex items-center gap-2">
                              <Switch id={"inst-enabled-" + i} checked={inst.isEnabled} onCheckedChange={(v) => updateInstallment(i, { isEnabled: v })} />
                              <Button type="button" variant="ghost" size="icon" className="text-destructive h-8 w-8" onClick={() => removeInstallment(i)} aria-label={"Delete"}>
                                <Trash2 className="w-4 h-4" />
                              </Button>
                            </div>
                          </div>
                          <div className="grid grid-cols-2 gap-2">
                            <div className="space-y-1">
                              <Label htmlFor={`inst-type-${i}`} className="text-xs">{"Type"}</Label>
                              <Select value={inst.paymentType} onValueChange={(v: 'Installment' | 'Cash') => updateInstallment(i, { paymentType: v, downPaymentPercent: v === 'Cash' ? '100' : '10', years: v === 'Cash' ? '0' : '5', discountPercent: v === 'Cash' ? '20' : '' })}>
                                <SelectTrigger id={`inst-type-${i}`}><SelectValue placeholder="Select" /></SelectTrigger>
                                <SelectContent>
                                  <SelectItem value="Installment">Installment</SelectItem>
                                  <SelectItem value="Cash">Cash</SelectItem>
                                </SelectContent>
                              </Select>
                            </div>
                            {inst.paymentType === 'Cash' ? (
                              <div className="space-y-1">
                                <Label htmlFor={`inst-discount-${i}`} className="text-xs">{"Discount %"}</Label>
                                <Input id={`inst-discount-${i}`} autoComplete="off" type="number" value={inst.discountPercent} onChange={(e) => updateInstallment(i, { discountPercent: e.target.value })} />
                              </div>
                            ) : (
                              <>
                                <div className="space-y-1">
                                  <Label htmlFor={`inst-down-${i}`} className="text-xs">{"Down %"}</Label>
                                  <Input id={`inst-down-${i}`} autoComplete="off" type="number" value={inst.downPaymentPercent} onChange={(e) => updateInstallment(i, { downPaymentPercent: e.target.value })} />
                                </div>
                                <div className="space-y-1">
                                  <Label htmlFor={`inst-years-${i}`} className="text-xs">{"Years"}</Label>
                                  <Input id={`inst-years-${i}`} autoComplete="off" type="number" value={inst.years} onChange={(e) => updateInstallment(i, { years: e.target.value })} />
                                </div>
                                <div className="space-y-1">
                                  <Label htmlFor={`inst-monthly-${i}`} className="text-xs">{"Monthly"} ({form.currency})</Label>
                                  <Input id={`inst-monthly-${i}`} autoComplete="off" type="number" value={inst.monthlyAmount || (monthly || '')} onChange={(e) => updateInstallment(i, { monthlyAmount: e.target.value })} placeholder={monthly ? String(monthly) : ''} />
                                </div>
                              </>
                            )}
                          </div>
                          {inst.paymentType === 'Cash' && cashPrice > 0 && (
                            <p className="text-sm text-muted-foreground">
                              Cash price: <strong>{cashPrice.toLocaleString()} {form.currency}</strong> (after {inst.discountPercent}% discount)
                            </p>
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            )}

            {/* Contact */}
            <div className="border border-border rounded-xl p-4 space-y-3">
              <div className="flex items-center justify-between">
                <p className="text-base font-semibold">{"Contact"}</p>
                <Button type="button" variant="outline" size="sm" onClick={() => { setNewContactMode(!newContactMode); if (!newContactMode) setForm({ ...form, contactId: '' }); }}>
                  {newContactMode ? "Select contact" : "+ New contact"}
                </Button>
              </div>
              {newContactMode ? (
                <div className="space-y-3">
                  <div>
                    <Label htmlFor="new-contact-name">{"Name"}</Label>
                    <Input id="new-contact-name" autoComplete="name" value={newContactName} onChange={e => setNewContactName(e.target.value)} placeholder={"Name"} />
                  </div>
                  <div>
                    <Label htmlFor="new-contact-phone">{"Phone"}</Label>
                    <Input id="new-contact-phone" autoComplete="tel" value={newContactPhone} onChange={e => setNewContactPhone(e.target.value)} placeholder={"Phone"} />
                  </div>
                  <div>
                    <Label htmlFor="new-contact-type">{"Contact Type"}</Label>
                    <Select value={newContactType} onValueChange={(v: 'Owner' | 'Broker') => setNewContactType(v)}>
                      <SelectTrigger id="new-contact-type"><SelectValue /></SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Owner">{"Owner"}</SelectItem>
                        <SelectItem value="Broker">{"Broker"}</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              ) : (
                <Select value={form.contactId} onValueChange={(v) => setForm({ ...form, contactId: v })}>
                  <SelectTrigger id="prop-contact"><SelectValue placeholder={"Select contact"} /></SelectTrigger>
                  <SelectContent>
                    {contacts.map(c => <SelectItem key={c.id} value={c.id}>{c.name} ({c.type}) - {c.phone}</SelectItem>)}
                  </SelectContent>
                </Select>
              )}
            </div>

            {/* Images */}
            <div className="space-y-2">
              <p className="text-sm font-medium">{"Images"}</p>
              <div className="flex flex-wrap gap-2">
                {form.images.map((img) => (
                  <div key={img} className="relative group w-20 h-20 rounded-xl overflow-hidden border border-border">
                    <img src={img} alt="" loading="lazy" width={80} height={80} className="w-full h-full object-cover" />
                    <button type="button" onClick={() => {
                      const id = imageIdByUrl[img];
                      if (id) setRemovedImageIds(prev => [...prev, id]);
                      setForm({ ...form, images: form.images.filter((u) => u !== img) });
                    }} aria-label={"Delete"}
                      className="absolute inset-0 bg-background/60 opacity-0 group-hover:opacity-100 flex items-center justify-center transition-opacity">
                      <X className="w-4 h-4 text-destructive" />
                    </button>
                  </div>
                ))}
                {pendingPreviews.map((url) => (
                  <div key={url} className="relative group w-20 h-20 rounded-xl overflow-hidden border border-dashed border-primary">
                    <img src={url} alt="" loading="lazy" width={80} height={80} className="w-full h-full object-cover" />
                    <button type="button" onClick={() => removePending(pendingPreviews.indexOf(url))} aria-label={"Delete"}
                      className="absolute inset-0 bg-background/60 opacity-0 group-hover:opacity-100 flex items-center justify-center transition-opacity">
                      <X className="w-4 h-4 text-destructive" />
                    </button>
                    <span className="absolute bottom-1 left-1 text-[8px] bg-primary/80 text-primary-foreground px-1 rounded">{"New Image"}</span>
                  </div>
                ))}
                <button type="button" onClick={() => fileRef.current?.click()} aria-label="Upload Image"
                  className="w-20 h-20 rounded-xl border-2 border-dashed border-border hover:border-primary flex flex-col items-center justify-center text-muted-foreground hover:text-primary transition-colors">
                  <ImagePlus className="w-5 h-5" />
                  <span className="text-[10px] mt-1">{"Upload Image"}</span>
                </button>
              </div>
              <input ref={fileRef} id="pp-imageUpload" name="imageUpload" type="file" accept="image/*" multiple className="hidden" onChange={(e) => handleImageUpload(e.target.files)} aria-hidden="true" />
            </div>

            {/* Videos */}
            <VideoUploadZone
              key={editingId || 'create'}
              entityType="properties"
              entityId={editingId ? parseInt(editingId, 10) : null}
              existingVideos={existingVideos}
              onVideoAdded={(v) => {
                setExistingVideos(prev => {
                  const next = [...prev, v];
                  if (editingId) updateProperty(editingId, { videos: next });
                  return next;
                });
              }}
              onVideoRemoved={(videoId) => {
                setExistingVideos(prev => {
                  const next = prev.filter(x => x.id !== videoId);
                  if (editingId) updateProperty(editingId, { videos: next });
                  return next;
                });
              }}
            />

            {/* SEO accordion */}
            <Accordion type="single" collapsible>
              <AccordionItem value="seo">
                <AccordionTrigger className="text-sm">{"SEO & Slug"}</AccordionTrigger>
                <AccordionContent className="space-y-3 pt-2">
                  <div className="flex items-center justify-between">
                    <Label htmlFor="prop-auto-slug" className="text-xs">{"Auto-generate slug from title"}</Label>
                    <Switch id="prop-auto-slug" checked={form.slugIsAuto} onCheckedChange={(v) => setForm({ ...form, slugIsAuto: v })} />
                  </div>
                  <div className="space-y-1">
                    <Label htmlFor="prop-slug" className="text-xs">{"Slug"}</Label>
                    <Input id="prop-slug" autoComplete="off"
                      value={form.slugIsAuto ? autoSlug(form.titleEn) : form.slug}
                      disabled={form.slugIsAuto}
                      onChange={(e) => setForm({ ...form, slug: e.target.value })}
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1"><Label htmlFor="prop-seo-title-en" className="text-xs">{"SEO Title (EN)"}</Label><Input id="prop-seo-title-en" autoComplete="off" value={form.seoTitle} onChange={(e) => setForm({ ...form, seoTitle: e.target.value })} placeholder={"Auto-generate SEO from description"} /></div>
                    <div className="space-y-1"><Label htmlFor="prop-seo-title-ar" className="text-xs">{"SEO Title (AR)"}</Label><Input id="prop-seo-title-ar" autoComplete="off" dir="rtl" value={form.seoTitleAr} onChange={(e) => setForm({ ...form, seoTitleAr: e.target.value })} /></div>
                    <div className="space-y-1"><Label htmlFor="prop-seo-desc-en" className="text-xs">{"SEO Description (EN)"}</Label><Textarea id="prop-seo-desc-en" value={form.seoDescription} onChange={(e) => setForm({ ...form, seoDescription: e.target.value })} rows={2} /></div>
                    <div className="space-y-1"><Label htmlFor="prop-seo-desc-ar" className="text-xs">{"SEO Description (AR)"}</Label><Textarea id="prop-seo-desc-ar" dir="rtl" value={form.seoDescriptionAr} onChange={(e) => setForm({ ...form, seoDescriptionAr: e.target.value })} rows={2} /></div>
                    <div className="space-y-1"><Label htmlFor="prop-seo-keywords-en" className="text-xs">{"Keywords (EN)"}</Label><Input id="prop-seo-keywords-en" autoComplete="off" value={form.seoKeywords} onChange={(e) => setForm({ ...form, seoKeywords: e.target.value })} placeholder={"Comma-separated keywords"} /></div>
                    <div className="space-y-1"><Label htmlFor="prop-seo-keywords-ar" className="text-xs">{"Keywords (AR)"}</Label><Input id="prop-seo-keywords-ar" autoComplete="off" dir="rtl" value={form.seoKeywordsAr} onChange={(e) => setForm({ ...form, seoKeywordsAr: e.target.value })} /></div>
                  </div>
                  <p className="text-[11px] text-muted-foreground">{"Note: backend auto-generates SEO when fields are empty."}</p>
                </AccordionContent>
              </AccordionItem>
            </Accordion>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => { setNewContactMode(false); setNewContactName(''); setNewContactPhone(''); setNewContactType('Owner'); setIsFormOpen(false); }}>{"Cancel"}</Button>
            <Button onClick={handleSubmit} disabled={isSubmitting} className="bg-primary hover:bg-primary/90">
              {editingId ? "Update Property" : "Create Property"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <AdminPropertyDetailDialog key={detailProperty?.id} property={detailProperty} open={!!detailProperty} onOpenChange={(o) => !o && setDetailProperty(null)} />

      <AlertDialog open={!!deleteId} onOpenChange={(open) => !open && setDeleteId(null)}>
        <AlertDialogContent className="bg-card">
          <AlertDialogHeader>
            <AlertDialogTitle>{"Delete Property?"}</AlertDialogTitle>
            <AlertDialogDescription>{"This action cannot be undone."}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{"Cancel"}</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-destructive hover:bg-destructive/90">{"Delete"}</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
