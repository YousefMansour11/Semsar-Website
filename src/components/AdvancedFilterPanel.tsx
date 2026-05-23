import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { SlidersHorizontal, X, Check, Ruler, Sofa, CreditCard } from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import { fetchFilterMetadata } from '../lib/api';
import { CascadingLocationPicker } from './CascadingLocationPicker';
import { useLocationTree } from '../hooks/use-locations';
import type { SearchFilters } from '../hooks/use-property-search';
import type { LocationTreeNode } from '../hooks/use-locations';

const EMPTY_FILTERS: SearchFilters = {};

function activeCount(f: SearchFilters) {
  let c = 0;
  if (f.locationIds?.length) c++;
  if (f.minPrice !== undefined || f.maxPrice !== undefined) c++;
  if (f.minSize !== undefined || f.maxSize !== undefined) c++;
  if (f.bedrooms !== undefined) c++;
  if (f.bathrooms !== undefined) c++;
  if (f.propertyType) c++;
  if (f.features?.length) c += f.features.length;
  if (f.isFurnished) c++;
  if (f.hasInstallment) c++;
  if (f.sortBy && f.sortBy !== 'newest') c++;
  return c;
}

function resolveAncestors(nodes: LocationTreeNode[], id: number): { governorateId?: number; cityId?: number; areaIds: number[] } {
  for (const n of nodes) {
    if (n.id === id) return { governorateId: n.level === 1 ? n.id : undefined, areaIds: [] };
    for (const c of n.children) {
      if (c.id === id) return { governorateId: n.id, cityId: c.level === 2 ? c.id : undefined, areaIds: [] };
      for (const a of c.children) {
        if (a.id === id) return { governorateId: n.id, cityId: c.id, areaIds: [a.id] };
      }
    }
  }
  return { areaIds: [] };
}

interface Props {
  filters: SearchFilters;
  onApply: (f: SearchFilters) => void;
  priceSuffix?: string;
}

