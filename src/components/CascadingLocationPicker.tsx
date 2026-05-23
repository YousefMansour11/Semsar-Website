import { useState, useMemo, useRef, useEffect } from 'react';
import { useLanguage } from '../i18n/LanguageContext';
import { useLocationTree } from '../hooks/use-locations';
import { Search, X, ChevronRight, MapPin, Check } from 'lucide-react';
import type { LocationTreeNode } from '../hooks/use-locations';

interface Selection {
  governorateId?: number;
  cityId?: number;
  areaIds: number[];
}

interface Props {
  value: Selection;
  onChange: (val: Selection) => void;
  onLocationIdsChange: (ids: number[]) => void;
}

function findNode(nodes: LocationTreeNode[], id: number): LocationTreeNode | undefined {
  for (const n of nodes) {
    if (n.id === id) return n;
    if (n.children.length) {
      const found = findNode(n.children, id);
      if (found) return found;
    }
  }
  return undefined;
}

export function CascadingLocationPicker({ value, onChange, onLocationIdsChange }: Props) {
  const { t, language } = useLanguage();
  const { data: tree, isLoading: treeLoading } = useLocationTree();
  const [search, setSearch] = useState('');
  const [browsingLevel, setBrowsingLevel] = useState<'gov' | 'city' | 'area'>(
    value.areaIds.length ? 'area' : value.cityId || value.governorateId ? 'city' : 'gov'
  );
  const [isFocused, setIsFocused] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const blurTimeoutRef = useRef<ReturnType<typeof setTimeout>>();

  const govNode = useMemo(() => {
    if (!tree || !value.governorateId) return undefined;
    return tree.find(n => n.id === value.governorateId);
  }, [tree, value.governorateId]);

  const cityNode = useMemo(() => {
    if (!govNode || !value.cityId) return undefined;
    return findNode(govNode.children, value.cityId);
  }, [govNode, value.cityId]);

  const currentOptions = useMemo(() => {
    if (!tree) return [];
    if (browsingLevel === 'gov') return tree;
    if (browsingLevel === 'city') return govNode?.children ?? [];
    if (browsingLevel === 'area') return cityNode?.children ?? [];
    return [];
  }, [tree, browsingLevel, govNode, cityNode]);

  const filteredOptions = useMemo(() => {
    if (!search) return currentOptions;
    const q = search.toLowerCase();
    return currentOptions.filter(n => {
      const en = n.nameEn.toLowerCase();
      const ar = n.nameAr.toLowerCase();
      return en.startsWith(q) || ar.startsWith(q) || en.includes(` ${q}`) || ar.includes(` ${q}`);
    });
  }, [currentOptions, search]);

  const showDropdown = isFocused && currentOptions.length > 0;

  useEffect(() => {
    if (tree && tree.length === 1 && browsingLevel === 'gov' && !value.governorateId) {
      const onlyGov = tree[0];
      onChange({ governorateId: onlyGov.id, areaIds: [] });
      onLocationIdsChange([onlyGov.id]);
      setBrowsingLevel('city');
      setSearch('');
    }
  }, [tree, browsingLevel, value.governorateId, onChange, onLocationIdsChange]);

  useEffect(() => {
    if (isFocused) inputRef.current?.focus();
  }, [browsingLevel, isFocused]);

  const handleSelect = (node: LocationTreeNode) => {
    if (browsingLevel === 'gov') {
      onChange({ governorateId: node.id, areaIds: [] });
      onLocationIdsChange([node.id]);
      setBrowsingLevel('city');
      setSearch('');
      setIsFocused(true);
    } else if (browsingLevel === 'city') {
      onChange({ governorateId: value.governorateId, cityId: node.id, areaIds: [] });
      onLocationIdsChange([node.id]);
      setBrowsingLevel('area');
      setSearch('');
      setIsFocused(true);
    } else {
      const alreadyHas = value.areaIds.includes(node.id);
      const newAreaIds = alreadyHas
        ? value.areaIds.filter(id => id !== node.id)
        : [...value.areaIds, node.id];
      onChange({ ...value, areaIds: newAreaIds });
      onLocationIdsChange(newAreaIds.length ? newAreaIds : [value.cityId!]);
      setSearch('');
      inputRef.current?.blur();
      setIsFocused(false);
    }
  };

  const clearLevel = (level: 'gov' | 'city' | 'area') => {
    if (level === 'gov') {
      onChange({ areaIds: [] });
      onLocationIdsChange([]);
      setBrowsingLevel('gov');
    } else if (level === 'city') {
      onChange({ governorateId: value.governorateId, areaIds: [] });
      onLocationIdsChange([value.governorateId!]);
      setBrowsingLevel('city');
    } else {
      // Clear a single area chip
    }
    setSearch('');
    setIsFocused(true);
  };

  const removeArea = (areaId: number) => {
    const newAreaIds = value.areaIds.filter(id => id !== areaId);
    onChange({ ...value, areaIds: newAreaIds });
    onLocationIdsChange(newAreaIds.length ? newAreaIds : [value.cityId!]);
  };

  const selectedAreaNodes = useMemo(() => {
    if (!cityNode || !value.areaIds.length) return [];
    return value.areaIds
      .map(id => findNode(cityNode.children, id))
      .filter(Boolean) as LocationTreeNode[];
  }, [cityNode, value.areaIds]);

  return (
    <div>
      {/* Breadcrumb chips */}
      <div className="flex flex-wrap items-center gap-1.5 mb-3">
        {value.governorateId && govNode && (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-secondary/10 text-secondary text-xs font-semibold">
            <MapPin className="w-3 h-3" />
            {language === 'ar' ? (govNode.nameAr || govNode.nameEn) : govNode.nameEn}
            <button onClick={() => clearLevel('gov')} className="hover:bg-secondary/20 rounded p-0.5">
              <X className="w-3 h-3" />
            </button>
          </span>
        )}
        {value.cityId && cityNode && (
          <>
            <ChevronRight className={`w-3 h-3 text-muted-foreground ${language === 'ar' ? 'rotate-180' : ''}`} />
            <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-primary/10 text-primary text-xs font-semibold">
              <MapPin className="w-3 h-3" />
              {language === 'ar' ? (cityNode.nameAr || cityNode.nameEn) : cityNode.nameEn}
              <button onClick={() => clearLevel('city')} className="hover:bg-primary/20 rounded p-0.5">
                <X className="w-3 h-3" />
              </button>
            </span>
          </>
        )}
        {selectedAreaNodes.map(areaNode => (
          <span key={areaNode.id} className="flex items-center gap-0">
            <ChevronRight className={`w-3 h-3 text-muted-foreground ${language === 'ar' ? 'rotate-180' : ''}`} />
            <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400 text-xs font-semibold">
              <MapPin className="w-3 h-3" />
              {language === 'ar' ? (areaNode.nameAr || areaNode.nameEn) : areaNode.nameEn}
              <button onClick={() => removeArea(areaNode.id)} className="hover:bg-amber-200 dark:hover:bg-amber-800/30 rounded p-0.5">
                <X className="w-3 h-3" />
              </button>
            </span>
          </span>
        ))}
      </div>

      {/* Loading state */}
      {treeLoading && (
        <div className="flex items-center gap-2 px-3 py-2.5 mb-2 text-xs text-muted-foreground bg-muted/30 rounded-xl">
          <div className="w-3 h-3 rounded-full bg-muted-foreground/30 animate-pulse" />
          {t('general.loading')}
        </div>
      )}

      {/* Search */}
      {!treeLoading && (
        <>
      <div className="relative mb-2">
        <Search className={`absolute ${language === 'ar' ? 'right-3' : 'left-3'} top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground pointer-events-none`} />
        <input id="clp-search" name="locationSearch"
          ref={inputRef}
          type="text"
          placeholder={browsingLevel === 'gov' ? t('filters.searchGovernorate') : browsingLevel === 'city' ? t('filters.searchCity') : t('filters.searchArea')}
          value={search}
          onChange={e => setSearch(e.target.value)}
          onFocus={() => { clearTimeout(blurTimeoutRef.current); setIsFocused(true); }}
          onBlur={() => { blurTimeoutRef.current = setTimeout(() => setIsFocused(false), 200); }}
          className={`w-full bg-muted/50 border border-border rounded-xl ${language === 'ar' ? 'pr-9 pl-3' : 'pl-9 pr-3'} py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-secondary/30 focus:border-secondary transition-all`}
          autoComplete="off"
        />
      </div>

      {/* Suggestions dropdown */}
      {showDropdown && (
        <div className="max-h-48 overflow-y-auto space-y-1 rounded-xl border border-border bg-card">
          {filteredOptions.length === 0 && (
            <p className="px-3 py-6 text-xs text-muted-foreground text-center">{t('filters.noLocations')}</p>
          )}
          {filteredOptions.map(node => {
            const isGov = browsingLevel === 'gov';
            const isCity = browsingLevel === 'city';
            const selected = isGov ? node.id === value.governorateId
              : isCity ? node.id === value.cityId
              : value.areaIds.includes(node.id);
            return (
              <button
                key={node.id}
                onMouseDown={e => { e.preventDefault(); handleSelect(node); }}
                className={`w-full text-start px-3 py-2.5 text-sm flex items-center justify-between transition-colors ${
                  selected
                    ? 'bg-secondary/10 text-secondary font-semibold'
                    : 'hover:bg-muted/50'
                }`}
              >
                <span className="flex items-center gap-2">
                  {selected && <Check className="w-3.5 h-3.5 shrink-0" />}
                  <span>{language === 'ar' ? (node.nameAr || node.nameEn) : node.nameEn}</span>
                </span>
                {node.children.length > 0 && (
                  <ChevronRight className={`w-3.5 h-3.5 text-muted-foreground shrink-0 ${language === 'ar' ? 'rotate-180' : ''}`} />
                )}
              </button>
            );
          })}
        </div>
      )}
        </>
      )}
    </div>
  );
}
