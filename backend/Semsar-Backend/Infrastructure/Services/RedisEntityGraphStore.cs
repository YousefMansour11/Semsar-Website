using System.Text.Json;
using Application.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisEntityGraphStore : IEntityGraphService
{
    private readonly IDistributedCache _cache;
    private readonly IDatabase? _db;
    private readonly RedisSetIndex _index;
    private const string Prefix = "semsar:graph:node:";
    private const string IndexKey = "semsar:graph:index";
    private const string LockPrefix = "semsar:graph:lock:";
    private static readonly DistributedCacheEntryOptions Ttl = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365) };

    public RedisEntityGraphStore(IDistributedCache cache, IConnectionMultiplexer? muxer = null)
    {
        _cache = cache;
        _db = muxer?.GetDatabase();
        _index = new RedisSetIndex(cache, IndexKey, Ttl, muxer);
    }

    public EntityGraphNode BuildEntityNode(string entityType, string entityId, string name, string? description = null)
    {
        var existing = GetNodeAsync(entityType, entityId).GetAwaiter().GetResult();
        if (existing is not null)
            return existing;

        var node = new EntityGraphNode
        {
            EntityType = entityType,
            EntityId = entityId,
            Name = name,
            Description = description
        };

        if (_db != null)
        {
            var lockKey = LockPrefix + entityType + ":" + entityId;
            var lockToken = Guid.NewGuid().ToString();
            var acquired = _db.LockTake(lockKey, lockToken, TimeSpan.FromSeconds(5));
            try
            {
                existing = GetNodeAsync(entityType, entityId).GetAwaiter().GetResult();
                if (existing is not null)
                    return existing;

                SaveNodeAsync(node).GetAwaiter().GetResult();
                return node;
            }
            finally
            {
                if (acquired)
                    _db.LockRelease(lockKey, lockToken);
            }
        }

        SaveNodeAsync(node).GetAwaiter().GetResult();
        return node;
    }

    public void AddRelationship(EntityGraphNode source, string relationship, EntityGraphNode target, double weight = 1.0)
    {
        if (_db != null)
        {
            var sourceKey = source.EntityType + ":" + source.EntityId;
            var targetKey = target.EntityType + ":" + target.EntityId;
            var orderedKeys = string.Compare(sourceKey, targetKey, StringComparison.Ordinal) < 0
                ? (sourceKey, targetKey) : (targetKey, sourceKey);
            var lockKey = LockPrefix + orderedKeys.Item1 + "|" + orderedKeys.Item2;
            var lockToken = Guid.NewGuid().ToString();
            var acquired = _db.LockTake(lockKey, lockToken, TimeSpan.FromSeconds(5));
            try
            {
                var srcNode = GetNodeAsync(source.EntityType, source.EntityId).GetAwaiter().GetResult();
                var tgtNode = GetNodeAsync(target.EntityType, target.EntityId).GetAwaiter().GetResult();

                if (srcNode is null || tgtNode is null) return;

                if (!srcNode.Edges.Any(e => e.TargetEntityType == target.EntityType && e.TargetEntityId == target.EntityId))
                {
                    srcNode.Edges.Add(new EntityGraphEdge
                    {
                        Relationship = relationship,
                        TargetEntityType = target.EntityType,
                        TargetEntityId = target.EntityId,
                        Weight = weight
                    });
                    SaveNodeAsync(srcNode).GetAwaiter().GetResult();
                }

                if (!tgtNode.Edges.Any(e => e.TargetEntityType == source.EntityType && e.TargetEntityId == source.EntityId))
                {
                    tgtNode.Edges.Add(new EntityGraphEdge
                    {
                        Relationship = InverseRelationship(relationship),
                        TargetEntityType = source.EntityType,
                        TargetEntityId = source.EntityId,
                        Weight = weight
                    });
                    SaveNodeAsync(tgtNode).GetAwaiter().GetResult();
                }
            }
            finally
            {
                if (acquired)
                    _db.LockRelease(lockKey, lockToken);
            }
            return;
        }

        var sourceNode = GetNodeAsync(source.EntityType, source.EntityId).GetAwaiter().GetResult();
        var targetNode = GetNodeAsync(target.EntityType, target.EntityId).GetAwaiter().GetResult();

        if (sourceNode is null || targetNode is null) return;

        if (!sourceNode.Edges.Any(e => e.TargetEntityType == target.EntityType && e.TargetEntityId == target.EntityId))
        {
            sourceNode.Edges.Add(new EntityGraphEdge
            {
                Relationship = relationship,
                TargetEntityType = target.EntityType,
                TargetEntityId = target.EntityId,
                Weight = weight
            });
            SaveNodeAsync(sourceNode).GetAwaiter().GetResult();
        }

        if (!targetNode.Edges.Any(e => e.TargetEntityType == source.EntityType && e.TargetEntityId == source.EntityId))
        {
            targetNode.Edges.Add(new EntityGraphEdge
            {
                Relationship = InverseRelationship(relationship),
                TargetEntityType = source.EntityType,
                TargetEntityId = source.EntityId,
                Weight = weight
            });
            SaveNodeAsync(targetNode).GetAwaiter().GetResult();
        }
    }

    public EntityGraphResult BuildKnowledgeGraph(string entityType, string entityId)
    {
        var root = GetNodeAsync(entityType, entityId).GetAwaiter().GetResult();
        if (root is null)
            return new EntityGraphResult();

        var visited = new HashSet<string>();
        var nodes = new List<EntityGraphNode>();
        TraverseGraph(root, visited, nodes, 0, 3);

        var jsonLd = BuildKnowledgeGraphJsonLd(nodes);
        return new EntityGraphResult
        {
            Nodes = nodes,
            JsonLd = jsonLd,
            NodeCount = nodes.Count,
            EdgeCount = nodes.Sum(n => n.Edges.Count)
        };
    }

    public bool VerifyGraphIntegrity(string entityType, string entityId)
    {
        var root = GetNodeAsync(entityType, entityId).GetAwaiter().GetResult();
        if (root is null) return false;

        var visited = new HashSet<string>();
        var allNodes = GetAllNodesAsync().GetAwaiter().GetResult();
        var nodeMap = allNodes.ToDictionary(n => $"{n.EntityType}:{n.EntityId}");

        TraverseGraph(root, visited, new List<EntityGraphNode>(), 0, 10);

        foreach (var node in allNodes)
        {
            var key = $"{node.EntityType}:{node.EntityId}";
            if (!visited.Contains(key)) continue;

            foreach (var edge in node.Edges)
            {
                var targetKey = $"{edge.TargetEntityType}:{edge.TargetEntityId}";
                if (!nodeMap.ContainsKey(targetKey))
                    return false;
            }
        }

        return true;
    }

    public List<EntityGraphNode> GetRelatedEntities(string entityType, string entityId, int maxDepth = 2)
    {
        var root = GetNodeAsync(entityType, entityId).GetAwaiter().GetResult();
        if (root is null) return new List<EntityGraphNode>();

        var visited = new HashSet<string>();
        var result = new List<EntityGraphNode>();
        TraverseGraph(root, visited, result, 0, maxDepth);

        return result.Where(n => n != root).ToList();
    }

    private void TraverseGraph(EntityGraphNode node, HashSet<string> visited, List<EntityGraphNode> nodes, int depth, int maxDepth)
    {
        var key = $"{node.EntityType}:{node.EntityId}";
        if (!visited.Add(key)) return;
        if (depth > maxDepth) return;

        nodes.Add(node);

        foreach (var edge in node.Edges)
        {
            var target = GetNodeAsync(edge.TargetEntityType, edge.TargetEntityId).GetAwaiter().GetResult();
            if (target is not null)
                TraverseGraph(target, visited, nodes, depth + 1, maxDepth);
        }
    }

    private async Task<EntityGraphNode?> GetNodeAsync(string entityType, string entityId)
    {
        var key = $"{Prefix}{entityType}:{entityId}";
        var json = await _cache.GetStringAsync(key);
        return json is not null ? JsonSerializer.Deserialize<EntityGraphNode>(json) : null;
    }

    private async Task SaveNodeAsync(EntityGraphNode node)
    {
        var key = $"{Prefix}{node.EntityType}:{node.EntityId}";
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(node), Ttl);
        await _index.AddAsync($"{node.EntityType}:{node.EntityId}");
    }

    private async Task<List<EntityGraphNode>> GetAllNodesAsync()
    {
        var entries = await _index.GetAllAsync();
        var nodes = new List<EntityGraphNode>();
        foreach (var entry in entries)
        {
            var key = $"{Prefix}{entry}";
            var json = await _cache.GetStringAsync(key);
            if (json is not null)
                nodes.Add(JsonSerializer.Deserialize<EntityGraphNode>(json)!);
        }
        return nodes;
    }

    private string BuildKnowledgeGraphJsonLd(List<EntityGraphNode> nodes)
    {
        try
        {
            var graph = new List<object>();
            foreach (var node in nodes)
            {
                var entry = new Dictionary<string, object?>
                {
                    ["@type"] = MapEntityTypeToSchema(node.EntityType),
                    ["name"] = node.Name,
                    ["identifier"] = node.EntityId
                };
                if (!string.IsNullOrWhiteSpace(node.Description))
                    entry["description"] = node.Description;

                if (node.Edges.Count > 0)
                {
                    var connections = node.Edges.Select(e => new Dictionary<string, object?>
                    {
                        ["@type"] = "Relation",
                        ["relationship"] = e.Relationship,
                        ["identifier"] = $"{e.TargetEntityType}:{e.TargetEntityId}"
                    }).ToList<object>();
                    entry["connectedTo"] = connections;
                }
                graph.Add(entry);
            }

            var obj = new Dictionary<string, object?>
            {
                ["@context"] = new Dictionary<string, object?>
                {
                    ["@vocab"] = "https://schema.org/",
                    ["identifier"] = "https://schema.org/identifier"
                },
                ["@graph"] = graph
            };

            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string MapEntityTypeToSchema(string entityType)
    {
        return entityType.ToLowerInvariant() switch
        {
            "property" => "RealEstateListing",
            "project" => "RealEstateProject",
            "location" => "Place",
            "unit" => "RealEstateListing",
            "developer" => "Organization",
            _ => "Thing"
        };
    }

    private static string InverseRelationship(string relationship)
    {
        return relationship.ToLowerInvariant() switch
        {
            "located_in" => "contains",
            "contains" => "located_in",
            "part_of" => "has_part",
            "has_part" => "part_of",
            "developed_by" => "develops",
            "develops" => "developed_by",
            "related_to" => "related_to",
            _ => "related_to"
        };
    }
}