export function AdvancedFilterPanel({ filters, onApply, priceSuffix = 'EGP' }: Props) {
  const { t, language } = useLanguage();
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState<SearchFilters>(filters);
  const active = activeCount(filters) > 0;

  const { data: metadata } = useQuery({
    queryKey: ['filter-metadata'],
    queryFn: fetchFilterMetadata,
    staleTime: 5 * 60 * 1000,
  });

  const { data: locationTree } = useLocationTree();

  const types = (metadata?.propertyTypes?.map(p => p.value) ?? []);

  useEffect(() => {
    if (!open) return;
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') setOpen(false); };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [open]);

  const [locationSelection, setLocationSelection] = useState<{ governorateId?: number; cityId?: number; areaIds: number[] }>({ areaIds: [] });

  const handleLocationIdsChange = (ids: number[]) => {
    setDraft(prev => ({ ...prev, locationIds: ids.length ? ids : undefined }));
  };

  const openPanel = () => {
    setDraft(filters);
    if (filters.locationIds?.length && locationTree) {
      setLocationSelection(resolveAncestors(locationTree, filters.locationIds[0]));
    } else {
      setLocationSelection({ areaIds: [] });
    }
    setOpen(true);
  };

  useEffect(() => {
    if (open) setDraft(filters);
  }, [filters, open]);

  const applyAndClose = () => {
    // Sync locationSelection.areaIds into draft.locationIds
    if (locationSelection.areaIds.length) {
      setDraft(prev => ({ ...prev, locationIds: locationSelection.areaIds }));
    } else if (locationSelection.cityId) {
      setDraft(prev => ({ ...prev, locationIds: [locationSelection.cityId!] }));
    } else if (locationSelection.governorateId) {
      setDraft(prev => ({ ...prev, locationIds: [locationSelection.governorateId!] }));
    }
    // Apply from current draft state
    onApply(draft);
    setOpen(false);
  };

  useEffect(() => {
    if (!open) return;
    if (locationSelection.areaIds.length) {
      setDraft(prev => ({ ...prev, locationIds: locationSelection.areaIds }));
    }
  }, [open, locationSelection.areaIds]);

  const resetAndClose = () => {
    setLocationSelection({ areaIds: [] });
    setDraft(EMPTY_FILTERS);
    onApply(EMPTY_FILTERS);
    setOpen(false);
  };

  const set = (key: keyof SearchFilters, value: SearchFilters[keyof SearchFilters]) => setDraft(prev => ({ ...prev, [key]: value }));
  return (
    <>
      <button
        onClick={openPanel}
        aria-label={t('filters.filter')}
        className={`min-h-[44px] flex items-center gap-2 px-5 py-3 rounded-xl font-semibold text-sm border transition-all duration-200 shadow-sm ${
          active
            ? 'bg-secondary text-white border-secondary shadow-secondary/30 shadow-md'
            : 'bg-card text-foreground border-border hover:border-secondary/50 hover:shadow-md'
        }`}
      >
        <SlidersHorizontal className="w-4 h-4" />
        <span>{t('filters.filter')}</span>
        {active && (
          <span className="flex items-center justify-center w-5 h-5 rounded-full bg-white/25 text-xs font-bold">
            {activeCount(filters)}
          </span>
        )}
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-40 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200" onClick={() => setOpen(false)} aria-hidden="true" />
          <div className={`fixed inset-y-0 ${language === 'ar' ? 'left-0 animate-in slide-in-from-left' : 'right-0 animate-in slide-in-from-right'} duration-300 z-50 w-full max-w-sm bg-card shadow-2xl flex flex-col`} role="dialog" aria-modal="true" aria-label={t('filters.title')}>
            <div className="flex items-center justify-between p-6 border-b border-border">
              <div className="flex items-center gap-2">
                <SlidersHorizontal className="w-5 h-5 text-secondary" />
                <h2 className="text-lg font-bold">{t('filters.title')}</h2>
              </div>
              <button onClick={() => setOpen(false)} aria-label={t('filters.close')} className="p-2 rounded-lg hover:bg-muted transition-colors" autoFocus>
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="flex-1 overflow-y-auto p-6 space-y-8">
              {/* Sort */}
              <div>
                <h3 className="text-sm font-semibold uppercase tracking-wider mb-4">{t('filters.sortBy')}</h3>
                <select id="afp-sortBy" name="sortBy"
                  value={draft.sortBy || 'newest'}
                  onChange={e => set('sortBy', e.target.value)}
                  className="w-full bg-muted/50 border border-border rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-secondary/30 focus:border-secondary transition-all"
                >
                  <option value="newest">{t('filters.sortNewest')}</option>
                  <option value="price_asc">{t('filters.sortPriceAsc')}</option>
                  <option value="price_desc">{t('filters.sortPriceDesc')}</option>
                  <option value="size_asc">{t('filters.sortSizeAsc')}</option>
                  <option value="size_desc">{t('filters.sortSizeDesc')}</option>
                </select>
              </div>

              {/* Price Range */}
              <div>
                <h3 className="text-sm font-semibold uppercase tracking-wider mb-4">{t('filters.priceRange')}</h3>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label htmlFor="afp-minPrice" className="text-xs text-muted-foreground mb-1 block">{t('filters.minPrice')}</label>
                    <input id="afp-minPrice" type="text" inputMode="numeric" placeholder={t('filters.placeholder.min')} value={draft.minPrice !== undefined ? draft.minPrice.toLocaleString('en-US') : ''}
                      onChange={e => { const raw = e.target.value.replace(/,/g, ''); set('minPrice', raw === '' ? undefined : Number(raw)); }}
                      className="w-full bg-muted/50 border border-border rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-secondary/30 focus:border-secondary transition-all"
                    />
                  </div>
                  <div>
                    <label htmlFor="afp-maxPrice" className="text-xs text-muted-foreground mb-1 block">{t('filters.maxPrice')}</label>
                    <input id="afp-maxPrice" type="text" inputMode="numeric" placeholder={t('filters.placeholder.max')} value={draft.maxPrice !== undefined ? draft.maxPrice.toLocaleString('en-US') : ''}
                      onChange={e => { const raw = e.target.value.replace(/,/g, ''); set('maxPrice', raw === '' ? undefined : Number(raw)); }}
                      className="w-full bg-muted/50 border border-border rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-secondary/30 focus:border-secondary transition-all"
                    />
                  </div>
                </div>
                {priceSuffix && <p className="text-xs text-muted-foreground mt-2 font-mono">{priceSuffix}</p>}
              </div>

              {/* Size Range */}
              <div>
                <h3 className="text-sm font-semibold uppercase tracking-wider mb-4 flex items-center gap-2">
                  <Ruler className="w-4 h-4" />
                  {t('filters.size')}
                </h3>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label htmlFor="afp-minSize" className="text-xs text-muted-foreground mb-1 block">{t('filters.minSize')}</label>
                    <input id="afp-minSize" type="text" inputMode="numeric" placeholder={t('filters.placeholder.min')} value={draft.minSize !== undefined ? draft.minSize.toLocaleString('en-US') : ''}
                      onChange={e => { const raw = e.target.value.replace(/,/g, ''); set('minSize', raw === '' ? undefined : Number(raw)); }}
                      className="w-full bg-muted/50 border border-border rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-secondary/30 focus:border-secondary transition-all"
                    />
                  </div>
                  <div>
                    <label htmlFor="afp-maxSize" className="text-xs text-muted-foreground mb-1 block">{t('filters.maxSize')}</label>
                    <input id="afp-maxSize" type="text" inputMode="numeric" placeholder={t('filters.placeholder.max')} value={draft.maxSize !== undefined ? draft.maxSize.toLocaleString('en-US') : ''}
                      onChange={e => { const raw = e.target.value.replace(/,/g, ''); set('maxSize', raw === '' ? undefined : Number(raw)); }}
                      className="w-full bg-muted/50 border border-border rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-secondary/30 focus:border-secondary transition-all"
                    />
                  </div>
                </div>
                <p className="text-xs text-muted-foreground mt-2 font-mono">m²</p>
              </div>

              {/* Bedrooms / Bathrooms */}
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <h3 className="text-sm font-semibold uppercase tracking-wider mb-4">{t('filters.bedrooms')}</h3>
                  <select id="afp-bedrooms" name="bedrooms"
                    value={draft.bedrooms ?? ''}
                    onChange={e => set('bedrooms', e.target.value === '' ? undefined : Number(e.target.value))}
                    className="w-full bg-muted/50 border border-border rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-secondary/30 focus:border-secondary transition-all"
                  >
                    <option value="">{t('filters.any')}</option>
                    {[1, 2, 3, 4, 5].map(n => (
                      <option key={n} value={n}>{n}+</option>
                    ))}
                  </select>
                </div>
                <div>
                  <h3 className="text-sm font-semibold uppercase tracking-wider mb-4">{t('filters.bathrooms')}</h3>
                  <select id="afp-bathrooms" name="bathrooms"
                    value={draft.bathrooms ?? ''}
                    onChange={e => set('bathrooms', e.target.value === '' ? undefined : Number(e.target.value))}
                    className="w-full bg-muted/50 border border-border rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-secondary/30 focus:border-secondary transition-all"
                  >
                    <option value="">{t('filters.any')}</option>
                    {[1, 2, 3, 4, 5].map(n => (
                      <option key={n} value={n}>{n}+</option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Location — cascading picker */}
              <div>
                <h3 className="text-sm font-semibold uppercase tracking-wider mb-4">{t('filters.location')}</h3>
                <CascadingLocationPicker
                  value={locationSelection}
                  onChange={setLocationSelection}
                  onLocationIdsChange={handleLocationIdsChange}
                />
              </div>

              {/* Toggle filters */}
              <div className="space-y-3">
                <h3 className="text-sm font-semibold uppercase tracking-wider mb-4">{t('filters.more')}</h3>
                <label className="flex items-center gap-3 cursor-pointer">
                  <input id="afp-furnished" name="isFurnished"
                    type="checkbox"
                    checked={!!draft.isFurnished}
                    onChange={e => set('isFurnished', e.target.checked || undefined)}
                    className="w-4 h-4 rounded border-border text-secondary focus:ring-secondary/30"
                  />
                  <Sofa className="w-4 h-4 text-muted-foreground" />
                  <span className="text-sm">{t('filters.furnished')}</span>
                </label>
                <label className="flex items-center gap-3 cursor-pointer">
                  <input id="afp-installments" name="hasInstallment"
                    type="checkbox"
                    checked={!!draft.hasInstallment}
                    onChange={e => set('hasInstallment', e.target.checked || undefined)}
                    className="w-4 h-4 rounded border-border text-secondary focus:ring-secondary/30"
                  />
                  <CreditCard className="w-4 h-4 text-muted-foreground" />
                  <span className="text-sm">{t('filters.installments')}</span>
                </label>
              </div>

              {/* Property Type */}
              <div>
                <h3 className="text-sm font-semibold uppercase tracking-wider mb-4">{t('filters.type')}</h3>
                <div className="grid grid-cols-2 gap-2">
                  {types.map(type => {
                    const selected = draft.propertyType === type;
                    return (
                      <button key={type} onClick={() => set('propertyType', selected ? undefined : type)}
                        aria-pressed={selected}
                        className={`flex items-center justify-center gap-2 px-3 py-3 rounded-xl border text-sm font-semibold transition-all ${
                          selected ? 'bg-secondary text-white border-secondary shadow-md' : 'bg-muted/30 border-border hover:border-secondary/40'
                        }`}>
                        {selected && <Check className="w-3.5 h-3.5" />}
                        {t(`prop_type.${type}`, type)}
                      </button>
                    );
                  })}
                  {types.length === 0 && (
                    <div className="col-span-2 text-xs text-muted-foreground text-center py-4">{t('filters.noTypes')}</div>
                  )}
                </div>
              </div>

            </div>

            <div className="p-4 sm:p-6 border-t border-border flex gap-3">
              <button onClick={resetAndClose} className="flex-1 min-h-[44px] py-3 rounded-xl border border-border font-semibold text-sm hover:bg-muted/60 transition-colors active:scale-[0.98]">
                {t('filters.reset')}
              </button>
              <button onClick={applyAndClose} className="flex-1 min-h-[44px] py-3 rounded-xl bg-secondary text-white font-semibold text-sm hover:bg-secondary/90 shadow-md transition-all active:scale-[0.98]">
                {t('filters.apply')}
              </button>
            </div>
          </div>
        </>
      )}
    </>
  );
}
