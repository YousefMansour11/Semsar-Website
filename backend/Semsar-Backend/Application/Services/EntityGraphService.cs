using System.Text.Json;
using Application.Interfaces;

namespace Application.Services;

public class EntityGraphService : IEntityGraphService
{
    private readonly List<EntityGraphNode> _nodes = new();

    public EntityGraphNode BuildEntityNode(string entityType, string entityId, string name, string? description = null)
    {
        var existing = _nodes.FirstOrDefault(n => n.EntityType == entityType && n.EntityId == entityId);
        if (existing != null)
            return existing;

        var node = new EntityGraphNode
        {
            EntityType = entityType,
            EntityId = entityId,
            Name = name,
            Description = description
        };

        _nodes.Add(node);
        return node;
    }

    public void AddRelationship(EntityGraphNode source, string relationship, EntityGraphNode target, double weight = 1.0)
    {
        if (!source.Edges.Any(e => e.TargetEntityType == target.EntityType && e.TargetEntityId == target.EntityId))
        {
            source.Edges.Add(new EntityGraphEdge
            {
                Relationship = relationship,
                TargetEntityType = target.EntityType,
                TargetEntityId = target.EntityId,
                Weight = weight
            });
        }

        if (!target.Edges.Any(e => e.TargetEntityType == source.EntityType && e.TargetEntityId == source.EntityId))
        {
            target.Edges.Add(new EntityGraphEdge
            {
                Relationship = InverseRelationship(relationship),
                TargetEntityType = source.EntityType,
                TargetEntityId = source.EntityId,
                Weight = weight
            });
        }
    }

    public EntityGraphResult BuildKnowledgeGraph(string entityType, string entityId)
    {
        var root = _nodes.FirstOrDefault(n => n.EntityType == entityType && n.EntityId == entityId);
        if (root == null)
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
        var root = _nodes.FirstOrDefault(n => n.EntityType == entityType && n.EntityId == entityId);
        if (root == null) return false;

        var visited = new HashSet<string>();
        TraverseGraph(root, visited, new List<EntityGraphNode>(), 0, 10);

        foreach (var node in _nodes)
        {
            var key = $"{node.EntityType}:{node.EntityId}";
            if (!visited.Contains(key))
                continue;

            foreach (var edge in node.Edges)
            {
                var targetKey = $"{edge.TargetEntityType}:{edge.TargetEntityId}";
                if (!_nodes.Any(n => $"{n.EntityType}:{n.EntityId}" == targetKey))
                    return false;
            }
        }

        return true;
    }

    public List<EntityGraphNode> GetRelatedEntities(string entityType, string entityId, int maxDepth = 2)
    {
        var root = _nodes.FirstOrDefault(n => n.EntityType == entityType && n.EntityId == entityId);
        if (root == null) return new List<EntityGraphNode>();

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
            var target = _nodes.FirstOrDefault(n =>
                n.EntityType == edge.TargetEntityType && n.EntityId == edge.TargetEntityId);
            if (target != null)
                TraverseGraph(target, visited, nodes, depth + 1, maxDepth);
        }
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
