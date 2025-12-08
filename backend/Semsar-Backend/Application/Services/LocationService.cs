using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LocationService : ILocationService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<LocationService> _logger;

        public LocationService(IUnitOfWork uow, ILogger<LocationService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<List<LocationTreeNodeDto>> GetTreeAsync(CancellationToken ct = default)
        {
            var all = await _uow.Locations.Query()
                .AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.SortOrder)
                .ThenBy(l => l.NameEn)
                .ToListAsync(ct);

            var propertyLocationIds = await _uow.Locations.Query()
                .AsNoTracking()
                .Where(l => l.IsActive && l.Properties.Any())
                .Select(l => l.Id)
                .ToListAsync(ct);

            var parentLookup = all.ToDictionary(l => l.Id, l => l.ParentId);
            var neededIds = new HashSet<int>(propertyLocationIds);
            foreach (var id in propertyLocationIds)
            {
                var current = parentLookup.GetValueOrDefault(id);
                while (current.HasValue && neededIds.Add(current.Value))
                {
                    current = parentLookup.GetValueOrDefault(current.Value);
                }
            }

            var filtered = all.Where(l => neededIds.Contains(l.Id)).ToList();

            // Fallback: extract each level's Arabic name from property LocationAr
            // Property LocationAr is "البحر الاحمر, الغردقة, الدهار" — split and
            // find the part whose English counterpart matches this location's NameEn.
            // Also walks up ancestors so governorate/city get their Arabic names too.
            var locationArFallback = await _uow.Properties.Query()
                .AsNoTracking()
                .Where(p => p.LocationId.HasValue && p.LocationAr != null && p.LocationAr != "" && p.Location != null)
                .Select(p => new { p.LocationId, p.Location, p.LocationAr })
                .ToListAsync(ct);
            var fallbackMap = new Dictionary<int, string>();
            foreach (var fb in locationArFallback)
            {
                if (fallbackMap.ContainsKey(fb.LocationId!.Value)) continue;
                var loc = all.FirstOrDefault(l => l.Id == fb.LocationId!.Value);
                if (loc == null) continue;

                var enParts = fb.Location.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var arParts = fb.LocationAr!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var idx = Array.FindIndex(enParts, p => p.Equals(loc.NameEn, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0 && idx < arParts.Length)
                {
                    fallbackMap[fb.LocationId!.Value] = arParts[idx];

                    // Walk ancestors: the hierarchy part indices descend toward 0
                    var current = loc;
                    var ancestorIdx = idx;
                    while (current?.ParentId != null && ancestorIdx > 0)
                    {
                        ancestorIdx--;
                        var parent = all.FirstOrDefault(p => p.Id == current.ParentId.Value);
                        if (parent == null) break;
                        if (!fallbackMap.ContainsKey(parent.Id) && ancestorIdx < arParts.Length)
                        {
                            fallbackMap[parent.Id] = arParts[ancestorIdx];
                        }
                        current = parent;
                    }
                }
            }

            return BuildTree(filtered, null, fallbackMap);
        }

        private static List<LocationTreeNodeDto> BuildTree(List<Location> all, int? parentId, Dictionary<int, string>? fallbackMap = null)
        {
            return all
                .Where(l => l.ParentId == parentId)
                .Select(l => new LocationTreeNodeDto
                {
                    Id = l.Id,
                    NameEn = l.NameEn,
                    NameAr = ContainsArabic(l.NameAr) ? l.NameAr :
                        (fallbackMap?.GetValueOrDefault(l.Id) ?? l.NameAr),
                    Slug = l.Slug,
                    Level = (int)l.Level,
                    Path = l.Path,
                    ParentId = l.ParentId,
                    Children = BuildTree(all, l.Id, fallbackMap)
                })
                .ToList();
        }

        public async Task<List<LocationSearchResultDto>> SearchAsync(string query, int maxResults = 15, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<LocationSearchResultDto>();

            var q = query.Trim().ToLower();

            var matches = await _uow.Locations.Query()
                .AsNoTracking()
                .Where(l => l.IsActive && (l.NameEn.ToLower().Contains(q) || l.NameAr.Contains(q)))
                .OrderBy(l => l.SortOrder)
                .ThenBy(l => l.NameEn)
                .Take(maxResults)
                .Select(l => new { l.Id, l.NameEn, l.NameAr, l.Slug, l.Level, l.ParentId, l.Path })
                .ToListAsync(ct);

            if (matches.Count == 0)
                return new List<LocationSearchResultDto>();

            var parentIds = matches.Where(m => m.ParentId.HasValue).Select(m => m.ParentId!.Value).Distinct().ToList();
            var parents = new Dictionary<int, (string NameEn, string NameAr)>();
            if (parentIds.Count > 0)
            {
                parents = await _uow.Locations.Query()
                    .AsNoTracking()
                    .Where(l => parentIds.Contains(l.Id))
                    .Select(l => new { l.Id, l.NameEn, l.NameAr })
                    .ToListAsync(ct)
                    .ContinueWith(t => t.Result.ToDictionary(x => x.Id, x => (x.NameEn, x.NameAr)), ct);
            }

            return matches.Select(m =>
            {
                var pathEn = m.NameEn;
                var pathAr = m.NameAr;
                if (m.ParentId.HasValue && parents.TryGetValue(m.ParentId.Value, out var p))
                {
                    pathEn = $"{m.NameEn}, {p.NameEn}";
                    pathAr = $"{m.NameAr}, {p.NameAr}";
                }

                return new LocationSearchResultDto
                {
                    Id = m.Id,
                    NameEn = m.NameEn,
                    NameAr = m.NameAr,
                    Slug = m.Slug,
                    Level = (int)m.Level,
                    FullPathEn = pathEn,
                    FullPathAr = pathAr
                };
            }).ToList();
        }

        public async Task<List<int>> GetDescendantIdsAsync(int locationId, CancellationToken ct = default)
        {
            var all = await _uow.Locations.Query()
                .AsNoTracking()
                .Where(l => l.IsActive)
                .Select(l => new { l.Id, l.ParentId })
                .ToListAsync(ct);

            var parentMap = all
                .Where(x => x.ParentId.HasValue)
                .GroupBy(x => x.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            var result = new List<int>();
            var stack = new Stack<int>();
            if (parentMap.TryGetValue(locationId, out var directChildren))
            {
                foreach (var c in directChildren)
                    stack.Push(c);
            }

            while (stack.Count > 0)
            {
                var id = stack.Pop();
                result.Add(id);
                if (parentMap.TryGetValue(id, out var children))
                {
                    foreach (var c in children)
                        stack.Push(c);
                }
            }

            return result;
        }

        public static bool ContainsArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var c in text)
            {
                if (c is >= '\u0600' and <= '\u06FF') return true;
            }
            return false;
        }

        public static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "unknown";

            var slug = name.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }

        public static async Task<string> GenerateUniqueSlugAsync(IUnitOfWork uow, string name, CancellationToken ct = default)
        {
            var baseSlug = GenerateSlug(name);
            var slug = baseSlug;
            var counter = 1;
            while (await uow.Locations.Query().AnyAsync(l => l.Slug == slug, ct))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }
            return slug;
        }

        public static string BuildPath(string? parentPath, string slug)
        {
            if (string.IsNullOrEmpty(parentPath))
                return $"egypt/{slug}";
            return $"{parentPath}/{slug}";
        }

        public static int CalculateDepth(int? parentId, List<Location> all)
        {
            if (parentId == null) return 0;
            var parent = all.FirstOrDefault(l => l.Id == parentId);
            return parent?.Depth + 1 ?? 0;
        }

        public async Task<LocationResolutionResult?> ResolveLocationAsync(int? governorateId, int? cityId, int? areaId, CancellationToken ct = default)
        {
            if (governorateId == null && cityId == null && areaId == null)
                return null;

            var all = await _uow.Locations.Query()
                .AsNoTracking()
                .Where(l => l.IsActive)
                .Select(l => new { l.Id, l.NameEn, l.NameAr, l.ParentId, l.Level })
                .ToListAsync(ct);

            var map = all.ToDictionary(x => x.Id);

            var idsInOrder = new List<int>();
            if (governorateId.HasValue && map.ContainsKey(governorateId.Value)) idsInOrder.Add(governorateId.Value);
            if (cityId.HasValue && map.ContainsKey(cityId.Value)) idsInOrder.Add(cityId.Value);
            if (areaId.HasValue && map.ContainsKey(areaId.Value)) idsInOrder.Add(areaId.Value);

            if (idsInOrder.Count == 0) return null;

            var deepestId = idsInOrder.Last();
            var partsEn = new List<string>();
            var partsAr = new List<string>();

            foreach (var id in idsInOrder)
            {
                if (map.TryGetValue(id, out var loc))
                {
                    partsEn.Add(loc.NameEn);
                    if (!string.IsNullOrEmpty(loc.NameAr))
                        partsAr.Add(loc.NameAr);
                }
            }

            return new LocationResolutionResult
            {
                LocationString = string.Join(", ", partsEn),
                LocationStringAr = partsAr.Count > 0 ? string.Join("، ", partsAr) : string.Join(", ", partsEn),
                DeepestId = deepestId
            };
        }

        public async Task<LocationResolutionResult?> ResolveOrCreateFromStringAsync(string? locationString, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(locationString))
                return null;

            var parts = locationString
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (parts.Count == 0) return null;

            Location? parent = null;
            var resolved = new List<Location>();

            foreach (var part in parts)
            {
                var isArabic = ContainsArabic(part);
                Location? location;
                if (parent != null)
                {
                    location = isArabic
                        ? await _uow.Locations.Query()
                            .FirstOrDefaultAsync(l => l.NameAr == part && l.ParentId == parent.Id && l.IsActive, ct)
                        : await _uow.Locations.Query()
                            .FirstOrDefaultAsync(l => l.NameEn == part && l.ParentId == parent.Id && l.IsActive, ct);
                }
                else
                {
                    location = isArabic
                        ? await _uow.Locations.Query()
                            .FirstOrDefaultAsync(l => l.NameAr == part && l.ParentId == null && l.IsActive, ct)
                        : await _uow.Locations.Query()
                            .FirstOrDefaultAsync(l => l.NameEn == part && l.ParentId == null && l.IsActive, ct);
                }

                if (location == null)
                {
                    var depth = (parent?.Depth ?? -1) + 1;
                    var slug = await GenerateUniqueSlugAsync(_uow, part, ct);
                    var path = BuildPath(parent?.Path, slug);
                    var level = InferLevel(depth);

                    location = new Location
                    {
                        NameAr = part,
                        NameEn = part,
                        Slug = slug,
                        ParentId = parent?.Id,
                        Level = level,
                        Path = path,
                        Depth = depth,
                        IsActive = true,
                        SortOrder = 0,
                        CreatedAt = DateTime.UtcNow,
                    };

                    await _uow.Locations.AddAsync(location);
                    await _uow.CommitAsync(ct);
                }

                resolved.Add(location);
                parent = location;
            }

            var deepest = resolved.Last();
            var partsEn = resolved.Select(l => l.NameEn).ToList();
            var partsAr = resolved.Select(l => l.NameAr).ToList();

            return new LocationResolutionResult
            {
                LocationString = string.Join(", ", partsEn),
                LocationStringAr = partsAr.Count > 0 ? string.Join("، ", partsAr) : string.Join(", ", partsEn),
                DeepestId = deepest.Id,
            };
        }

        public static LocationLevel InferLevel(int depth)
        {
            return depth switch
            {
                0 => LocationLevel.Country,
                1 => LocationLevel.Governorate,
                2 => LocationLevel.City,
                _ => LocationLevel.Area
            };
        }
    }
}
