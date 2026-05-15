/* eslint-disable @typescript-eslint/no-explicit-any */
import { useState, useRef, useMemo, useEffect } from "react";
import { useStore, apiPropertyToStore, type Property, type Project, type PropertyType, type ListingType } from "@/store";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { CONTACT_TYPE_MAP } from "@/lib/constants";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
  AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Plus, MapPin, Building, ArrowLeft, Loader2, Languages } from "lucide-react";
import { UserProjectCard } from "@/components/projects/UserProjectCard";
import { SortableProjectCard } from "@/components/projects/SortableProjectCard";
import { SortableUnitCard } from "@/components/projects/SortableUnitCard";
import { AddProjectDialog } from "@/components/projects/AddProjectDialog";
import { UnitDialog } from "@/components/projects/UnitDialog";
import { ProjectDetailDialog } from "@/components/projects/ProjectDetailDialog";
import { PropertyDetailDialog } from "@/components/properties/PropertyDetailDialog";
import { AdminPropertyDetailDialog } from "@/components/properties/AdminPropertyDetailDialog";
import { UserPropertyCard } from "@/components/properties/UserPropertyCard";
import { VideoUploadZone, type VideoItem } from "@/components/VideoUploadZone";
import { autoSlug } from "@/lib/utils";
import {
  DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors, type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext, sortableKeyboardCoordinates, rectSortingStrategy,
} from "@dnd-kit/sortable";

