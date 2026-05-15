import { type Dispatch, type SetStateAction, type RefObject, type ReactNode } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { ImagePlus, Plus, X, Trash2 } from "lucide-react";
import { AdminLocationPicker } from "@/components/AdminLocationPicker";
import { type Contact, type PropertyType, type ListingType, PROPERTY_TYPES, LISTING_TYPES, PROPERTY_VIEWS, FINISHING_TYPES } from "@/store";
import type { DefaultUnitForm, VariantFormItem } from "@/lib/constants";
import { defaultVariantFormItem } from "@/lib/constants";

interface UnitDialogProps {
  isAddOpen: boolean;
  editingUnitId: string | null;
  unitForm: DefaultUnitForm;
  setUnitForm: Dispatch<SetStateAction<DefaultUnitForm>>;
  unitPendingPreviews: string[];
  unitNewContact: boolean;
  unitNewContactName: string;
  unitNewContactPhone: string;
  unitNewContactType: "Owner" | "Broker";
  unitImageIdByUrl: Record<string, number>;
  contacts: Contact[];
  projectName: string;
  fileRef: RefObject<HTMLInputElement>;
  onAdd: () => void;
  onUpdate: () => void;
  onClose: () => void;
  setUnitNewContact: Dispatch<SetStateAction<boolean>>;
  setUnitNewContactName: Dispatch<SetStateAction<string>>;
  setUnitNewContactPhone: Dispatch<SetStateAction<string>>;
  setUnitNewContactType: Dispatch<SetStateAction<"Owner" | "Broker">>;
  onRemoveExistingImage: (url: string) => void;
  onRemovePendingImage: (idx: number) => void;
  onUploadClick: () => void;
  onFileChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  addInstallment: () => void;
  addCashInstallment?: () => void;
  updateInstallment: (idx: number, patch: Partial<{ paymentType: 'Installment' | 'Cash'; downPaymentPercent: string; discountPercent: string; years: string; monthlyAmount: string; isEnabled: boolean }>) => void;
  removeInstallment: (idx: number) => void;
  isSubmitting?: boolean;
  videoUploadZone?: ReactNode;
}

function fmtNum(n: string): string {
  const raw = n.replace(/[^0-9]/g, '');
  return raw ? raw.replace(/\B(?=(\d{3})+(?!\d))/g, ',') : '';
}
function stripNum(s: string): string {
  return s.replace(/[^0-9]/g, '');
}

function autoSlug(s: string) {
  return s.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
}

