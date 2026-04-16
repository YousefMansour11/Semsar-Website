import { useMemo, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { API_BASE } from '../lib/api';

export interface LocationTreeNode {
  id: number;
  nameEn: string;
  nameAr: string;
  slug: string;
  level: number;
  path: string;
  parentId: number | null;
  children: LocationTreeNode[];
}

async function fetchLocationTree(): Promise<LocationTreeNode[]> {
  const res = await fetch(`${API_BASE}/locations/tree`);
  if (!res.ok) throw new Error(`Failed to fetch location tree (${res.status})`);
  return res.json();
}

export function useLocationTree() {
  return useQuery({
    queryKey: ['location-tree'],
    queryFn: fetchLocationTree,
    staleTime: 10 * 60 * 1000,
  });
}

export function flattenTree(nodes: LocationTreeNode[], parentPath = ''): { node: LocationTreeNode; displayPath: string }[] {
  const result: { node: LocationTreeNode; displayPath: string }[] = [];
  for (const node of nodes) {
    const path = parentPath ? `${parentPath} > ${node.nameEn}` : node.nameEn;
    result.push({ node, displayPath: path });
    if (node.children.length > 0) {
      result.push(...flattenTree(node.children, path));
    }
  }
  return result;
}

export interface LocationSearchResult {
  id: number;
  nameEn: string;
  nameAr: string;
  slug: string;
  level: number;
  fullPathEn: string;
  fullPathAr: string;
}

async function fetchLocationSearch(query: string): Promise<LocationSearchResult[]> {
  if (!query || query.length < 2) return [];
  const res = await fetch(`${API_BASE}/locations/search?q=${encodeURIComponent(query)}&maxResults=15`);
  if (!res.ok) throw new Error(`Failed to search locations (${res.status})`);
  return res.json();
}

export function useLocationSearch(query: string) {
  return useQuery({
    queryKey: ['location-search', query],
    queryFn: () => fetchLocationSearch(query),
    enabled: query.length >= 2,
    staleTime: 60 * 1000,
  });
}

function buildNameMap(nodes: LocationTreeNode[]): Map<string, string> {
  const map = new Map<string, string>();
  function walk(list: LocationTreeNode[]) {
    for (const node of list) {
      map.set(node.nameEn, node.nameAr);
      map.set(node.nameEn.toLowerCase(), node.nameAr);
      if (node.children.length) walk(node.children);
    }
  }
  walk(nodes);
  return map;
}

import { useLanguage } from '../i18n/LanguageContext';

function splitLocation(location: string): string[] {
  return location.split(',').map(s => s.trim()).filter(Boolean);
}

export function useTranslatedLocation() {
  const { language, t } = useLanguage();
  const { data: tree } = useLocationTree();

  const nameMap = useMemo(() => tree ? buildNameMap(tree) : new Map(), [tree]);

  const translate = useCallback((location: string): string => {
    if (language === 'en') return location;
    const parts = splitLocation(location);
    if (!parts.length) return location;
    return parts.map(part =>
      nameMap.get(part) || nameMap.get(part.toLowerCase()) || t(`location.${part}`, part)
    ).join(', ');
  }, [language, nameMap, t]);

  return translate;
}

