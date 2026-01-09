using Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SearchService : ISearchService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SearchService>? _logger;
        private bool? _ftsAvailable;

        public SearchService(IUnitOfWork uow, ILogger<SearchService>? logger = null)
        {
            _uow = uow;
            _logger = logger;
        }

        public bool IsFtsAvailable => _ftsAvailable ?? false;

        public async Task<List<SearchResult>> SearchPropertiesAsync(string query, int maxResults = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<SearchResult>();

            var term = query.Trim();

            if (_ftsAvailable == true)
            {
                try
                {
                    return await SearchWithFtsAsync(term, maxResults);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "FTS search failed, falling back to LIKE");
                    _ftsAvailable = false;
                }
            }

            return await SearchWithLikeAsync(term, maxResults);
        }

        private async Task<List<SearchResult>> SearchWithFtsAsync(string term, int maxResults)
        {
            var sql = @"
SELECT p.[Id], p.[TitleEn], p.[TitleAr], p.[Price], p.[Location], p.[LocationAr], p.[Slug], k.[RANK]
FROM [Properties] p
INNER JOIN CONTAINSTABLE([Properties], (TitleEn, TitleAr, Location), @term) k ON p.[Id] = k.[KEY]
WHERE p.[IsDeleted] = 0
ORDER BY k.[RANK] DESC";

            var ids = new List<int>();
            var rankMap = new Dictionary<int, double>();
            var resultMap = new Dictionary<int, (string TitleEn, string? TitleAr, decimal Price, string Location, string? LocationAr, string Slug)>();

            var connStr = _uow.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr))
                return await SearchWithLikeAsync(term, maxResults);

            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@term", term));
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                ids.Add(id);
                rankMap[id] = reader.GetDouble(7);
                resultMap[id] = (
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetDecimal(3),
                    reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.GetString(6)
                );
            }

            if (ids.Count == 0)
                return new List<SearchResult>();

            var imageLookup = await _uow.Properties.Query()
                .Where(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, Image = p.Images!.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault() })
                .ToDictionaryAsync(x => x.Id, x => x.Image);

            return ids.Select(id => new SearchResult
            {
                Id = id,
                TitleEn = resultMap[id].TitleEn,
                TitleAr = resultMap[id].TitleAr,
                Price = resultMap[id].Price,
                Location = resultMap[id].Location,
                LocationAr = resultMap[id].LocationAr,
                Slug = resultMap[id].Slug,
                Images = imageLookup.TryGetValue(id, out var img) && img != null ? new List<string> { img } : new List<string>(),
                Rank = rankMap[id]
            }).ToList();
        }

        private async Task<List<SearchResult>> SearchWithLikeAsync(string term, int maxResults)
        {
            var pattern = $"%{term}%";
            var results = await _uow.Properties.Query()
                .Where(p => EF.Functions.Like(p.TitleEn, pattern)
                         || EF.Functions.Like(p.TitleAr, pattern)
                         || EF.Functions.Like(p.Location, pattern))
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(maxResults)
                .Select(p => new
                {
                    p.Id,
                    p.TitleEn,
                    p.TitleAr,
                    p.Price,
                    p.Location,
                    p.LocationAr,
                    p.Slug,
                    Image = p.Images!.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()
                })
                .AsNoTracking()
                .ToListAsync();

            return results.Select(r => new SearchResult
            {
                Id = r.Id,
                TitleEn = r.TitleEn ?? string.Empty,
                TitleAr = r.TitleAr,
                Price = r.Price,
                Location = r.Location ?? string.Empty,
                LocationAr = r.LocationAr,
                Slug = r.Slug ?? string.Empty,
                Images = r.Image != null ? new List<string> { r.Image } : new List<string>(),
                Rank = 0.5
            }).ToList();
        }

        public async Task InitializeFtsAsync()
        {
            try
            {
                var connStr = _uow.ConnectionString;
                if (string.IsNullOrWhiteSpace(connStr))
                {
                    _ftsAvailable = false;
                    return;
                }

                await using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                await using var checkCmd = new SqlCommand(
                    "SELECT 1 WHERE EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'SemsarCatalog')",
                    conn);
                var result = await checkCmd.ExecuteScalarAsync();

                if (result != null)
                {
                    _ftsAvailable = true;
                    return;
                }

                // Try to create the FTS catalog
                try
                {
                    await using var createCmd = new SqlCommand(
                        "CREATE FULLTEXT CATALOG SemsarCatalog AS DEFAULT",
                        conn);
                    await createCmd.ExecuteNonQueryAsync();
                    _ftsAvailable = true;
                    _logger?.LogInformation("FTS catalog 'SemsarCatalog' created successfully");
                }
                catch (Exception createEx)
                {
                    _logger?.LogInformation(createEx, "Could not create FTS catalog. Search will use LIKE. To create manually: CREATE FULLTEXT CATALOG SemsarCatalog AS DEFAULT");
                    _ftsAvailable = false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "FTS initialization check failed");
                _ftsAvailable = false;
            }
        }
    }
}