export default function ProjectsPage() {
  const projects = useStore(s => s.projects);
  const properties = useStore(s => s.properties);
  const units = useStore(s => s.units);
  const contacts = useStore(s => s.contacts);
  const previewMode = useStore(s => s.previewMode);
  const addProject = useStore(s => s.addProject);
  const updateProject = useStore(s => s.updateProject);
  const deleteProject = useStore(s => s.deleteProject);
  const updateProperty = useStore(s => s.updateProperty);
  const deleteUnit = useStore(s => s.deleteUnit);
  const reorderUnits = useStore(s => s.reorderUnits);
  const reorderProjects = useStore(s => s.reorderProjects);
  const loadProjects = useStore(s => s.loadProjects);
  const loadProperties = useStore(s => s.loadProperties);
  const loadUnits = useStore(s => s.loadUnits);
  const loadContacts = useStore(s => s.loadContacts);


  const [initialLoading, setInitialLoading] = useState(true);

  useEffect(() => {
    setInitialLoading(true);
    (async () => {
      await loadProjects();
      await Promise.all([loadProperties(), loadUnits(), loadContacts()]);
    })().finally(() => setInitialLoading(false));
  }, [loadProjects, loadProperties, loadUnits, loadContacts]);
  const [selectedProjectId, setSelectedProjectId] = useState<string | null>(null);
  const [isAddProjectOpen, setIsAddProjectOpen] = useState(false);
  const [isEditProjectOpen, setIsEditProjectOpen] = useState(false);
  const [isAddUnitOpen, setIsAddUnitOpen] = useState(false);
  const [deleteProjectId, setDeleteProjectId] = useState<string | null>(null);
  const [editingProject, setEditingProject] = useState<Project | null>(null);
  const [editingUnitId, setEditingUnitId] = useState<string | null>(null);
  const [deleteUnitId, setDeleteUnitId] = useState<string | null>(null);
  const [unitSearch, setUnitSearch] = useState('');

  // Video state for projects
  const [projectExistingVideos, setProjectExistingVideos] = useState<VideoItem[]>([]);
  // Video state for units
  const [unitExistingVideos, setUnitExistingVideos] = useState<VideoItem[]>([]);

  const defaultProjectForm = {
    nameEn: '', nameAr: '',
    descriptionEn: '', descriptionAr: '',
    location: '', locationAr: '', developer: '', unitCount: '0',
    image: '',
    highlights: [] as string[], highlightInput: '',
    highlightsAr: [] as string[], highlightsArInput: '',
    startingPrice: '',
    nearbyPlaces: [] as string[], nearbyPlaceInput: '',
    nearbyPlacesAr: [] as string[], nearbyPlaceArInput: '',
    propertyTypes: [] as string[],
    latitude: '',
    longitude: '',
    totalArea: '',
    ownershipType: '',
    deliveryText: '',
    deliveryTextAr: '',
    isRecommended: false,
    constructionStatus: '',
    availabilityStatus: 'Available',
    virtualTourUrl: '',
    slug: '', slugIsAuto: true,
    seoTitle: '', seoDescription: '', seoKeywords: '',
    seoTitleAr: '', seoDescriptionAr: '', seoKeywordsAr: '',
  };
  const [projectForm, setProjectForm] = useState(defaultProjectForm);

  const [detailProject, setDetailProject] = useState<Project | null>(null);
  const [detailUnit, setDetailUnit] = useState<Property | null>(null);
  const [previewProjectLang, setPreviewProjectLang] = useState<'en' | 'ar'>('en');
  const [fullProject, setFullProject] = useState<any | null>(null);

  useEffect(() => {
    let ignore = false;
    if (!previewMode || !selectedProjectId) return;
    const apiId = parseInt(selectedProjectId, 10);
    if (!isNaN(apiId)) {
      adminApi.getProject(apiId).then(data => { if (!ignore) setFullProject(data); }).catch(() => { if (!ignore) { setFullProject(null); toast.error("Failed to load project details"); } });
    } else {
      setFullProject(null);
    }
    return () => { ignore = true; };
  }, [previewMode, selectedProjectId]);

  const defaultUnitForm = {
    titleEn: '', titleAr: '',
    descriptionEn: '', descriptionAr: '',
    rentPerMonth: '', currency: 'EGP',
    location: '', locationAr: '',
    governorate: '', city: '', area: '',
    governorateAr: '', cityAr: '', areaAr: '',
    bedrooms: '', bathrooms: '', floor: '',
    isFurnished: false, view: 'Unknown',
    unitNumber: '', buildingNumber: '', deliveryDate: '',
    finishingType: '', hasBalcony: false, hasParking: false,
    propertyType: 'Apartment' as PropertyType, listingType: 'Project' as ListingType,
    features: [] as string[], featuresAr: [] as string[], featuresInput: '', featuresArInput: '',
    contactId: '', images: [] as string[],
    installments: [] as { downPaymentPercent: string; years: string; monthlyAmount: string; isEnabled: boolean }[],
    variants: [] as { name: string; nameAr?: string; size: string; price: string; currency: string; rentPerMonth: string; bedrooms: string; bathrooms: string; floor: string; isFurnished: boolean; view: string; unitNumber: string; buildingNumber: string; deliveryDate: string; finishingType: string; hasBalcony: boolean; hasParking: boolean; floorPlanUrl: string; availabilityStatus: string; sortOrder: string; isActive: boolean; isFeatured?: boolean; isRecommended?: boolean; deliveryText?: string }[],
    isRecommended: false,
    deliveryText: '',
    constructionStatus: '',
    availabilityStatus: 'Available',
    ownershipType: '',
    virtualTourUrl: '',
    highlightsAr: [] as string[],
    nearbyPlaces: [] as string[],
    nearbyPlacesAr: [] as string[],
    slug: '', slugIsAuto: true,
    seoTitle: '', seoDescription: '', seoKeywords: '',
    seoTitleAr: '', seoDescriptionAr: '', seoKeywordsAr: '',
  };
  const [unitForm, setUnitForm] = useState(defaultUnitForm);
  const [unitPendingFiles, setUnitPendingFiles] = useState<File[]>([]);
  const [unitPendingPreviews, setUnitPendingPreviews] = useState<string[]>([]);
  const [unitNewContact, setUnitNewContact] = useState(false);
  const [unitNewContactName, setUnitNewContactName] = useState('');
  const [unitNewContactPhone, setUnitNewContactPhone] = useState('');
  const [unitNewContactType, setUnitNewContactType] = useState<'Owner' | 'Broker'>('Owner');
  const [unitImageIdByUrl, setUnitImageIdByUrl] = useState<Record<string, number>>({});
  const [unitRemovedImageIds, setUnitRemovedImageIds] = useState<number[]>([]);
  const [isUnitSubmitting, setIsUnitSubmitting] = useState(false);

  const addUnitInstallment = () => setUnitForm(prev => ({
    ...prev, installments: [...prev.installments, { paymentType: 'Installment' as const, downPaymentPercent: '10', discountPercent: '', years: '5', monthlyAmount: '', isEnabled: true }],
  }));
  const addUnitCashInstallment = () => setUnitForm(prev => ({
    ...prev, installments: [...prev.installments, { paymentType: 'Cash' as const, downPaymentPercent: '100', discountPercent: '20', years: '0', monthlyAmount: '', isEnabled: true }],
  }));
  const updateUnitInstallment = (idx: number, patch: Partial<{ paymentType: 'Installment' | 'Cash'; downPaymentPercent: string; discountPercent: string; years: string; monthlyAmount: string; isEnabled: boolean }>) => setUnitForm(prev => ({
    ...prev, installments: prev.installments.map((i, n) => n === idx ? { ...i, ...patch } : i),
  }));
  const removeUnitInstallment = (idx: number) => setUnitForm(prev => ({
    ...prev, installments: prev.installments.filter((_, i) => i !== idx),
  }));
  const fileRef = useRef<HTMLInputElement>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  const selectedProject = projects.find((p) => p.id === selectedProjectId);
  const sortedProjects = useMemo(() => [...projects].sort((a, b) => a.order - b.order), [projects]);
  const projectUnitsAll = useMemo(
    () => [...units, ...properties].filter((p) => p.projectId === selectedProjectId).sort((a, b) => a.order - b.order),
    [units, properties, selectedProjectId]
  );
  const projectUnits = useMemo(
    () => unitSearch ? projectUnitsAll.filter(u =>
      u.code.toLowerCase().includes(unitSearch.toLowerCase()) ||
      u.title.toLowerCase().includes(unitSearch.toLowerCase()) ||
      u.location.toLowerCase().includes(unitSearch.toLowerCase())
    ) : projectUnitsAll,
    [projectUnitsAll, unitSearch]
  );

  const resetProjectForm = () => setProjectForm(defaultProjectForm);

  const handleAddProject = async () => {
    if (!projectForm.nameEn || !projectForm.nameAr || !projectForm.descriptionEn || !projectForm.location) {
      toast.error("Please fill in all required fields (Name EN/AR, Description EN, Location)");
      return;
    }
    try {
      const slug = projectForm.slugIsAuto ? autoSlug(projectForm.nameEn) : projectForm.slug;
      const startingPrice = projectForm.startingPrice ? Number(projectForm.startingPrice) : undefined;
      const nearbyPlaces = projectForm.nearbyPlaces.length ? projectForm.nearbyPlaces : undefined;
      const nearbyPlacesAr = projectForm.nearbyPlacesAr.length ? projectForm.nearbyPlacesAr : undefined;
      const propertyTypes = projectForm.propertyTypes.length ? projectForm.propertyTypes : undefined;
      const latitude = projectForm.latitude ? Number(projectForm.latitude) : undefined;
      const longitude = projectForm.longitude ? Number(projectForm.longitude) : undefined;
      const totalArea = projectForm.totalArea ? Number(projectForm.totalArea) : undefined;
      const ownershipType = projectForm.ownershipType || undefined;
      const deliveryText = projectForm.deliveryText || undefined;
      const deliveryTextAr = projectForm.deliveryTextAr || undefined;
      const isRecommended = projectForm.isRecommended || undefined;
      const constructionStatus = projectForm.constructionStatus || undefined;
      const availabilityStatus = projectForm.availabilityStatus || undefined;
      const virtualTourUrl = projectForm.virtualTourUrl || undefined;
      await adminApi.createProject({
        nameEn: projectForm.nameEn, nameAr: projectForm.nameAr || projectForm.nameEn,
        descriptionEn: projectForm.descriptionEn, descriptionAr: projectForm.descriptionAr || projectForm.descriptionEn,
        location: projectForm.location, locationAr: projectForm.locationAr || undefined, developer: projectForm.developer,
        image: projectForm.image || undefined,
        highlights: projectForm.highlights, highlightsAr: projectForm.highlightsAr.length ? projectForm.highlightsAr : undefined,
        startingPrice, nearbyPlaces, nearbyPlacesAr, propertyTypes, latitude, longitude, totalArea, ownershipType,
        unitCount: Number(projectForm.unitCount) || 0,
          deliveryText, deliveryTextAr, isRecommended, constructionStatus, availabilityStatus, virtualTourUrl,
        slug,
        seoTitle: projectForm.seoTitle || undefined, seoDescription: projectForm.seoDescription || undefined, seoKeywords: projectForm.seoKeywords || undefined,
        seoTitleAr: projectForm.seoTitleAr || undefined, seoDescriptionAr: projectForm.seoDescriptionAr || undefined, seoKeywordsAr: projectForm.seoKeywordsAr || undefined,
      });
      addProject({
        name: projectForm.nameEn, nameEn: projectForm.nameEn, nameAr: projectForm.nameAr,
        description: projectForm.descriptionEn, descriptionEn: projectForm.descriptionEn, descriptionAr: projectForm.descriptionAr,
        location: projectForm.location, locationAr: projectForm.locationAr || undefined, developer: projectForm.developer,
        unitCount: Number(projectForm.unitCount) || 0,
        image: projectForm.image, highlights: projectForm.highlights, highlightsAr: projectForm.highlightsAr.length ? projectForm.highlightsAr : undefined,
        startingPrice, nearbyPlaces, nearbyPlacesAr, propertyTypes, latitude, longitude, totalArea, ownershipType,
        deliveryText: projectForm.deliveryText || undefined,
        deliveryTextAr: projectForm.deliveryTextAr || undefined,
        isRecommended: projectForm.isRecommended || undefined,
        constructionStatus: projectForm.constructionStatus || undefined,
        availabilityStatus: projectForm.availabilityStatus || undefined,
        virtualTourUrl: projectForm.virtualTourUrl || undefined,
      });
      await loadProjects();
      toast.success("Project created");
      setIsAddProjectOpen(false);
      resetProjectForm();
    } catch (e: any) {
      toast.error(e?.message || "Failed to create project");
    }
  };

  const openEditProject = async (p: Project) => {
    setEditingProject(p);
    setProjectExistingVideos([]);
    const apiId = parseInt(p.id, 10);
    if (!isNaN(apiId)) {
      try {
        const full = await adminApi.getProject(apiId);
        if (Array.isArray(full?.videos)) {
          setProjectExistingVideos(full.videos.map((v: any) => ({ id: v.id, url: v.url, publicId: v.publicId ?? '' })));
        }
        p = { ...p, ...full };
      } catch {       toast.error("Failed to load full project details from server; using list data as fallback"); }
    }
    setProjectForm({
      nameEn: p.nameEn || p.name, nameAr: p.nameAr || '',
      descriptionEn: p.descriptionEn || p.description || '', descriptionAr: p.descriptionAr || '',
      location: p.location, locationAr: (p as any).locationAr || '', developer: p.developer || '', unitCount: String(p.unitCount || 0),
      image: p.image, highlights: p.highlights || [], highlightInput: '',
      highlightsAr: Array.isArray((p as any).highlightsAr) ? (p as any).highlightsAr : [], highlightsArInput: '',
      startingPrice: (p as any).startingPrice != null ? String((p as any).startingPrice) : '',
      nearbyPlaces: Array.isArray((p as any).nearbyPlaces) ? (p as any).nearbyPlaces : [], nearbyPlaceInput: '',
      nearbyPlacesAr: Array.isArray((p as any).nearbyPlacesAr) ? (p as any).nearbyPlacesAr : [], nearbyPlaceArInput: '',
      propertyTypes: Array.isArray((p as any).propertyTypes) ? (p as any).propertyTypes : [],
      latitude: (p as any).latitude != null ? String((p as any).latitude) : '',
      longitude: (p as any).longitude != null ? String((p as any).longitude) : '',
      totalArea: (p as any).totalArea != null ? String((p as any).totalArea) : '',
      ownershipType: (p as any).ownershipType || '',
      deliveryText: (p as any).deliveryText || '',
      deliveryTextAr: (p as any).deliveryTextAr || '',
      isRecommended: (p as any).isRecommended ?? false,
      constructionStatus: (p as any).constructionStatus || '',
      availabilityStatus: (p as any).availabilityStatus || '',
      virtualTourUrl: (p as any).virtualTourUrl || '',
      slug: p.slug || '', slugIsAuto: !p.slug,
      seoTitle: p.seoTitle || '', seoDescription: p.seoDescription || '', seoKeywords: p.seoKeywords || '',
      seoTitleAr: p.seoTitleAr || '', seoDescriptionAr: p.seoDescriptionAr || '', seoKeywordsAr: p.seoKeywordsAr || '',
    });
    setIsEditProjectOpen(true);
  };

  const handleUpdateProject = async () => {
    if (!editingProject) return;
    const projectSlug = projectForm.slugIsAuto ? autoSlug(projectForm.nameEn) : projectForm.slug;
    const startingPrice = projectForm.startingPrice ? Number(projectForm.startingPrice) : undefined;
    const nearbyPlaces = projectForm.nearbyPlaces.length ? projectForm.nearbyPlaces : undefined;
    const nearbyPlacesAr = projectForm.nearbyPlacesAr.length ? projectForm.nearbyPlacesAr : undefined;
    const propertyTypes = projectForm.propertyTypes.length ? projectForm.propertyTypes : undefined;
    const latitude = projectForm.latitude ? Number(projectForm.latitude) : undefined;
    const longitude = projectForm.longitude ? Number(projectForm.longitude) : undefined;
    const totalArea = projectForm.totalArea ? Number(projectForm.totalArea) : undefined;
    const ownershipType = projectForm.ownershipType || undefined;
    const deliveryText = projectForm.deliveryText || undefined;
    const deliveryTextAr = projectForm.deliveryTextAr || undefined;
    const isRecommended = projectForm.isRecommended || undefined;
    const constructionStatus = projectForm.constructionStatus || undefined;
    const availabilityStatus = projectForm.availabilityStatus || undefined;
    const virtualTourUrl = projectForm.virtualTourUrl || undefined;
    updateProject(editingProject.id, {
      name: projectForm.nameEn, nameEn: projectForm.nameEn, nameAr: projectForm.nameAr,
      description: projectForm.descriptionEn, descriptionEn: projectForm.descriptionEn, descriptionAr: projectForm.descriptionAr,
      location: projectForm.location, locationAr: projectForm.locationAr || undefined, developer: projectForm.developer,
      unitCount: Number(projectForm.unitCount) || 0,
      image: projectForm.image, highlights: projectForm.highlights, highlightsAr: projectForm.highlightsAr.length ? projectForm.highlightsAr : undefined,
      startingPrice, nearbyPlaces, nearbyPlacesAr, propertyTypes, latitude, longitude, totalArea, ownershipType,
      deliveryText: projectForm.deliveryText || undefined,
      deliveryTextAr: projectForm.deliveryTextAr || undefined,
      isRecommended: projectForm.isRecommended || undefined,
      constructionStatus: projectForm.constructionStatus || undefined,
      availabilityStatus: projectForm.availabilityStatus || undefined,
      virtualTourUrl: projectForm.virtualTourUrl || undefined,
      slug: projectSlug,
    });
    const apiId = parseInt(editingProject.id, 10);
    if (!isNaN(apiId)) {
      try {
        const slug = projectForm.slugIsAuto ? autoSlug(projectForm.nameEn) : projectForm.slug;
        await adminApi.updateProject(apiId, {
          nameEn: projectForm.nameEn, nameAr: projectForm.nameAr || projectForm.nameEn,
          descriptionEn: projectForm.descriptionEn, descriptionAr: projectForm.descriptionAr || projectForm.descriptionEn,
          location: projectForm.location, locationAr: projectForm.locationAr || undefined, developer: projectForm.developer,
          highlights: projectForm.highlights, highlightsAr: projectForm.highlightsAr.length ? projectForm.highlightsAr : undefined,
        startingPrice, nearbyPlaces, nearbyPlacesAr, propertyTypes, latitude, longitude, totalArea, ownershipType,
        deliveryText, deliveryTextAr, isRecommended, constructionStatus, availabilityStatus, virtualTourUrl,
          unitCount: Number(projectForm.unitCount) || 0,
          image: projectForm.image,
          slug,
          seoTitle: projectForm.seoTitle || undefined, seoDescription: projectForm.seoDescription || undefined, seoKeywords: projectForm.seoKeywords || undefined,
          seoTitleAr: projectForm.seoTitleAr || undefined, seoDescriptionAr: projectForm.seoDescriptionAr || undefined, seoKeywordsAr: projectForm.seoKeywordsAr || undefined,
        });
        toast.success("Project updated");
        setIsEditProjectOpen(false);
        setEditingProject(null);
        resetProjectForm();
      } catch (e: any) {
        toast.error(e?.message || "Failed to update project");
      }
    } else {
      toast.success("Project updated");
      setIsEditProjectOpen(false);
      setEditingProject(null);
      resetProjectForm();
    }
  };

  const handleDeleteProject = async () => {
    if (deleteProjectId) {
      const apiId = parseInt(deleteProjectId, 10);
      if (!isNaN(apiId)) {
        try {
          const detail: any = await adminApi.getProject(apiId);
          if (detail?.videos) {
            for (const v of detail.videos) {
              try { await adminApi.deleteProjectVideo(apiId, v.id); } catch { /* skip individual failures */ }
            }
          }
        } catch { /* fall through to delete entity */ }
        try { await adminApi.deleteProject(apiId); } catch { toast.error("Failed to sync project delete"); }
      }
      deleteProject(deleteProjectId);
      if (selectedProjectId === deleteProjectId) setSelectedProjectId(null);
      toast.success("Project deleted");
      setDeleteProjectId(null);
    }
  };

  const handleAddUnit = async () => {
    if (!unitForm.titleEn || !unitForm.titleAr || !unitForm.descriptionEn || !unitForm.location) {
      toast.error("Please fill in required fields (Title EN/AR, Description EN, Location)");
      return;
    }
    if (unitForm.listingType === 'Rental' && !unitForm.rentPerMonth) {
      toast.error("Please enter a rent amount");
      return;
    }
    if (!unitForm.contactId && !unitNewContact) {
      toast.error("Please select a contact");
      return;
    }
    if (unitNewContact && (!unitNewContactName.trim() || !unitNewContactPhone.trim())) {
      toast.error("Please select a contact");
      return;
    }
    const variantPrices = (unitForm as any).variants?.length ? (unitForm as any).variants.map((v: any) => Number(v.price)).filter((p: number) => p > 0) : [];
    const minPrice = variantPrices.length > 0 ? Math.min(...variantPrices) : 0;
    const maxPrice = variantPrices.length > 0 ? Math.max(...variantPrices) : minPrice;
    const variantSizes = (unitForm as any).variants?.length ? (unitForm as any).variants.map((v: any) => Number(v.size)).filter((s: number) => s > 0) : [];
    const computedMinArea = variantSizes.length > 0 ? Math.min(...variantSizes) : 0;
    const computedMaxArea = variantSizes.length > 0 ? Math.max(...variantSizes) : computedMinArea;
    const selectedContact = contacts.find(c => c.id === unitForm.contactId);
    const contactPayload = unitNewContact
      ? { name: unitNewContactName.trim(), phone: unitNewContactPhone.trim(), type: CONTACT_TYPE_MAP[unitNewContactType] ?? 0 }
      : selectedContact
        ? { name: selectedContact.name, phone: selectedContact.phone, type: CONTACT_TYPE_MAP[selectedContact.type] ?? 0 }
        : undefined;
    const unitInstallments = unitForm.installments
      .filter(i => i.paymentType === 'Cash' || (i.downPaymentPercent && i.years))
      .map(i => i.paymentType === 'Cash' ? ({
        paymentType: 'Cash' as const,
        downPaymentPercent: 100,
        discountPercent: Number(i.discountPercent) || 0,
        years: 0,
        isEnabled: i.isEnabled,
      }) : ({
        paymentType: 'Installment' as const,
        downPaymentPercent: Number(i.downPaymentPercent),
        years: Number(i.years),
        isEnabled: i.isEnabled,
      }));
    setIsUnitSubmitting(true);
    const projectApiId = selectedProjectId ? parseInt(selectedProjectId, 10) : 0;
    if (projectApiId > 0) {
      try {
        const unitSlug = unitForm.slugIsAuto ? autoSlug(unitForm.titleEn) : unitForm.slug;
        const created: any = await adminApi.createUnit({
          titleEn: unitForm.titleEn,
          titleAr: unitForm.titleAr || unitForm.titleEn,
          descriptionEn: unitForm.descriptionEn,
          descriptionAr: unitForm.descriptionAr || unitForm.descriptionEn,
          minPrice, maxPrice,
          rentPerMonth: unitForm.listingType === 'Rental' ? Number(unitForm.rentPerMonth) : null,
          location: unitForm.location, locationAr: [unitForm.governorateAr, unitForm.cityAr, unitForm.areaAr].filter(Boolean).join(', ') || undefined,
          minArea: computedMinArea, maxArea: computedMaxArea,
          bedrooms: unitForm.bedrooms ? Number(unitForm.bedrooms) : null,
          bathrooms: unitForm.bathrooms ? Number(unitForm.bathrooms) : null,
          floor: unitForm.floor ? Number(unitForm.floor) : null,
          isFurnished: unitForm.isFurnished,
          isRecommended: unitForm.isRecommended ?? false,
          view: unitForm.view !== 'Unknown' ? unitForm.view : null,
          unitNumber: unitForm.unitNumber || null,
          buildingNumber: unitForm.buildingNumber || null,
          deliveryDate: unitForm.deliveryDate || null,
          deliveryText: unitForm.deliveryText || null,
          deliveryTextAr: unitForm.deliveryTextAr || null,
          finishingType: unitForm.finishingType || null,
          hasBalcony: unitForm.hasBalcony,
          hasParking: unitForm.hasParking,
          propertyType: unitForm.propertyType,
          listingType: unitForm.listingType,
          constructionStatus: unitForm.constructionStatus || null,
          availabilityStatus: unitForm.availabilityStatus || null,
          ownershipType: unitForm.ownershipType || null,
          virtualTourUrl: unitForm.virtualTourUrl || null,
          highlightsAr: unitForm.highlightsAr?.length ? unitForm.highlightsAr : undefined,
          nearbyPlaces: unitForm.nearbyPlaces?.length ? unitForm.nearbyPlaces : undefined,
          nearbyPlacesAr: unitForm.nearbyPlacesAr?.length ? unitForm.nearbyPlacesAr : undefined,
          projectId: projectApiId, contact: contactPayload,
          features: unitForm.features, featuresAr: unitForm.featuresAr.length ? unitForm.featuresAr : undefined,
          installments: unitInstallments,
          variants: (unitForm as any).variants?.length ? (unitForm as any).variants.map((v: any) => ({
            name: v.name || undefined,
            nameAr: v.nameAr || undefined,
            size: v.size ? Number(v.size) : undefined,
            price: v.price ? Number(v.price.replace(/,/g, '')) : undefined,
            currency: v.currency || undefined,
            rentPerMonth: v.rentPerMonth ? Number(v.rentPerMonth.replace(/,/g, '')) : undefined,
            bedrooms: v.bedrooms ? Number(v.bedrooms) : undefined,
            bathrooms: v.bathrooms ? Number(v.bathrooms) : undefined,
            floor: v.floor ? Number(v.floor) : undefined,
            isFurnished: v.isFurnished,
            view: v.view !== 'Unknown' ? v.view : undefined,
            unitNumber: v.unitNumber || undefined,
            buildingNumber: v.buildingNumber || undefined,
            deliveryDate: v.deliveryDate || undefined,
            finishingType: v.finishingType || undefined,
            hasBalcony: v.hasBalcony,
            hasParking: v.hasParking,
            floorPlanUrl: v.floorPlanUrl || undefined,
            images: v.images ? v.images.split('\n').map((s: string) => s.trim()).filter(Boolean) : undefined,
            availabilityStatus: v.availabilityStatus || undefined,
            sortOrder: v.sortOrder ? Number(v.sortOrder) : undefined,
            isActive: v.isActive,
            isFeatured: v.isFeatured ?? undefined,
            isRecommended: v.isRecommended ?? undefined,
            deliveryText: v.deliveryText || undefined,
            deliveryTextAr: v.deliveryTextAr || undefined,
          })) : undefined,
          slug: unitSlug,
          seoTitle: unitForm.seoTitle || null, seoDescription: unitForm.seoDescription || null, seoKeywords: unitForm.seoKeywords || null,
          seoTitleAr: unitForm.seoTitleAr || null, seoDescriptionAr: unitForm.seoDescriptionAr || null, seoKeywordsAr: unitForm.seoKeywordsAr || null,
        });
        const newId = created?.id ?? created?.Id;
        if (newId && unitPendingFiles.length > 0) {
          await adminApi.uploadUnitImages(newId, unitPendingFiles);
        }
        loadUnits();
        clearUnitPending();
        toast.success("Unit added");
        setIsAddUnitOpen(false);
        setUnitForm(defaultUnitForm);
      } catch (e) { toast.error(e instanceof Error ? e.message : "Failed to create unit"); }
      finally { setIsUnitSubmitting(false); }
    } else {
      setIsUnitSubmitting(false);
      toast.error("Cannot create unit: project not found on server. Try reloading.");
    }
  };

  const openEditUnit = async (u: Property) => {
    clearUnitPending();
    setUnitRemovedImageIds([]);
    setUnitExistingVideos([]);
    const apiId = parseInt(u.id.replace('u-', ''), 10);
    const urlToId: Record<string, number> = {};
    let fetched: any = null;
    let fetchedInstallments: any[] = [];
    let fetchedContactId = '';
    let adminImageUrls: string[] = [];
    if (!isNaN(apiId)) {
      try {
        fetched = await adminApi.getUnit(apiId);
        if (fetched) {
          const adminImages = fetched.adminImages ?? fetched.AdminImages;
          if (Array.isArray(adminImages)) {
            adminImages.forEach((img: any) => {
              const key = img.Url ?? img.url;
              const val = img.Id ?? img.id;
              if (key && val) urlToId[key] = val;
            });
            adminImageUrls = adminImages.map((img: any) => img.Url ?? img.url);
          }
          if (Array.isArray(fetched.videos)) {
            setUnitExistingVideos(fetched.videos.map((v: any) => ({ id: v.id, url: v.url, publicId: v.publicId ?? '' })));
          }
          fetchedInstallments = fetched.installments ?? fetched.Installments ?? [];
          const contactInfo = fetched.contactInfo ?? fetched.ContactInfo;
          if (contactInfo) {
            const ciName = contactInfo.name ?? contactInfo.Name;
            const ciPhone = contactInfo.phone ?? contactInfo.Phone;
            const matched = contacts.find(c => c.name === ciName && c.phone === ciPhone);
            if (matched) fetchedContactId = matched.id;
          }
        }
      } catch { toast.error("Failed to load unit"); /* use store data as fallback */ }
    }
    setUnitImageIdByUrl(urlToId);
    setUnitForm({
      titleEn: u.titleEn, titleAr: u.titleAr || '',
      descriptionEn: u.descriptionEn || '', descriptionAr: u.descriptionAr || '',
      rentPerMonth: String(u.rentPerMonth || ''),
      currency: u.currency, location: u.location,
      governorate: (u.location || '').split(/[،,]\s*/)[0]?.trim() || '',
      city: (u.location || '').split(/[،,]\s*/)[1]?.trim() || '',
      area: (u.location || '').split(/[،,]\s*/)[2]?.trim() || '',
      governorateAr: ((u as any).locationAr || '').split(/[،,]\s*/)[0]?.trim() || '',
      cityAr: ((u as any).locationAr || '').split(/[،,]\s*/)[1]?.trim() || '',
      areaAr: ((u as any).locationAr || '').split(/[،,]\s*/)[2]?.trim() || '',
      bedrooms: u.bedrooms != null && u.bedrooms > 0 ? String(u.bedrooms) : '',
      bathrooms: u.bathrooms != null && u.bathrooms > 0 ? String(u.bathrooms) : '',
      floor: u.floor != null ? String(u.floor) : '',
      isFurnished: !!u.isFurnished, view: u.view || 'Unknown',
      unitNumber: u.unitNumber || '', buildingNumber: u.buildingNumber || '',
      deliveryDate: u.deliveryDate || '', finishingType: u.finishingType || '',
      hasBalcony: !!u.hasBalcony, hasParking: !!u.hasParking,
      propertyType: u.propertyType, listingType: u.listingType,
      contactId: fetchedContactId || u.contactId, images: adminImageUrls.length > 0 ? adminImageUrls : u.images,
      features: (u as any).features || [], featuresAr: (u as any).featuresAr || [], featuresInput: '', featuresArInput: '',
      locationAr: (u as any).locationAr || '',
      isRecommended: (u as any).isRecommended ?? false,
      deliveryText: (u as any).deliveryText || '',
      constructionStatus: (u as any).constructionStatus || '',
      availabilityStatus: (u as any).availabilityStatus || 'Available',
      ownershipType: (u as any).ownershipType || '',
      virtualTourUrl: (u as any).virtualTourUrl || '',
      highlightsAr: (u as any).highlightsAr || [],
      nearbyPlaces: (u as any).nearbyPlaces || [],
      nearbyPlacesAr: (u as any).nearbyPlacesAr || [],
      installments: fetchedInstallments.length > 0 ? fetchedInstallments.map((i: any) => ({
        paymentType: (i.paymentType ?? i.PaymentType) === 'Cash' ? 'Cash' as const : 'Installment' as const,
        downPaymentPercent: String(i.downPaymentPercent ?? i.DownPaymentPercent ?? ''),
        discountPercent: String(i.discountPercent ?? i.DiscountPercent ?? ''),
        years: String(i.years ?? i.Years ?? ''),
        monthlyAmount: String(i.monthlyAmount ?? i.MonthlyAmount ?? ''),
        isEnabled: i.isEnabled ?? i.IsEnabled ?? true,
      })) : (u.installments?.length ? u.installments.map(i => ({
        paymentType: (i.paymentType ?? i.PaymentType) === 'Cash' ? 'Cash' as const : 'Installment' as const,
        downPaymentPercent: String(i.downPaymentPercent),
        discountPercent: String(i.discountPercent ?? ''),
        years: String(i.years),
        monthlyAmount: String(i.monthlyAmount || ''),
        isEnabled: i.isEnabled,
      })) : []),
      slug: u.slug, slugIsAuto: u.slugIsAuto,
      seoTitle: u.seoTitle || '', seoDescription: u.seoDescription || '', seoKeywords: u.seoKeywords || '',
      seoTitleAr: u.seoTitleAr || '', seoDescriptionAr: u.seoDescriptionAr || '', seoKeywordsAr: u.seoKeywordsAr || '',
      variants: [],
    });
    const fetchedVariants = (fetched as any)?.variants;
    if (Array.isArray(fetchedVariants) && fetchedVariants.length > 0) {
      setUnitForm(prev => ({
        ...prev,
        variants: fetchedVariants.map((v: any) => ({
          name: v.name || '',
          nameAr: v.nameAr || '',
          size: v.size != null ? String(v.size) : '',
          price: v.price != null ? String(v.price) : '',
          currency: v.currency || 'EGP',
          rentPerMonth: v.rentPerMonth != null ? String(v.rentPerMonth) : '',
          bedrooms: v.bedrooms != null ? String(v.bedrooms) : '0',
          bathrooms: v.bathrooms != null ? String(v.bathrooms) : '0',
          floor: v.floor != null ? String(v.floor) : '',
          isFurnished: !!v.isFurnished,
          view: v.view || 'Unknown',
          unitNumber: v.unitNumber || '',
          buildingNumber: v.buildingNumber || '',
          deliveryDate: v.deliveryDate || '',
          finishingType: v.finishingType || '',
          hasBalcony: !!v.hasBalcony,
          hasParking: !!v.hasParking,
          floorPlanUrl: v.floorPlanUrl || '',
          images: Array.isArray(v.images) ? v.images.join('\n') : '',
          availabilityStatus: v.availabilityStatus || 'Available',
          sortOrder: v.sortOrder != null ? String(v.sortOrder) : '0',
          isActive: v.isActive !== false,
          isFeatured: v.isFeatured ?? false,
          isRecommended: v.isRecommended ?? false,
          deliveryText: v.deliveryText || '',
        })),
      }));
    }
    setEditingUnitId(u.id);
  };

  const handleUpdateUnit = async () => {
    if (!unitForm.titleEn || !unitForm.titleAr || !unitForm.descriptionEn || !unitForm.location || !editingUnitId) {
      toast.error("Please fill in required fields (Title EN/AR, Description EN, Location)");
      return;
    }
    const variantPrices = (unitForm as any).variants?.length ? (unitForm as any).variants.map((v: any) => Number(v.price)).filter((p: number) => p > 0) : [];
    const minPrice = variantPrices.length > 0 ? Math.min(...variantPrices) : 0;
    const maxPrice = variantPrices.length > 0 ? Math.max(...variantPrices) : minPrice;
    const variantSizes = (unitForm as any).variants?.length ? (unitForm as any).variants.map((v: any) => Number(v.size)).filter((s: number) => s > 0) : [];
    const computedMinArea = variantSizes.length > 0 ? Math.min(...variantSizes) : 0;
    const computedMaxArea = variantSizes.length > 0 ? Math.max(...variantSizes) : computedMinArea;
    const unitInstallments = unitForm.installments
      .filter(i => i.paymentType === 'Cash' || (i.downPaymentPercent && i.years))
      .map(i => i.paymentType === 'Cash' ? ({
        paymentType: 'Cash' as const,
        downPaymentPercent: 100,
        discountPercent: Number(i.discountPercent) || 0,
        years: 0,
        isEnabled: i.isEnabled,
      }) : ({
        paymentType: 'Installment' as const,
        downPaymentPercent: Number(i.downPaymentPercent),
        years: Number(i.years),
        isEnabled: i.isEnabled,
      }));
    const unitSlug = unitForm.slugIsAuto ? autoSlug(unitForm.titleEn) : unitForm.slug;
    const selectedContact = contacts.find(c => c.id === unitForm.contactId);
    const unitContactPayload = unitNewContact
      ? { name: unitNewContactName.trim(), phone: unitNewContactPhone.trim(), type: CONTACT_TYPE_MAP[unitNewContactType] ?? 0 }
      : selectedContact
        ? { name: selectedContact.name, phone: selectedContact.phone, type: CONTACT_TYPE_MAP[selectedContact.type] ?? 0 }
        : undefined;
    const data = {
      titleEn: unitForm.titleEn,
      titleAr: unitForm.titleAr || unitForm.titleEn,
      descriptionEn: unitForm.descriptionEn,
      descriptionAr: unitForm.descriptionAr || unitForm.descriptionEn,
      minPrice, maxPrice,
      rentPerMonth: unitForm.listingType === 'Rental' ? Number(unitForm.rentPerMonth) : null,
      location: unitForm.location, locationAr: [unitForm.governorateAr, unitForm.cityAr, unitForm.areaAr].filter(Boolean).join(', ') || undefined,
      minArea: computedMinArea, maxArea: computedMaxArea,
      bedrooms: unitForm.bedrooms ? Number(unitForm.bedrooms) : null,
      bathrooms: unitForm.bathrooms ? Number(unitForm.bathrooms) : null,
      floor: unitForm.floor ? Number(unitForm.floor) : null,
      isFurnished: unitForm.isFurnished,
      isRecommended: unitForm.isRecommended ?? false,
      view: unitForm.view !== 'Unknown' ? unitForm.view : null,
      unitNumber: unitForm.unitNumber || null,
      buildingNumber: unitForm.buildingNumber || null,
      deliveryDate: unitForm.deliveryDate || null,
      deliveryText: unitForm.deliveryText || null,
      deliveryTextAr: unitForm.deliveryTextAr || null,
      finishingType: unitForm.finishingType || null,
      hasBalcony: unitForm.hasBalcony,
      hasParking: unitForm.hasParking,
      constructionStatus: unitForm.constructionStatus || null,
      availabilityStatus: unitForm.availabilityStatus || null,
      ownershipType: unitForm.ownershipType || null,
      virtualTourUrl: unitForm.virtualTourUrl || null,
      highlightsAr: unitForm.highlightsAr?.length ? unitForm.highlightsAr : undefined,
      nearbyPlaces: unitForm.nearbyPlaces?.length ? unitForm.nearbyPlaces : undefined,
      nearbyPlacesAr: unitForm.nearbyPlacesAr?.length ? unitForm.nearbyPlacesAr : undefined,
      currency: unitForm.currency,
      contactId: unitNewContact ? '' : unitForm.contactId,
      contactName: unitNewContact ? unitNewContactName.trim() : selectedContact?.name || '',
      contactPhone: unitNewContact ? unitNewContactPhone.trim() : selectedContact?.phone || '',
      propertyType: unitForm.propertyType, listingType: unitForm.listingType,
      features: unitForm.features,
      featuresAr: unitForm.featuresAr.length ? unitForm.featuresAr : undefined,
      installments: unitInstallments,
      variants: (unitForm as any).variants?.length ? (unitForm as any).variants.map((v: any) => ({
        name: v.name || undefined,
        nameAr: v.nameAr || undefined,
        size: v.size ? Number(v.size) : undefined,
        price: v.price ? Number(v.price.replace(/,/g, '')) : undefined,
        currency: v.currency || undefined,
        rentPerMonth: v.rentPerMonth ? Number(v.rentPerMonth.replace(/,/g, '')) : undefined,
        bedrooms: v.bedrooms ? Number(v.bedrooms) : undefined,
        bathrooms: v.bathrooms ? Number(v.bathrooms) : undefined,
        floor: v.floor ? Number(v.floor) : undefined,
        isFurnished: v.isFurnished,
        view: v.view !== 'Unknown' ? v.view : undefined,
        unitNumber: v.unitNumber || undefined,
        buildingNumber: v.buildingNumber || undefined,
        deliveryDate: v.deliveryDate || undefined,
        finishingType: v.finishingType || undefined,
        hasBalcony: v.hasBalcony,
        hasParking: v.hasParking,
        floorPlanUrl: v.floorPlanUrl || undefined,
        images: v.images ? v.images.split('\n').map((s: string) => s.trim()).filter(Boolean) : undefined,
        availabilityStatus: v.availabilityStatus || undefined,
        sortOrder: v.sortOrder ? Number(v.sortOrder) : undefined,
        isActive: v.isActive,
        isFeatured: v.isFeatured ?? undefined,
        isRecommended: v.isRecommended ?? undefined,
        deliveryText: v.deliveryText || undefined,
        deliveryTextAr: v.deliveryTextAr || undefined,
      })) : undefined,
      slug: unitSlug,
      seoTitle: unitForm.seoTitle || null, seoDescription: unitForm.seoDescription || null, seoKeywords: unitForm.seoKeywords || null,
      seoTitleAr: unitForm.seoTitleAr || null, seoDescriptionAr: unitForm.seoDescriptionAr || null, seoKeywordsAr: unitForm.seoKeywordsAr || null,
    };
    setIsUnitSubmitting(true);
    updateProperty(editingUnitId, data);
    const apiId = parseInt(editingUnitId.replace('u-', ''), 10);
    if (!isNaN(apiId)) {
      for (const imgId of unitRemovedImageIds) {
        try { await adminApi.deleteUnitImage(apiId, imgId); }
        catch { /* skip - image might already be gone */ }
      }
      try {
        const projectApiId = parseInt(selectedProject.id, 10);
        await adminApi.updateUnit(apiId, {
          ...data,
          projectId: isNaN(projectApiId) ? undefined : projectApiId,
          contact: unitContactPayload,
        });
        if (unitPendingFiles.length > 0) {
          await adminApi.uploadUnitImages(apiId, unitPendingFiles);
        }
        loadUnits();
        clearUnitPending();
        toast.success("Unit updated");
        setEditingUnitId(null);
        setUnitForm(defaultUnitForm);
      } catch (e: any) {
        toast.error(e?.message || "Failed to update unit");
      }
      finally { setIsUnitSubmitting(false); }
    } else {
      setIsUnitSubmitting(false);
    }
  };

  const handleViewUnit = async (u: Property) => {
    const apiId = parseInt(u.id.replace('u-', ''), 10);
    if (!isNaN(apiId)) {
      try {
        const fetched: any = await adminApi.getUnit(apiId);
        if (fetched) {
          const storeFields = apiPropertyToStore(fetched);
          const contactInfo = fetched.contactInfo ?? fetched.ContactInfo;
          const contactName = contactInfo?.name ?? contactInfo?.Name ?? storeFields.contactName ?? '';
          const contactPhone = contactInfo?.phone ?? contactInfo?.Phone ?? storeFields.contactPhone ?? '';
          const found = contacts.find(c => c.name === contactName && c.phone === contactPhone);
          setDetailUnit({
            ...u,
            ...storeFields,
            id: u.id,
            code: storeFields.code || u.code,
            projectName: storeFields.projectName || u.projectName,
            contactId: found?.id || storeFields.contactId || u.contactId,
            contactName: contactName || undefined,
            contactPhone: contactPhone || undefined,
          });
          return;
        }
      } catch { toast.error("Failed to load unit details"); /* fallback to store */ }
    }
    setDetailUnit(u);
  };

  const handleDeleteUnit = async () => {
    if (deleteUnitId) {
      const apiId = parseInt(deleteUnitId.replace('u-', ''), 10);
      if (!isNaN(apiId)) {
        try {
          const detail: any = await adminApi.getUnit(apiId);
          if (detail?.videos) {
            for (const v of detail.videos) {
              try { await adminApi.deleteUnitVideo(apiId, v.id); } catch { /* skip individual failures */ }
            }
          }
        } catch { /* fall through to delete entity */ }
        try { await adminApi.deleteUnit(apiId); } catch { toast.error("Failed to sync unit delete"); }
      }
      deleteUnit(deleteUnitId);
      toast.success("Unit deleted");
      setDeleteUnitId(null);
    }
  };

  const clearUnitPending = () => {
    unitPendingPreviews.forEach(u => URL.revokeObjectURL(u));
    setUnitPendingFiles([]);
    setUnitPendingPreviews([]);
    setUnitRemovedImageIds([]);
    setUnitImageIdByUrl({});
    setUnitNewContact(false);
    setUnitNewContactName('');
    setUnitNewContactPhone('');
    setUnitNewContactType('Owner');
  };

  const removeUnitPending = (idx: number) => {
    URL.revokeObjectURL(unitPendingPreviews[idx]);
    setUnitPendingFiles(prev => prev.filter((_, i) => i !== idx));
    setUnitPendingPreviews(prev => prev.filter((_, i) => i !== idx));
  };

  const handleImageUpload = async (files: FileList | null, target: 'unit' | 'project') => {
    if (!files) return;
    if (target === 'unit') {
      const newFiles = Array.from(files);
      setUnitPendingFiles(prev => [...prev, ...newFiles]);
      const urls = newFiles.map(f => URL.createObjectURL(f));
      setUnitPendingPreviews(prev => [...prev, ...urls]);
    } else {
      for (const file of Array.from(files)) {
        try {
          const result = await adminApi.uploadImage(file, 'projects');
          setProjectForm((prev) => ({ ...prev, image: result.url }));
        } catch {
          toast.error("Failed to upload image. Please check file size/type and try again.");
        }
      }
    }
  };

  const handleProjectDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      reorderProjects(active.id as string, over.id as string);
      const latest = [...useStore.getState().projects]
        .sort((a, b) => a.order - b.order)
        .map((p, i) => ({ ...p, order: i }));
      for (const p of latest) {
        const apiId = parseInt(p.id, 10);
        if (!isNaN(apiId)) {
          try {
            await adminApi.updateProject(apiId, { sortOrder: p.order });
          } catch { /* skip individual failures */ }
        }
      }
      toast.success("Project order updated");
    }
  };

  const handleUnitDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      reorderUnits(active.id as string, over.id as string);
      toast.success("Unit order updated");
    }
  };

  if (initialLoading) {
    return <div className="flex items-center justify-center py-20"><Loader2 className="w-8 h-8 animate-spin text-primary" /></div>;
  }

  // Preview: project detail view (user POV matching ProjectDetailsPage)
  if (previewMode && selectedProject) {
    const fp = fullProject || selectedProject;
    const name = previewProjectLang === 'ar' ? (fp.nameAr || selectedProject.nameAr || fp.name) : (fp.nameEn || selectedProject.nameEn || fp.name);
    const description = previewProjectLang === 'ar' ? (fp.descriptionAr || selectedProject.descriptionAr) : (fp.descriptionEn || selectedProject.descriptionEn || fp.description);
    const previewUnitCount = selectedProject.unitCount;
    return (
      <div className="min-h-screen bg-background animate-slide-in">
        {/* Hero */}
        <div className="relative h-[65vh] min-h-[420px]">
          <img src={fp.image || ''} alt={name} loading="lazy" width={1920} height={1080} className="absolute inset-0 w-full h-full object-cover" />
          <div className="absolute inset-0 bg-gradient-to-t from-background via-background/50 to-navy/20" />
          <div className="absolute inset-0 flex flex-col justify-end pb-16">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full">
              <div className="max-w-3xl">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2 text-gold font-medium mb-3">
                    <MapPin className="w-5 h-5" />
                    <span className="text-base">{previewProjectLang === 'ar' && fp.locationAr ? fp.locationAr : fp.location}</span>
                  </div>
                  {(fp.nameAr || selectedProject?.nameAr) && (
                    <Button variant="ghost" size="sm" onClick={() => setPreviewProjectLang(lang => lang === 'en' ? 'ar' : 'en')} className="gap-1 text-xs text-white/80 hover:text-white">
                      <Languages className="w-3 h-3" /> {previewProjectLang === 'en' ? "AR" : "EN"}
                    </Button>
                  )}
                </div>
                <h1 className="font-display text-4xl sm:text-5xl md:text-6xl font-bold text-foreground mb-4 leading-tight">{name}</h1>
                <div className="flex items-center gap-3">
                  <span className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-emerald-500/15 text-emerald-600 text-sm font-semibold border border-emerald-500/30">
                    <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                    {"Now Selling"}
                  </span>
                  {fp.developer && (
                    <span className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-navy/60 text-foreground text-sm font-semibold border border-border/50">
                      <Building className="w-4 h-4" />
                      {fp.developer}
                    </span>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-8">
          <Button variant="outline" size="sm" onClick={() => setSelectedProjectId(null)} className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground mb-6 font-medium text-sm transition-colors group">
            <ArrowLeft className="w-4 h-4 group-hover:-translate-x-1 transition-transform" />
            {"Back"}
          </Button>
        </div>

        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
            <div className="lg:col-span-2 space-y-12">
              <section>
                <h2 className="font-display text-3xl font-bold mb-2">{"About"}</h2>
                <div className="w-12 h-1 bg-gold rounded-full mb-6" />
                <p className="text-muted-foreground text-lg leading-relaxed whitespace-pre-wrap break-words">{description}</p>
              </section>
              {(previewProjectLang === 'ar' && fp.highlightsAr?.length ? fp.highlightsAr : fp.highlights).length > 0 && (
                <section>
                  <h2 className="font-display text-3xl font-bold mb-2">{previewProjectLang === 'ar' ? 'المميزات' : 'Highlights'}</h2>
                  <div className="w-12 h-1 bg-gold rounded-full mb-6" />
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    {(previewProjectLang === 'ar' && fp.highlightsAr?.length ? fp.highlightsAr : fp.highlights).map((h: string, _i: number) => (
                      <div key={h} className="flex items-center gap-3 bg-muted/40 p-4 rounded-xl border border-border hover:border-gold/30 transition-colors">
                        <div className="w-8 h-8 rounded-full bg-gold/10 flex items-center justify-center text-gold shrink-0">
                          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" /></svg>
                        </div>
                        <span className="font-medium">{h}</span>
                      </div>
                    ))}
                  </div>
                </section>
              )}
              {(() => {
                const nearby = previewProjectLang === 'ar' ? (fp.nearbyPlacesAr?.length ? fp.nearbyPlacesAr : fp.nearbyPlaces) : fp.nearbyPlaces;
                if (!nearby?.length) return null;
                return (
                  <section>
                    <h2 className="font-display text-2xl font-bold mb-2">{previewProjectLang === 'ar' ? 'الأماكن القريبة' : 'Nearby Places'}</h2>
                    <div className="w-12 h-1 bg-gold rounded-full mb-6" />
                    <div className="flex flex-wrap gap-2">
                      {nearby.map((p: string, i: number) => (
                        <span key={i} className="px-4 py-2 bg-muted/40 rounded-full border border-border text-sm font-medium">{p}</span>
                      ))}
                    </div>
                  </section>
                );
              })()}
            </div>

            {/* Overview Sidebar */}
            <div>
              <div className="bg-card border border-border shadow-xl rounded-2xl overflow-hidden sticky top-32">
                <div className="bg-navy px-7 py-5">
                  <h3 className="font-display text-lg font-bold text-white">{"Overview"}</h3>
                </div>
                <div className="px-7 py-6">
                  <dl className="space-y-5">
                    <div className="flex justify-between items-center pb-5 border-b border-border">
                      <dt className="text-muted-foreground text-sm">{"Location"}</dt>
                      <dd className="font-semibold text-sm">{previewProjectLang === 'ar' && fp.locationAr ? fp.locationAr : fp.location}</dd>
                    </div>
                    <div className="flex justify-between items-center pb-5 border-b border-border">
                      <dt className="text-muted-foreground text-sm">{"Status"}</dt>
                      <dd className="font-semibold text-sm text-emerald-600">{"Now Selling"}</dd>
                    </div>
                    <div className="flex justify-between items-center">
                      <dt className="text-muted-foreground text-sm">{"Available Units"}</dt>
                      <dd className="font-semibold text-sm">{previewUnitCount}</dd>
                    </div>
                    {fp.ownershipType && (
                      <div className="flex justify-between items-center pt-5 border-t border-border">
                        <dt className="text-muted-foreground text-sm">{"Ownership Type"}</dt>
                        <dd className="font-semibold text-sm">{fp.ownershipType === 'GreenContract' ? 'Green Contract' : fp.ownershipType === 'Freehold' ? 'Freehold' : fp.ownershipType === 'Leasehold' ? 'Leasehold' : fp.ownershipType}</dd>
                      </div>
                    )}
                    {fp.startingPrice != null && (
                      <div className="flex justify-between items-center pt-5 border-t border-border">
                        <dt className="text-muted-foreground text-sm">{"Starting Price"}</dt>
                        <dd className="font-semibold text-sm text-emerald-600">{Number(fp.startingPrice).toLocaleString()} EGP</dd>
                      </div>
                    )}
                  </dl>
                  <button onClick={() => document.getElementById('preview-units')?.scrollIntoView({ behavior: 'smooth' })}
                    className="w-full mt-7 py-4 bg-navy text-white rounded-xl font-semibold hover:bg-navy-light transition-colors">
                    {"View Units"}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Units Section */}
        <section id="preview-units" className="bg-muted/30 py-24">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <h2 className="font-display text-3xl font-bold mb-2">{"Available Units"}</h2>
            <div className="w-16 h-1.5 bg-gold rounded-full mb-10" />
            {projectUnits.length === 0 ? (
              <div className="text-center py-20 border-2 border-dashed border-border rounded-2xl">
                <Building className="mx-auto h-12 w-12 text-muted-foreground/30 mb-4" />
                <h3 className="text-lg font-medium">{"No units available"}</h3>
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-8">
                {projectUnits.map((u) => <UserPropertyCard key={u.id} property={u} onClick={() => handleViewUnit(u)} />)}
              </div>
            )}
          </div>
        </section>

        <PropertyDetailDialog key={detailUnit?.id} property={detailUnit} open={!!detailUnit} onOpenChange={(o) => !o && setDetailUnit(null)} />
      </div>
    );
  }

  // Preview: projects list
  if (previewMode) {
    return (
      <div className="space-y-6 animate-slide-in">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">{"Projects"}</h2>
          <p className="text-muted-foreground mt-1">{"Manage developments and their units."}</p>
        </div>
        {sortedProjects.length === 0 ? (
          <div className="text-center py-20 border-2 border-dashed border-border rounded-2xl">
            <Building className="mx-auto h-12 w-12 text-muted-foreground/30 mb-4" />
            <h3 className="text-lg font-medium">{"No projects yet"}</h3>
          </div>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {sortedProjects.map((project) => (
              <UserProjectCard key={project.id} project={project} onClick={() => setSelectedProjectId(project.id)} lang={previewProjectLang} />
            ))}
          </div>
        )}
        <ProjectDetailDialog key={detailProject?.id} project={detailProject} open={!!detailProject} onOpenChange={(o) => !o && setDetailProject(null)} onUnitClick={(u) => { setDetailProject(null); handleViewUnit(u); }} />
        <PropertyDetailDialog key={detailUnit?.id} property={detailUnit} open={!!detailUnit} onOpenChange={(o) => !o && setDetailUnit(null)} />
      </div>
    );
  }

  // Admin: project detail (units)
  if (selectedProject) {
    return (
      <div className="space-y-6 animate-slide-in w-full max-w-full">
        <div className="flex items-center gap-4 flex-wrap">
          <Button variant="outline" size="sm" onClick={() => { setSelectedProjectId(null); setUnitSearch(''); }}>
            <ArrowLeft className="mr-2 h-4 w-4" /> {"Back"}
          </Button>
          <div className="flex-1 min-w-0">
            <h2 className="text-2xl font-bold truncate">{selectedProject.name}</h2>
            <p className="text-sm text-muted-foreground flex flex-wrap items-center gap-3">
              <span className="flex items-center gap-1"><MapPin className="w-3 h-3" /> {selectedProject.location}</span>
              {selectedProject.developer && <span className="flex items-center gap-1"><Building className="w-3 h-3" /> {selectedProject.developer}</span>}
            </p>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <Input id="unit-search" autoComplete="off" placeholder={"Search by code, title..."} value={unitSearch} onChange={(e) => setUnitSearch(e.target.value)} className="w-40 sm:w-52" />
            <Button onClick={() => { setUnitForm({ ...defaultUnitForm, location: selectedProject.location }); setIsAddUnitOpen(true); }} className="bg-primary hover:bg-primary/90 shrink-0">
              <Plus className="mr-2 h-4 w-4" /> {"Add Unit"}
            </Button>
          </div>
        </div>

        {projectUnits.length === 0 ? (
          <div className="text-center py-20 border-2 border-dashed border-border rounded-2xl">
            <Building className="mx-auto h-12 w-12 text-muted-foreground/30 mb-4" />
            <h3 className="text-lg font-medium">{"No units available"}</h3>
          </div>
        ) : (
          <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleUnitDragEnd}>
            <SortableContext items={projectUnits.map((u) => u.id)} strategy={rectSortingStrategy}>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 min-w-0">
                  {projectUnits.map((u) => {
                  const contact = contacts.find((c) => c.id === u.contactId);
                  return <SortableUnitCard key={u.id} unit={u} contact={contact} onEdit={() => openEditUnit(u)} onView={() => handleViewUnit(u)} onDelete={() => setDeleteUnitId(u.id)} />;
                })}
              </div>
            </SortableContext>
          </DndContext>
        )}

        <UnitDialog
          isAddOpen={isAddUnitOpen}
          editingUnitId={editingUnitId}
          unitForm={unitForm}
          setUnitForm={setUnitForm}
          unitPendingPreviews={unitPendingPreviews}
          unitNewContact={unitNewContact}
          unitNewContactName={unitNewContactName}
          unitNewContactPhone={unitNewContactPhone}
          unitNewContactType={unitNewContactType}
          unitImageIdByUrl={unitImageIdByUrl}
          contacts={contacts}
          projectName={selectedProject.name}
          fileRef={fileRef}
          onAdd={handleAddUnit}
          onUpdate={handleUpdateUnit}
          onClose={() => { setIsAddUnitOpen(false); setEditingUnitId(null); clearUnitPending(); setUnitForm(defaultUnitForm); setUnitExistingVideos([]); }}
          setUnitNewContact={setUnitNewContact}
          setUnitNewContactName={setUnitNewContactName}
          setUnitNewContactPhone={setUnitNewContactPhone}
          setUnitNewContactType={setUnitNewContactType}
          onRemoveExistingImage={(img) => {
            const id = unitImageIdByUrl[img];
            if (id) setUnitRemovedImageIds(prev => [...prev, id]);
            setUnitForm({ ...unitForm, images: unitForm.images.filter((u) => u !== img) });
          }}
          onRemovePendingImage={removeUnitPending}
          onUploadClick={() => fileRef.current?.click()}
          onFileChange={(e) => handleImageUpload(e.target.files, 'unit')}
          addInstallment={addUnitInstallment}
          addCashInstallment={addUnitCashInstallment}
          updateInstallment={updateUnitInstallment}
          removeInstallment={removeUnitInstallment}
          isSubmitting={isUnitSubmitting}
          videoUploadZone={editingUnitId ? (
            <VideoUploadZone
              key={editingUnitId}
              entityType="units"
              entityId={parseInt(editingUnitId.replace('u-', ''), 10)}
              existingVideos={unitExistingVideos}
              onVideoAdded={(v) => setUnitExistingVideos(prev => [...prev, v])}
              onVideoRemoved={(videoId) => setUnitExistingVideos(prev => prev.filter(x => x.id !== videoId))}
              projectId={selectedProjectId ? (parseInt(selectedProjectId, 10) || undefined) : undefined}
            />
          ) : undefined}
        />

        <AdminPropertyDetailDialog key={detailUnit?.id} property={detailUnit} open={!!detailUnit} onOpenChange={(o) => !o && setDetailUnit(null)} />

        <AlertDialog open={!!deleteUnitId} onOpenChange={(o) => !o && setDeleteUnitId(null)}>
          <AlertDialogContent className="bg-card">
            <AlertDialogHeader>
              <AlertDialogTitle>{"Delete Unit?"}</AlertDialogTitle>
              <AlertDialogDescription>{"This action cannot be undone."}</AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>{"Cancel"}</AlertDialogCancel>
              <AlertDialogAction onClick={handleDeleteUnit} className="bg-destructive hover:bg-destructive/90">{"Delete"}</AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    );
  }

  // Admin: projects list
  return (
    <div className="space-y-6 animate-slide-in">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">{"Projects"}</h2>
          <p className="text-muted-foreground mt-1">{"Manage developments and their units."}</p>
        </div>
        <Button onClick={() => { resetProjectForm(); setIsAddProjectOpen(true); }} className="bg-primary hover:bg-primary/90">
          <Plus className="mr-2 h-4 w-4" /> {"Add Project"}
        </Button>
      </div>

      {sortedProjects.length === 0 ? (
        <div className="text-center py-20 border-2 border-dashed border-border rounded-2xl">
          <Building className="mx-auto h-12 w-12 text-muted-foreground/30 mb-4" />
            <h3 className="text-lg font-medium">{"No projects yet"}</h3>
        </div>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleProjectDragEnd}>
          <SortableContext items={sortedProjects.map((p) => p.id)} strategy={rectSortingStrategy}>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {sortedProjects.map((project) => (
                <SortableProjectCard
                  key={project.id} project={project}
                  onClick={() => setSelectedProjectId(project.id)}
                  onEdit={() => openEditProject(project)}
                  onDelete={() => setDeleteProjectId(project.id)}
                />
              ))}
            </div>
          </SortableContext>
        </DndContext>
      )}

      <AddProjectDialog
        isOpen={isAddProjectOpen}
        onOpenChange={setIsAddProjectOpen}
        form={projectForm}
        setForm={setProjectForm}
        onSave={handleAddProject}
        title={"Add Project"}
        saveLabel={"Create Project"}
      />

      <AddProjectDialog
        isOpen={isEditProjectOpen}
        onOpenChange={(o) => { if (!o) { setIsEditProjectOpen(false); setEditingProject(null); setProjectExistingVideos([]); } }}
        form={projectForm}
        setForm={setProjectForm}
        onSave={handleUpdateProject}
        title={"Edit Project"}
        saveLabel={"Update Project"}
        videoUploadZone={editingProject && isEditProjectOpen ? (
          <VideoUploadZone
            key={editingProject.id}
            entityType="projects"
            entityId={parseInt(editingProject.id, 10)}
            existingVideos={projectExistingVideos}
            onVideoAdded={(v) => setProjectExistingVideos(prev => [...prev, v])}
            onVideoRemoved={(videoId) => setProjectExistingVideos(prev => prev.filter(x => x.id !== videoId))}
          />
        ) : undefined}
      />

      <AlertDialog open={!!deleteProjectId} onOpenChange={(o) => !o && setDeleteProjectId(null)}>
        <AlertDialogContent className="bg-card">
          <AlertDialogHeader>
            <AlertDialogTitle>{"Delete Project?"}</AlertDialogTitle>
            <AlertDialogDescription>{"This will delete the project permanently. Associated units will remain but their project reference will be removed."}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{"Cancel"}</AlertDialogCancel>
            <AlertDialogAction onClick={handleDeleteProject} className="bg-destructive hover:bg-destructive/90">{"Delete"}</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