export function UnitDialog({
  isAddOpen,
  editingUnitId,
  unitForm,
  setUnitForm,
  unitPendingPreviews,
  unitNewContact,
  unitNewContactName,
  unitNewContactPhone,
  unitNewContactType,
  contacts,
  projectName,
  fileRef,
  onAdd,
  onUpdate,
  onClose,
  setUnitNewContact,
  setUnitNewContactName,
  setUnitNewContactPhone,
  setUnitNewContactType,
  onRemoveExistingImage,
  onRemovePendingImage,
  onUploadClick,
  onFileChange,
  addInstallment,
  addCashInstallment,
  updateInstallment,
  removeInstallment,
  isSubmitting,
  videoUploadZone,
}: UnitDialogProps) {
  const isOpen = isAddOpen || !!editingUnitId;

  const addFeature = () => {
    if (unitForm.featuresInput.trim()) {
      setUnitForm(prev => ({ ...prev, features: [...prev.features, prev.featuresInput.trim()], featuresInput: '' }));
    }
  };
  const removeFeature = (idx: number) => setUnitForm(prev => ({ ...prev, features: prev.features.filter((_, i) => i !== idx) }));
  const addFeatureAr = () => {
    if (unitForm.featuresArInput.trim()) {
      setUnitForm(prev => ({ ...prev, featuresAr: [...prev.featuresAr, prev.featuresArInput.trim()], featuresArInput: '' }));
    }
  };
  const removeFeatureAr = (idx: number) => setUnitForm(prev => ({ ...prev, featuresAr: prev.featuresAr.filter((_, i) => i !== idx) }));

  const addVariant = () => setUnitForm(prev => ({ ...prev, variants: [...prev.variants, { ...defaultVariantFormItem }] }));
  const updateVariant = (idx: number, patch: Partial<VariantFormItem>) => setUnitForm(prev => ({
    ...prev, variants: prev.variants.map((v, i) => i === idx ? { ...v, ...patch } : v),
  }));
  const removeVariant = (idx: number) => setUnitForm(prev => ({ ...prev, variants: prev.variants.filter((_, i) => i !== idx) }));

  return (
    <Dialog open={isOpen} onOpenChange={(o) => { if (!o) onClose(); }}>
      <DialogContent className="sm:max-w-[1000px] lg:max-w-[1100px] max-h-[90vh] overflow-y-auto bg-card">
        <DialogHeader><DialogTitle>{editingUnitId ? "Update Unit" : `Add Unit to ${projectName}`}</DialogTitle></DialogHeader>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="unit-title-en">{"Title (EN)"} *</Label><Input id="unit-title-en" autoComplete="off" value={unitForm.titleEn} onChange={(e) => setUnitForm({ ...unitForm, titleEn: e.target.value })} /></div>
              <div className="space-y-2"><Label htmlFor="unit-title-ar">{"Title (AR)"}</Label><Input id="unit-title-ar" autoComplete="off" dir="rtl" value={unitForm.titleAr} onChange={(e) => setUnitForm({ ...unitForm, titleAr: e.target.value })} /></div>
              <div className="space-y-2"><Label htmlFor="unit-desc-en">{"Description (EN)"}</Label><Textarea id="unit-desc-en" value={unitForm.descriptionEn} onChange={(e) => setUnitForm({ ...unitForm, descriptionEn: e.target.value })} rows={5} className="min-h-[120px]" /></div>
              <div className="space-y-2"><Label htmlFor="unit-desc-ar">{"Description (AR)"}</Label><Textarea id="unit-desc-ar" dir="rtl" value={unitForm.descriptionAr} onChange={(e) => setUnitForm({ ...unitForm, descriptionAr: e.target.value })} rows={5} className="min-h-[120px]" /></div>
            </div>
            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="unit-type">{"Property Type"}</Label>
                <Select value={unitForm.propertyType} onValueChange={(v) => setUnitForm({ ...unitForm, propertyType: v as PropertyType })}>
                  <SelectTrigger id="unit-type"><SelectValue /></SelectTrigger>
                  <SelectContent>{PROPERTY_TYPES.map(t => <SelectItem key={t.value} value={t.value}>{t.label}</SelectItem>)}</SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="unit-listing-type">{"Listing Type"}</Label>
                <Select value={unitForm.listingType} onValueChange={(v) => setUnitForm({ ...unitForm, listingType: v as ListingType })}>
                  <SelectTrigger id="unit-listing-type"><SelectValue /></SelectTrigger>
                  <SelectContent>{LISTING_TYPES.map(t => <SelectItem key={t.value} value={t.value}>{t.label}</SelectItem>)}</SelectContent>
                </Select>
              </div>
              <div className="space-y-2"><Label htmlFor="unit-currency">{"Currency"}</Label><Input id="unit-currency" autoComplete="off" value={unitForm.currency} onChange={(e) => setUnitForm({ ...unitForm, currency: e.target.value })} /></div>
              {unitForm.listingType === 'Rental' && (
                <div className="space-y-2"><Label htmlFor="unit-rent">{"Rent / month"} *</Label><Input id="unit-rent" autoComplete="off" type="text" inputMode="numeric" value={fmtNum(unitForm.rentPerMonth)} onChange={(e) => setUnitForm({ ...unitForm, rentPerMonth: stripNum(e.target.value) })} /></div>
              )}
              <div className="col-span-3 space-y-2">
                <Label htmlFor="alp-governorate">{"Location"} *</Label>
                <AdminLocationPicker
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  governorate={(unitForm as any).governorate ?? ''}
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  city={(unitForm as any).city ?? ''}
                  // eslint-disable-next-line @typescript-eslint/no-explicit-any
                  area={(unitForm as any).area ?? ''}
                  onChange={(gov, cty, are, combined) => setUnitForm({ ...unitForm, governorate: gov, city: cty, area: are, location: combined })}
                />
              </div>
              <div className="col-span-3 space-y-2">
                <Label htmlFor="unit-add-governorate-ar">{"الموقع (Arabic)"}</Label>
                <div className="grid grid-cols-3 gap-2" dir="rtl">
                  <Input id="unit-add-governorate-ar" value={unitForm.governorateAr} onChange={e => setUnitForm({...unitForm, governorateAr: e.target.value})} placeholder="المحافظة" />
                  <Input id="unit-add-city-ar" value={unitForm.cityAr} onChange={e => setUnitForm({...unitForm, cityAr: e.target.value})} placeholder="المدينة" />
                  <Input id="unit-add-area-ar" value={unitForm.areaAr} onChange={e => setUnitForm({...unitForm, areaAr: e.target.value})} placeholder="المنطقة" />
                </div>
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="unit-add-features-input">{"Features"}</Label>
              <div className="flex gap-2">
                <Input id="unit-add-features-input" autoComplete="off" value={unitForm.featuresInput} onChange={(e) => setUnitForm({ ...unitForm, featuresInput: e.target.value })}
                  placeholder={"Type and press Enter"} onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addFeature())} />
                <Button type="button" variant="outline" size="sm" onClick={addFeature}>{"Add"}</Button>
              </div>
              <div className="flex flex-wrap gap-1.5">
                {unitForm.features.map((f, i) => (
                  <Badge key={f + '-' + i} variant="secondary" className="gap-1">
                    {f}
                    <button onClick={() => removeFeature(i)} aria-label={"Delete"}><X className="w-3 h-3" /></button>
                  </Badge>
                ))}
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="unit-add-features-ar-input">{"المميزات (Arabic)"}</Label>
              <div className="flex gap-2" dir="rtl">
                <Input id="unit-add-features-ar-input" autoComplete="off" value={unitForm.featuresArInput} onChange={(e) => setUnitForm({ ...unitForm, featuresArInput: e.target.value })}
                  placeholder={"Type and press Enter"} onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addFeatureAr())} />
                <Button type="button" variant="outline" size="sm" onClick={addFeatureAr}>{"Add"}</Button>
              </div>
              <div className="flex flex-wrap gap-1.5">
                {unitForm.featuresAr.map((f, i) => (
                  <Badge key={f + '-' + i} variant="secondary" className="gap-1">
                    {f}
                    <button onClick={() => removeFeatureAr(i)} aria-label={"Delete"}><X className="w-3 h-3" /></button>
                  </Badge>
                ))}
              </div>
            </div>
            <div className="border border-border rounded-xl p-4 space-y-3">
              <p className="text-base font-semibold">{"Unit Details"}</p>
              <div className="grid grid-cols-3 gap-4">
                <div className="space-y-2"><Label htmlFor="unit-add-bedrooms">{"Bedrooms"}</Label><Input id="unit-add-bedrooms" autoComplete="off" type="number" min={0} max={20} value={unitForm.bedrooms} onChange={(e) => setUnitForm({ ...unitForm, bedrooms: e.target.value })} /></div>
                <div className="space-y-2"><Label htmlFor="unit-add-bathrooms">{"Bathrooms"}</Label><Input id="unit-add-bathrooms" autoComplete="off" type="number" min={0} max={20} value={unitForm.bathrooms} onChange={(e) => setUnitForm({ ...unitForm, bathrooms: e.target.value })} /></div>
                <div className="space-y-2"><Label htmlFor="unit-add-floor">{"Floor"}</Label><Input id="unit-add-floor" autoComplete="off" type="number" min={0} value={unitForm.floor} onChange={(e) => setUnitForm({ ...unitForm, floor: e.target.value })} /></div>
                <div className="space-y-2"><Label htmlFor="unit-add-unit-number">{"Unit Number"}</Label><Input id="unit-add-unit-number" autoComplete="off" value={unitForm.unitNumber} onChange={(e) => setUnitForm({ ...unitForm, unitNumber: e.target.value })} /></div>
                <div className="space-y-2"><Label htmlFor="unit-add-building-number">{"Building Number"}</Label><Input id="unit-add-building-number" autoComplete="off" value={unitForm.buildingNumber} onChange={(e) => setUnitForm({ ...unitForm, buildingNumber: e.target.value })} /></div>
                <div className="space-y-2"><Label htmlFor="unit-add-delivery-date">{"Delivery Date"}</Label><Input id="unit-add-delivery-date" autoComplete="off" type="date" value={unitForm.deliveryDate} onChange={(e) => setUnitForm({ ...unitForm, deliveryDate: e.target.value })} /></div>
                <div className="space-y-2">
                  <Label htmlFor="unit-add-view">{"View"}</Label>
                  <Select value={unitForm.view} onValueChange={(v) => setUnitForm({ ...unitForm, view: v })}>
                    <SelectTrigger id="unit-add-view"><SelectValue /></SelectTrigger>
                    <SelectContent>{PROPERTY_VIEWS.map(v => <SelectItem key={v} value={v}>{v}</SelectItem>)}</SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="unit-add-finishing">{"Finishing"}</Label>
                  <Select value={unitForm.finishingType} onValueChange={(v) => setUnitForm({ ...unitForm, finishingType: v === 'none' ? '' : v })}>
                    <SelectTrigger id="unit-add-finishing"><SelectValue placeholder="Select" /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">{"None"}</SelectItem>
                      {FINISHING_TYPES.map(v => <SelectItem key={v} value={v}>{v}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
                <div className="col-span-3 flex items-end gap-4 pb-2">
                  <div className="flex items-center gap-2">
                    <Switch id="unit-add-furnished" checked={unitForm.isFurnished} onCheckedChange={(v) => setUnitForm({ ...unitForm, isFurnished: v })} />
                    <Label htmlFor="unit-add-furnished">{"Furnished"}</Label>
                  </div>
                  <div className="flex items-center gap-2">
                    <Switch id="unit-add-balcony" checked={unitForm.hasBalcony} onCheckedChange={(v) => setUnitForm({ ...unitForm, hasBalcony: v })} />
                    <Label htmlFor="unit-add-balcony">{"Balcony"}</Label>
                  </div>
                  <div className="flex items-center gap-2">
                    <Switch id="unit-add-parking" checked={unitForm.hasParking} onCheckedChange={(v) => setUnitForm({ ...unitForm, hasParking: v })} />
                    <Label htmlFor="unit-add-parking">{"Parking"}</Label>
                  </div>
                  <div className="flex items-center gap-2">
                    <Switch id="unit-add-recommended" checked={!!unitForm.isRecommended} onCheckedChange={(v) => setUnitForm({ ...unitForm, isRecommended: v })} />
                    <Label htmlFor="unit-add-recommended">{"Recommended"}</Label>
                  </div>
                </div>
                <div className="grid grid-cols-3 gap-4 pt-2 col-span-3">
                  <div className="space-y-2">
                    <Label htmlFor="unit-add-delivery-text">{"Delivery Text"}</Label>
                    <Input id="unit-add-delivery-text" autoComplete="off" value={unitForm.deliveryText ?? ''} onChange={(e) => setUnitForm({ ...unitForm, deliveryText: e.target.value })} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="unit-add-delivery-text-ar">{"Delivery Text (AR)"}</Label>
                    <Input id="unit-add-delivery-text-ar" autoComplete="off" value={unitForm.deliveryTextAr ?? ''} onChange={(e) => setUnitForm({ ...unitForm, deliveryTextAr: e.target.value })} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="unit-add-construction-status">{"Construction Status"}</Label>
                    <select id="unit-add-construction-status"
                      className="flex h-10 w-full rounded-xl border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      value={unitForm.constructionStatus ?? ''} onChange={(e) => setUnitForm({ ...unitForm, constructionStatus: e.target.value })}>
                      <option value="">None</option>
                      <option value="Planned">Planned</option>
                      <option value="UnderConstruction">Under Construction</option>
                      <option value="NearDelivery">Near Delivery</option>
                      <option value="Delivered">Delivered</option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="unit-add-availability-status">{"Availability"}</Label>
                    <Input id="unit-add-availability-status" autoComplete="off" value={unitForm.availabilityStatus ?? ''} onChange={(e) => setUnitForm({ ...unitForm, availabilityStatus: e.target.value })} placeholder="Available" />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="unit-add-ownership-type">{"Ownership Type"}</Label>
                    <select id="unit-add-ownership-type"
                      className="flex h-10 w-full rounded-xl border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      value={unitForm.ownershipType ?? ''} onChange={(e) => setUnitForm({ ...unitForm, ownershipType: e.target.value })}>
                      <option value="">None</option>
                      <option value="GreenContract">Green Contract</option>
                      <option value="Freehold">Freehold</option>
                      <option value="Leasehold">Leasehold</option>
                    </select>
                  </div>
                </div>
              </div>
            </div>
            {unitForm.listingType !== 'Rental' && (
              <div className="border border-border rounded-xl p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <p className="text-base font-semibold">{"Installment Plans"}</p>
                  <div className="flex gap-2">
                    <Button type="button" variant="outline" size="sm" onClick={addInstallment}>
                      <Plus className="w-3 h-3 mr-1" /> {"Installment"}
                    </Button>
                    {addCashInstallment && (
                      <Button type="button" variant="outline" size="sm" onClick={addCashInstallment}>
                        <Plus className="w-3 h-3 mr-1" /> {"Cash"}
                      </Button>
                    )}
                  </div>
                </div>
                {unitForm.installments.length === 0 ? (
                  <p className="text-sm text-muted-foreground">{"No installment plans. Add one to enable financing."}</p>
                ) : (
                  <div className="space-y-2">
                    {unitForm.installments.map((inst, i) => {
                      const basePrice = unitForm.variants.length > 0
                        ? Math.min(...unitForm.variants.map(v => Number(v.price || 0)))
                        : 0;
                      const cashPrice = inst.paymentType === 'Cash' && basePrice && inst.discountPercent
                        ? Math.round(basePrice * (1 - Number(inst.discountPercent) / 100))
                        : 0;
                      return (
                        <div key={`add-${i}`} className="border border-border rounded-lg p-3 space-y-2">
                          <div className="flex items-center justify-between">
                            <Badge variant={inst.paymentType === 'Cash' ? 'secondary' : 'default'}>
                              {inst.paymentType === 'Cash' ? 'Cash' : 'Installment'}
                            </Badge>
                            <div className="flex items-center gap-2">
                              <Switch id={"unit-add-inst-enabled-" + i} checked={inst.isEnabled} onCheckedChange={(v) => updateInstallment(i, { isEnabled: v })} />
                              <Button type="button" variant="ghost" size="icon" className="text-destructive h-8 w-8" onClick={() => removeInstallment(i)} aria-label="Delete">
                                <Trash2 className="w-4 h-4" />
                              </Button>
                            </div>
                          </div>
                          <div className="grid grid-cols-2 gap-2">
                            <div className="space-y-1">
                              <Label className="text-xs" htmlFor={`unit-add-inst-type-${i}`}>{"Type"}</Label>
                              <Select value={inst.paymentType} onValueChange={(v: 'Installment' | 'Cash') => updateInstallment(i, {
                                paymentType: v,
                                downPaymentPercent: v === 'Cash' ? '100' : '10',
                                years: v === 'Cash' ? '0' : '5',
                                discountPercent: v === 'Cash' ? '20' : '',
                              })}>
                                <SelectTrigger id={`unit-add-inst-type-${i}`}><SelectValue placeholder="Select" /></SelectTrigger>
                                <SelectContent>
                                  <SelectItem value="Installment">Installment</SelectItem>
                                  <SelectItem value="Cash">Cash</SelectItem>
                                </SelectContent>
                              </Select>
                            </div>
                            {inst.paymentType === 'Cash' ? (
                              <div className="space-y-1">
                                <Label className="text-xs" htmlFor={`unit-add-inst-discount-${i}`}>{"Discount %"}</Label>
                                <Input id={`unit-add-inst-discount-${i}`} autoComplete="off" type="number" value={inst.discountPercent} onChange={(e) => updateInstallment(i, { discountPercent: e.target.value })} />
                              </div>
                            ) : (
                              <>
                                <div className="space-y-1">
                                  <Label className="text-xs" htmlFor={`unit-add-inst-down-${i}`}>{"Down %"}</Label>
                                  <Input id={`unit-add-inst-down-${i}`} autoComplete="off" type="number" value={inst.downPaymentPercent} onChange={(e) => updateInstallment(i, { downPaymentPercent: e.target.value })} />
                                </div>
                                <div className="space-y-1">
                                  <Label className="text-xs" htmlFor={`unit-add-inst-years-${i}`}>{"Years"}</Label>
                                  <Input id={`unit-add-inst-years-${i}`} autoComplete="off" type="number" value={inst.years} onChange={(e) => updateInstallment(i, { years: e.target.value })} />
                                </div>
                              </>
                            )}
                          </div>
                          {inst.paymentType === 'Cash' && cashPrice > 0 && (
                            <p className="text-sm text-muted-foreground">
                              Cash price: <strong>{cashPrice.toLocaleString()} {unitForm.currency}</strong> (after {inst.discountPercent}% discount)
                            </p>
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}
            </div>
            )}
            <div className="border border-border rounded-xl p-4 space-y-3">
              <div className="flex items-center justify-between">
                <p className="text-base font-semibold">{"Variants"}</p>
                <Button type="button" variant="outline" size="sm" onClick={addVariant}>
                  <Plus className="w-3 h-3 mr-1" /> {"Add Variant"}
                </Button>
              </div>
              {(unitForm.variants ?? []).length === 0 ? (
                <p className="text-sm text-muted-foreground">{"No variants. Add one to offer different options for this unit."}</p>
              ) : (
                <div className="space-y-2">
                  {unitForm.variants.map((v, i) => (
                    <div key={`edit-v-${i}`} className="border border-border rounded-lg p-3 space-y-2">
                      <div className="flex items-center justify-between">
                        <Badge variant="secondary">{v.name || `Variant ${i + 1}`}</Badge>
                        <Button type="button" variant="ghost" size="icon" className="text-destructive h-8 w-8" onClick={() => removeVariant(i)} aria-label="Delete">
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                      <div className="grid grid-cols-3 gap-2">
                        <div className="space-y-1"><Label htmlFor={`edit-v-name-${i}`} className="text-xs">{"Name"}</Label><Input id={`edit-v-name-${i}`} autoComplete="off" value={v.name} onChange={(e) => updateVariant(i, { name: e.target.value })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-name-ar-${i}`} className="text-xs">{"الاسم (عربي)"}</Label><Input id={`edit-v-name-ar-${i}`} dir="rtl" autoComplete="off" value={v.nameAr ?? ''} onChange={(e) => updateVariant(i, { nameAr: e.target.value })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-size-${i}`} className="text-xs">{"Size (m²)"}</Label><Input id={`edit-v-size-${i}`} autoComplete="off" type="number" value={v.size} onChange={(e) => updateVariant(i, { size: e.target.value })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-price-${i}`} className="text-xs">{"Price"}</Label><Input id={`edit-v-price-${i}`} autoComplete="off" type="text" inputMode="numeric" value={fmtNum(v.price)} onChange={(e) => updateVariant(i, { price: stripNum(e.target.value) })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-rent-${i}`} className="text-xs">{"Rent / Month"}</Label><Input id={`edit-v-rent-${i}`} autoComplete="off" type="text" inputMode="numeric" value={fmtNum(v.rentPerMonth)} onChange={(e) => updateVariant(i, { rentPerMonth: stripNum(e.target.value) })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-bedrooms-${i}`} className="text-xs">{"Bedrooms"}</Label><Input id={`edit-v-bedrooms-${i}`} autoComplete="off" type="number" value={v.bedrooms} onChange={(e) => updateVariant(i, { bedrooms: e.target.value })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-bathrooms-${i}`} className="text-xs">{"Bathrooms"}</Label><Input id={`edit-v-bathrooms-${i}`} autoComplete="off" type="number" value={v.bathrooms} onChange={(e) => updateVariant(i, { bathrooms: e.target.value })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-floor-${i}`} className="text-xs">{"Floor"}</Label><Input id={`edit-v-floor-${i}`} autoComplete="off" type="number" value={v.floor} onChange={(e) => updateVariant(i, { floor: e.target.value })} /></div>
                        <div className="space-y-1">
                          <Label htmlFor={`edit-v-view-${i}`} className="text-xs">{"View"}</Label>
                          <Select value={v.view} onValueChange={(val) => updateVariant(i, { view: val })}>
                            <SelectTrigger id={`edit-v-view-${i}`}><SelectValue /></SelectTrigger>
                            <SelectContent>{PROPERTY_VIEWS.map(pv => <SelectItem key={pv} value={pv}>{pv}</SelectItem>)}</SelectContent>
                          </Select>
                        </div>
                        <div className="space-y-1">
                          <Label htmlFor={`edit-v-finishing-${i}`} className="text-xs">{"Finishing"}</Label>
                          <Select value={v.finishingType} onValueChange={(val) => updateVariant(i, { finishingType: val === 'none' ? '' : val })}>
                            <SelectTrigger id={`edit-v-finishing-${i}`}><SelectValue placeholder="Select" /></SelectTrigger>
                            <SelectContent>
                              <SelectItem value="none">{"None"}</SelectItem>
                              {FINISHING_TYPES.map(ft => <SelectItem key={ft} value={ft}>{ft}</SelectItem>)}
                            </SelectContent>
                          </Select>
                        </div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-unit-${i}`} className="text-xs">{"Unit #"}</Label><Input id={`edit-v-unit-${i}`} autoComplete="off" value={v.unitNumber} onChange={(e) => updateVariant(i, { unitNumber: e.target.value })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-building-${i}`} className="text-xs">{"Building #"}</Label><Input id={`edit-v-building-${i}`} autoComplete="off" value={v.buildingNumber} onChange={(e) => updateVariant(i, { buildingNumber: e.target.value })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-delivery-${i}`} className="text-xs">{"Delivery Date"}</Label><Input id={`edit-v-delivery-${i}`} autoComplete="off" type="date" value={v.deliveryDate} onChange={(e) => updateVariant(i, { deliveryDate: e.target.value })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-currency-${i}`} className="text-xs">{"Currency"}</Label><Input id={`edit-v-currency-${i}`} autoComplete="off" value={v.currency} onChange={(e) => updateVariant(i, { currency: e.target.value })} /></div>
                        <div className="space-y-1"><Label htmlFor={`edit-v-sort-${i}`} className="text-xs">{"Sort Order"}</Label><Input id={`edit-v-sort-${i}`} autoComplete="off" type="number" value={v.sortOrder} onChange={(e) => updateVariant(i, { sortOrder: e.target.value })} /></div>
                        <div className="space-y-1">
                          <Label htmlFor={`edit-v-availability-${i}`} className="text-xs">{"Availability"}</Label>
                          <Select value={v.availabilityStatus} onValueChange={(val) => updateVariant(i, { availabilityStatus: val })}>
                            <SelectTrigger id={`edit-v-availability-${i}`}><SelectValue /></SelectTrigger>
                            <SelectContent>
                              <SelectItem value="Available">{"Available"}</SelectItem>
                              <SelectItem value="Sold">{"Sold"}</SelectItem>
                              <SelectItem value="Reserved">{"Reserved"}</SelectItem>
                              <SelectItem value="UnderOffer">{"Under Offer"}</SelectItem>
                            </SelectContent>
                          </Select>
                        </div>
                      </div>
                      <div className="flex items-center gap-4 pt-1">
                        <div className="flex items-center gap-2">
                          <Switch id={`edit-v-furnished-${i}`} checked={v.isFurnished} onCheckedChange={(val) => updateVariant(i, { isFurnished: val })} />
                          <Label htmlFor={`edit-v-furnished-${i}`} className="text-xs">{"Furnished"}</Label>
                        </div>
                        <div className="flex items-center gap-2">
                          <Switch id={`edit-v-balcony-${i}`} checked={v.hasBalcony} onCheckedChange={(val) => updateVariant(i, { hasBalcony: val })} />
                          <Label htmlFor={`edit-v-balcony-${i}`} className="text-xs">{"Balcony"}</Label>
                        </div>
                        <div className="flex items-center gap-2">
                          <Switch id={`edit-v-parking-${i}`} checked={v.hasParking} onCheckedChange={(val) => updateVariant(i, { hasParking: val })} />
                          <Label htmlFor={`edit-v-parking-${i}`} className="text-xs">{"Parking"}</Label>
                        </div>
                        <div className="flex items-center gap-2">
                          <Switch id={`edit-v-active-${i}`} checked={v.isActive} onCheckedChange={(val) => updateVariant(i, { isActive: val })} />
                          <Label htmlFor={`edit-v-active-${i}`} className="text-xs">{"Active"}</Label>
                        </div>
                        <div className="flex items-center gap-2">
                          <Switch id={`edit-v-featured-${i}`} checked={!!v.isFeatured} onCheckedChange={(val) => updateVariant(i, { isFeatured: val })} />
                          <Label htmlFor={`edit-v-featured-${i}`} className="text-xs">{"Featured"}</Label>
                        </div>
                        <div className="flex items-center gap-2">
                          <Switch id={`edit-v-recommended-${i}`} checked={!!v.isRecommended} onCheckedChange={(val) => updateVariant(i, { isRecommended: val })} />
                          <Label htmlFor={`edit-v-recommended-${i}`} className="text-xs">{"Recommended"}</Label>
                        </div>
                      </div>
                      <div className="grid grid-cols-2 gap-2 pt-1">
                        <div className="space-y-1">
                          <Label htmlFor={`edit-v-delivery-text-${i}`} className="text-xs">{"Delivery Text"}</Label>
                          <Input id={`edit-v-delivery-text-${i}`} value={v.deliveryText ?? ''} onChange={(e) => updateVariant(i, { deliveryText: e.target.value })} />
                        </div>
                        <div className="space-y-1">
                          <Label htmlFor={`edit-v-delivery-text-ar-${i}`} className="text-xs">{"Delivery Text (AR)"}</Label>
                          <Input id={`edit-v-delivery-text-ar-${i}`} value={v.deliveryTextAr ?? ''} onChange={(e) => updateVariant(i, { deliveryTextAr: e.target.value })} />
                        </div>
                        <div className="space-y-1">
                          <Label htmlFor={`edit-v-floorplan-${i}`} className="text-xs">{"Floor Plan URL"}</Label>
                          <Input id={`edit-v-floorplan-${i}`} value={v.floorPlanUrl ?? ''} onChange={(e) => updateVariant(i, { floorPlanUrl: e.target.value })} placeholder="https://..." />
                        </div>
                      </div>
                      <div className="space-y-1 pt-1">
                        <Label htmlFor={`edit-v-images-${i}`} className="text-xs">{"Image URLs (one per line)"}</Label>
                        <textarea
                          id={`edit-v-images-${i}`}
                          value={v.images ?? ''}
                          onChange={(e) => updateVariant(i, { images: e.target.value })}
                          placeholder="https://..."
                          rows={2}
                          className="w-full rounded-lg border border-border bg-background px-3 py-2 text-xs outline-none focus:border-primary transition-colors resize-none"
                        />
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
            <div className="border border-border rounded-xl p-4 space-y-3">
              <div className="flex items-center justify-between">
                <p className="text-base font-semibold">{"Contact"}</p>
                <Button type="button" variant="outline" size="sm" onClick={() => { setUnitNewContact(!unitNewContact); if (!unitNewContact) setUnitForm({ ...unitForm, contactId: '' }); }}>
                  {unitNewContact ? "Select existing" : "+ New contact"}
                </Button>
              </div>
              {unitNewContact ? (
                <div className="space-y-3">
                  <div><Label htmlFor="unit-edit-new-name">{"Name"}</Label><Input id="unit-edit-new-name" autoComplete="name" value={unitNewContactName} onChange={e => setUnitNewContactName(e.target.value)} placeholder="Contact name" /></div>
                  <div><Label htmlFor="unit-edit-new-phone">{"Phone"}</Label><Input id="unit-edit-new-phone" autoComplete="tel" value={unitNewContactPhone} onChange={e => setUnitNewContactPhone(e.target.value)} placeholder="Phone number" /></div>
                  <div><Label htmlFor="unit-edit-new-type">{"Type"}</Label>
                    <Select value={unitNewContactType} onValueChange={(v: 'Owner' | 'Broker') => setUnitNewContactType(v)}>
                      <SelectTrigger id="unit-edit-new-type"><SelectValue /></SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Owner">{"Owner"}</SelectItem>
                        <SelectItem value="Broker">{"Broker"}</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              ) : (
                <Select value={unitForm.contactId} onValueChange={(v) => setUnitForm({ ...unitForm, contactId: v })}>
                  <SelectTrigger id="unit-contact"><SelectValue placeholder={"Select contact"} /></SelectTrigger>
                  <SelectContent>
                    {contacts.map((c) => <SelectItem key={c.id} value={c.id}>{c.name} ({c.type})</SelectItem>)}
                  </SelectContent>
                </Select>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="unit-edit-imageUpload" className="text-sm font-medium">{"Images"}</Label>
              <div className="flex flex-wrap gap-2">
                {unitForm.images.map((img) => (
                  <div key={img} className="relative group w-20 h-20 rounded-xl overflow-hidden border border-border">
                    <img src={img} alt="" loading="lazy" width={80} height={80} className="w-full h-full object-cover" />
                    <button type="button" onClick={() => onRemoveExistingImage(img)}
                      className="absolute inset-0 bg-background/60 opacity-0 group-hover:opacity-100 flex items-center justify-center transition-opacity">
                      <X className="w-4 h-4 text-destructive" />
                    </button>
                  </div>
                ))}
                {unitPendingPreviews.map((url) => (
                  <div key={url} className="relative group w-20 h-20 rounded-xl overflow-hidden border border-dashed border-primary">
                    <img src={url} alt="" loading="lazy" width={80} height={80} className="w-full h-full object-cover" />
                    <button type="button" onClick={() => onRemovePendingImage(unitPendingPreviews.indexOf(url))}
                      className="absolute inset-0 bg-background/60 opacity-0 group-hover:opacity-100 flex items-center justify-center transition-opacity">
                      <X className="w-4 h-4 text-destructive" />
                    </button>
                    <span className="absolute bottom-1 left-1 text-[8px] bg-primary/80 text-primary-foreground px-1 rounded">new</span>
                  </div>
                ))}
                <button type="button" onClick={onUploadClick}
                  className="w-20 h-20 rounded-xl border-2 border-dashed border-border hover:border-primary flex flex-col items-center justify-center text-muted-foreground hover:text-primary transition-colors">
                  <ImagePlus className="w-5 h-5" />
                </button>
              </div>
              <input ref={fileRef} id="unit-edit-imageUpload" name="imageUpload" type="file" accept="image/*" multiple className="hidden" onChange={onFileChange} />
            </div>
            <Accordion type="single" collapsible>
              <AccordionItem value="seo">
                <AccordionTrigger className="text-sm">{"SEO & Slug"}</AccordionTrigger>
                <AccordionContent className="space-y-3 pt-2">
                  <div className="flex items-center justify-between">
                    <Label htmlFor="unit-edit-auto-slug" className="text-xs">{"Auto-generate slug from title"}</Label>
                    <Switch id="unit-edit-auto-slug" checked={unitForm.slugIsAuto} onCheckedChange={(v) => setUnitForm({ ...unitForm, slugIsAuto: v })} />
                  </div>
                  <div className="space-y-1">
                    <Label htmlFor="unit-edit-slug" className="text-xs">{"Slug"}</Label>
                    <Input id="unit-edit-slug" autoComplete="off"
                      value={unitForm.slugIsAuto ? autoSlug(unitForm.titleEn) : unitForm.slug}
                      disabled={unitForm.slugIsAuto}
                      onChange={(e) => setUnitForm({ ...unitForm, slug: e.target.value })}
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1"><Label htmlFor="unit-edit-seo-title-en" className="text-xs">{"SEO Title (EN)"}</Label><Input id="unit-edit-seo-title-en" autoComplete="off" value={unitForm.seoTitle} onChange={(e) => setUnitForm({ ...unitForm, seoTitle: e.target.value })} placeholder="Auto-generated if empty" /></div>
                    <div className="space-y-1"><Label htmlFor="unit-edit-seo-title-ar" className="text-xs">{"SEO Title (AR)"}</Label><Input id="unit-edit-seo-title-ar" autoComplete="off" dir="rtl" value={unitForm.seoTitleAr} onChange={(e) => setUnitForm({ ...unitForm, seoTitleAr: e.target.value })} /></div>
                    <div className="space-y-1"><Label htmlFor="unit-edit-seo-desc-en" className="text-xs">{"SEO Description (EN)"}</Label><Textarea id="unit-edit-seo-desc-en" value={unitForm.seoDescription} onChange={(e) => setUnitForm({ ...unitForm, seoDescription: e.target.value })} rows={2} /></div>
                    <div className="space-y-1"><Label htmlFor="unit-edit-seo-desc-ar" className="text-xs">{"SEO Description (AR)"}</Label><Textarea id="unit-edit-seo-desc-ar" dir="rtl" value={unitForm.seoDescriptionAr} onChange={(e) => setUnitForm({ ...unitForm, seoDescriptionAr: e.target.value })} rows={2} /></div>
                    <div className="space-y-1"><Label htmlFor="unit-edit-seo-keywords-en" className="text-xs">{"Keywords (EN)"}</Label><Input id="unit-edit-seo-keywords-en" autoComplete="off" value={unitForm.seoKeywords} onChange={(e) => setUnitForm({ ...unitForm, seoKeywords: e.target.value })} placeholder="comma, separated" /></div>
                    <div className="space-y-1"><Label htmlFor="unit-edit-seo-keywords-ar" className="text-xs">{"Keywords (AR)"}</Label><Input id="unit-edit-seo-keywords-ar" autoComplete="off" dir="rtl" value={unitForm.seoKeywordsAr} onChange={(e) => setUnitForm({ ...unitForm, seoKeywordsAr: e.target.value })} /></div>
                  </div>
                  <p className="text-[11px] text-muted-foreground">{"Note: backend auto-generates SEO when fields are empty."}</p>
                </AccordionContent>
              </AccordionItem>
            </Accordion>
          </div>
        {videoUploadZone && <div className="border-t border-border pt-4 px-1">{videoUploadZone}</div>}
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>{"Cancel"}</Button>
          <Button onClick={editingUnitId ? onUpdate : onAdd} disabled={isSubmitting} className="bg-primary hover:bg-primary/90">{editingUnitId ? "Update Unit" : "Add Unit"}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
