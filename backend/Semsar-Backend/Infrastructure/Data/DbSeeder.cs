using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task EnsureSeedDataAsync(IServiceProvider services)
        {
            if (services == null) return;

            try
            {
                using var scope = services.CreateScope();
                var sp = scope.ServiceProvider;

                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("DbSeeder");
                var auth = sp.GetService<IAuthenticationService>();
                var uow = sp.GetService<IUnitOfWork>();
                if (auth == null)
                {
                    logger?.LogWarning("IAuthenticationService not registered - skipping dev seeding");
                    return;
                }

                var usersExist = false;
                try
                {
                    if (uow != null)
                    {
                        usersExist = await uow.Users.Query().AnyAsync(u => u.Username == "admin");
                    }
                    else
                    {
                        usersExist = await auth.AnyUsersExistAsync();
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to check users existence via UnitOfWork, falling back to auth service");
                    usersExist = await auth.AnyUsersExistAsync();
                }

                if (!usersExist)
                {
                    await SeedAdminUserAsync(auth, uow, logger);
                }
                else
                {
                    logger?.LogDebug("Admin user already exists");
                }

                var ctx = sp.GetService<Infrastructure.Data.AppDbContext>();
                if (ctx != null)
                {
                    await MigrateLocationsAsync(ctx, logger);
                    await MigrateFeaturesAsync(ctx, logger);
                }
                else
                {
                    logger?.LogWarning("AppDbContext not registered - skipping location/feature migration");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    var loggerFactory = services.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("DbSeeder");
                    logger?.LogError(ex, "Failed to ensure seed data");
                }
                catch (Exception logEx)
                {
                    Console.Error.WriteLine($"DbSeeder failed to log error: {logEx.Message}");
                }
            }
        }

        private static async Task SeedAdminUserAsync(IAuthenticationService auth, IUnitOfWork? uow, ILogger? logger)
        {
            const string username = "admin";
            var devPassword = Environment.GetEnvironmentVariable("DEV_ADMIN_PASSWORD");
            if (devPassword == null)
            {
                devPassword = Guid.NewGuid().ToString("N")[..12] + "!Aa1";
                logger?.LogWarning("DEV_ADMIN_PASSWORD environment variable is not set. Generated a random admin password: {DevPassword}. Set DEV_ADMIN_PASSWORD to suppress this warning.", devPassword);
            }

            try
            {
                await auth.CreateUserAsync(username, devPassword, "Admin");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "AuthenticationService.CreateUserAsync failed during seeding - attempting direct DB insert");
                if (uow != null)
                {
                    var hashed = Application.Services.PasswordHelper.HashPassword(devPassword);
                    var user = new Domain.Entities.User { Username = username, PasswordHash = hashed, Role = "Admin", CreatedAt = DateTime.UtcNow };
                    await uow.Users.AddAsync(user);
                    await uow.CommitAsync();
                }
                else
                {
                    logger?.LogError(ex, "Failed to create admin user during seeding and no UnitOfWork available");
                }
            }
        }

        private static string MakeSlug(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "unknown";
            var slug = s.ToLowerInvariant();
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }

        private static async Task<string> UniqueSlugAsync(AppDbContext ctx, string rawName, CancellationToken ct = default)
        {
            var baseSlug = MakeSlug(rawName);
            var slug = baseSlug;
            var counter = 1;
            while (await ctx.Locations.AnyAsync(l => l.Slug == slug, ct))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }
            return slug;
        }

        private static async Task MigrateLocationsAsync(AppDbContext ctx, ILogger? logger)
        {
            try
            {
                var existing = await ctx.Locations.AnyAsync();
                if (existing)
                {
                    logger?.LogDebug("Locations already exist — skipping destructive re-migration");
                    return;
                }

                var propertyLocations = await ctx.Properties
                    .Where(p => !string.IsNullOrEmpty(p.Location))
                    .Select(p => p.Location.Trim())
                    .Distinct()
                    .ToListAsync();

                var unitLocations = await ctx.Units
                    .Where(u => !string.IsNullOrEmpty(u.Location))
                    .Select(u => u.Location.Trim())
                    .Distinct()
                    .ToListAsync();

                var allRaw = propertyLocations
                    .Concat(unitLocations)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var arabicNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var locationArPairs = await ctx.Properties
                    .Where(p => !string.IsNullOrEmpty(p.Location) && !string.IsNullOrEmpty(p.LocationAr))
                    .Select(p => new { En = p.Location.Trim(), Ar = p.LocationAr!.Trim() })
                    .Distinct()
                    .ToListAsync();

                var unitArPairs = await ctx.Units
                    .Where(u => !string.IsNullOrEmpty(u.Location) && !string.IsNullOrEmpty(u.LocationAr))
                    .Select(u => new { En = u.Location.Trim(), Ar = u.LocationAr!.Trim() })
                    .Distinct()
                    .ToListAsync();

                foreach (var pair in locationArPairs.Concat(unitArPairs))
                {
                    var enParts = pair.En.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var arParts = pair.Ar.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var minLen = Math.Min(enParts.Length, arParts.Length);
                    for (int i = 0; i < minLen; i++)
                    {
                        if (!arabicNames.ContainsKey(enParts[i]))
                            arabicNames[enParts[i]] = arParts[i];
                    }
                }

                string ArabicFor(string name) =>
                    arabicNames.TryGetValue(name.Trim(), out var a) ? a : name;

                var govMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var cityMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var areaMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var rawToLocationId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var sortOrder = 0;

                //
                // New format: "Governorate, City, Area" (3-part) or "Governorate, City" (2-part) or "Governorate" (1-part)
                //

                // Phase 1: Create governorates (parts[0] of 2+ part entries only — 1-part entries may be orphan cities)
                foreach (var raw in allRaw.OrderBy(r => r))
                {
                    var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;
                    var govName = parts[0];
                    if (govMap.ContainsKey(govName)) continue;
                    var slug = await UniqueSlugAsync(ctx, govName);
                    var gov = new Location
                    {
                        NameEn = govName,
                        NameAr = ArabicFor(govName),
                        Slug = slug,
                        Level = LocationLevel.Governorate,
                        Path = $"egypt/{slug}",
                        Depth = 1,
                        SortOrder = sortOrder++
                    };
                    ctx.Locations.Add(gov);
                    govMap[govName] = gov.Id;
                }
                await ctx.SaveChangesAsync();

                // Phase 2: Create cities (parts[1] for 2+ part entries)
                foreach (var raw in allRaw.OrderBy(r => r))
                {
                    var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;

                    var govName = parts[0];
                    var cityName = parts[1];
                    if (!govMap.TryGetValue(govName, out var govId)) continue;

                    var compositeCityKey = $"{govId}:{cityName}";
                    if (cityMap.ContainsKey(compositeCityKey)) continue;

                    var govPath = (await ctx.Locations.FindAsync(govId))?.Path ?? $"egypt/{MakeSlug(govName)}";
                    var slug = await UniqueSlugAsync(ctx, cityName);
                    var city = new Location
                    {
                        NameEn = cityName,
                        NameAr = ArabicFor(cityName),
                        Slug = slug,
                        Level = LocationLevel.City,
                        ParentId = govId,
                        Path = $"{govPath}/{slug}",
                        Depth = 2,
                        SortOrder = sortOrder++
                    };
                    ctx.Locations.Add(city);
                    cityMap[compositeCityKey] = city.Id;
                }
                await ctx.SaveChangesAsync();

                // Phase 2.5: Handle 1-part strings — they may be orphan city names (e.g. "Hurghada" from old data).
                // Check if the name matches any existing city; if not, create as governorate.
                var singlePartCityLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in cityMap)
                {
                    var city = await ctx.Locations.FindAsync(kvp.Value);
                    if (city != null)
                        singlePartCityLookup[city.NameEn] = city.Id;
                }

                foreach (var raw in allRaw.OrderBy(r => r))
                {
                    var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 1) continue;

                    var name = parts[0];
                    // Check if this name matches an existing city
                    if (singlePartCityLookup.TryGetValue(name, out var cityId))
                    {
                        rawToLocationId[raw] = cityId;
                        continue;
                    }
                    // Not a known city — create as standalone governorate
                    if (govMap.ContainsKey(name)) continue;
                    var slug = await UniqueSlugAsync(ctx, name);
                    var gov = new Location
                    {
                        NameEn = name,
                        NameAr = ArabicFor(name),
                        Slug = slug,
                        Level = LocationLevel.Governorate,
                        Path = $"egypt/{slug}",
                        Depth = 1,
                        SortOrder = sortOrder++
                    };
                    ctx.Locations.Add(gov);
                    govMap[name] = gov.Id;
                    rawToLocationId[raw] = gov.Id;
                }
                await ctx.SaveChangesAsync();

                // Phase 3: Create areas (parts[2] for 3-part entries)
                foreach (var raw in allRaw.OrderBy(r => r))
                {
                    var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3) continue;

                    var govName = parts[0];
                    var cityName = parts[1];
                    var areaName = parts[2];
                    if (!govMap.TryGetValue(govName, out var govId)) continue;

                    var compositeCityKey = $"{govId}:{cityName}";
                    if (!cityMap.TryGetValue(compositeCityKey, out var cityId)) continue;

                    var compositeAreaKey = $"{cityId}:{areaName}";
                    if (areaMap.ContainsKey(compositeAreaKey)) continue;

                    var cityPath = (await ctx.Locations.FindAsync(cityId))?.Path ?? "";
                    var slug = await UniqueSlugAsync(ctx, areaName);
                    var area = new Location
                    {
                        NameEn = areaName,
                        NameAr = ArabicFor(areaName),
                        Slug = slug,
                        Level = LocationLevel.Area,
                        ParentId = cityId,
                        Path = $"{cityPath}/{slug}",
                        Depth = 3,
                        SortOrder = sortOrder++
                    };
                    ctx.Locations.Add(area);
                    areaMap[compositeAreaKey] = area.Id;
                }
                await ctx.SaveChangesAsync();

                // Phase 4: Map each raw string to the deepest location ID
                foreach (var raw in allRaw.OrderBy(r => r))
                {
                    if (rawToLocationId.ContainsKey(raw)) continue; // already mapped (1-part handled in Phase 2.5)
                    var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length >= 3)
                    {
                        var key = $"{govMap.GetValueOrDefault(parts[0])}:{parts[1]}";
                        if (cityMap.TryGetValue(key, out var cityId))
                        {
                            var areaKey = $"{cityId}:{parts[2]}";
                            if (areaMap.TryGetValue(areaKey, out var areaId))
                                rawToLocationId[raw] = areaId;
                            else
                                rawToLocationId[raw] = cityId;
                        }
                    }
                    else if (parts.Length == 2)
                    {
                        var key = $"{govMap.GetValueOrDefault(parts[0])}:{parts[1]}";
                        if (cityMap.TryGetValue(key, out var cityId))
                            rawToLocationId[raw] = cityId;
                        else if (govMap.TryGetValue(parts[0], out var govId))
                            rawToLocationId[raw] = govId;
                    }
                    else
                    {
                        if (govMap.TryGetValue(parts[0], out var govId))
                            rawToLocationId[raw] = govId;
                    }
                }

                logger?.LogInformation("Migrated {GovCount} governorates, {CityCount} cities, {AreaCount} areas",
                    govMap.Count, cityMap.Count, areaMap.Count);

                // Phase 5: Link LocationId on properties/units
                foreach (var raw in allRaw)
                {
                    if (!rawToLocationId.TryGetValue(raw, out var locId)) continue;

                    var props = await ctx.Properties
                        .Where(p => p.Location != null && p.Location.Trim() == raw && p.LocationId == null)
                        .ToListAsync();
                    foreach (var p in props) p.LocationId = locId;

                    var units = await ctx.Units
                        .Where(u => u.Location != null && u.Location.Trim() == raw && u.LocationId == null)
                        .ToListAsync();
                    foreach (var u in units) u.LocationId = locId;
                }
                await ctx.SaveChangesAsync();

                var linkedProps = await ctx.Properties.CountAsync(p => p.LocationId != null);
                var linkedUnits = await ctx.Units.CountAsync(u => u.LocationId != null);
                logger?.LogInformation("Linked LocationId on {PropCount} properties and {UnitCount} units", linkedProps, linkedUnits);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to migrate locations");
            }
        }

        private static async Task MigrateFeaturesAsync(AppDbContext ctx, ILogger? logger)
        {
            try
            {
                if (await ctx.Features.AnyAsync())
                {
                    logger?.LogDebug("Features already migrated");
                    return;
                }

                var propertyFeatures = await ctx.Properties
                    .Where(p => p.Features != null && p.Features.Count > 0)
                    .SelectMany(p => p.Features)
                    .Distinct()
                    .ToListAsync();

                var propertyFeaturesAr = await ctx.Properties
                    .Where(p => p.FeaturesAr != null && p.FeaturesAr.Count > 0)
                    .SelectMany(p => p.FeaturesAr)
                    .Distinct()
                    .ToListAsync();

                var unitFeatures = await ctx.Units
                    .Where(u => u.Features != null && u.Features.Count > 0)
                    .SelectMany(u => u.Features)
                    .Distinct()
                    .ToListAsync();

                var unitFeaturesAr = await ctx.Units
                    .Where(u => u.FeaturesAr != null && u.FeaturesAr.Count > 0)
                    .SelectMany(u => u.FeaturesAr)
                    .Distinct()
                    .ToListAsync();

                var allFeatureKeys = propertyFeatures
                    .Concat(unitFeatures)
                    .Concat(propertyFeaturesAr)
                    .Concat(unitFeaturesAr)
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                if (allFeatureKeys.Count == 0)
                {
                    logger?.LogDebug("No features found to migrate");
                    return;
                }

                var featureEntities = allFeatureKeys.Select(key => new Feature
                {
                    Key = key.ToLower().Replace(" ", "_").Replace("-", "_"),
                    NameEn = key,
                    NameAr = key
                }).ToList();

                ctx.Features.AddRange(featureEntities);
                await ctx.SaveChangesAsync();
                logger?.LogInformation("Migrated {Count} features from existing data", featureEntities.Count);

                var featureMap = await ctx.Features.ToDictionaryAsync(f => f.Key, f => f.Id, StringComparer.OrdinalIgnoreCase);

                var props = await ctx.Properties
                    .Include(p => p.PropertyFeatures)
                    .Where(p => p.Features != null && p.Features.Count > 0)
                    .ToListAsync();

                foreach (var p in props)
                {
                    foreach (var feat in p.Features)
                    {
                        var key = feat.ToLower().Replace(" ", "_").Replace("-", "_");
                        if (featureMap.TryGetValue(key, out var fid) && !p.PropertyFeatures.Any(pf => pf.FeatureId == fid))
                        {
                            p.PropertyFeatures.Add(new PropertyFeature { PropertyId = p.Id, FeatureId = fid });
                        }
                    }
                }
                await ctx.SaveChangesAsync();
                logger?.LogInformation("Linked features on {Count} properties", props.Count);

                var unitList = await ctx.Units
                    .Include(u => u.UnitFeatures)
                    .Where(u => u.Features != null && u.Features.Count > 0)
                    .ToListAsync();

                foreach (var u in unitList)
                {
                    foreach (var feat in u.Features)
                    {
                        var key = feat.ToLower().Replace(" ", "_").Replace("-", "_");
                        if (featureMap.TryGetValue(key, out var fid) && !u.UnitFeatures.Any(uf => uf.FeatureId == fid))
                        {
                            u.UnitFeatures.Add(new UnitFeature { UnitId = u.Id, FeatureId = fid });
                        }
                    }
                }
                await ctx.SaveChangesAsync();
                logger?.LogInformation("Linked features on {Count} units", unitList.Count);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to migrate features");
            }
        }
    }
}
